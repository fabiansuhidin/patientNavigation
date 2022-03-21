using System.Text;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using PatientNavigation.Common.KakfaHelpers;
using PatientNavigation.Common.Repositories;

namespace Procedure.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProcedureController : ControllerBase
    {
        private readonly ILogger<ProcedureController> _logger;
        private readonly IProcedureRepository _procedureRepository;
        private readonly EventsHelper _eventsHelper;
        private readonly string _topicName;

        public ProcedureController(ILogger<ProcedureController> logger, IConfiguration configuration, IProcedureRepository procedureRepository)
        {
            _logger = logger;
            _procedureRepository = procedureRepository;
            var boostrapServer = configuration.GetValue<string>("KafkaConfig:Servers");
            _eventsHelper = new EventsHelper(boostrapServer);

            _topicName = configuration.GetValue<string>("KafkaConfig:TopicName");
        }

        [HttpPost]
        public async Task<ObjectResult> Post([FromBody] Hl7.Fhir.Model.Procedure procedure)
        {
            procedure.Id = Guid.NewGuid().ToString();
            var result = _procedureRepository.Create(new PatientNavigation.Common.Models.ProcedureResource
            {
                Procedure = procedure,
                LastUpdated = DateTime.UtcNow,
                Status = "PENDING"
            });

            await _eventsHelper.Produce(_topicName, procedure.ToJson());
            return Ok(new {Id = procedure.Id });
        }

        [HttpPut("{procedureId}")]
        public async Task<ObjectResult> Put([FromRoute] string procedureId, [FromBody] Hl7.Fhir.Model.Procedure procedure)
        {
            await _eventsHelper.Produce(_topicName, procedure.ToJson());
            return Ok("");
        }

        [HttpGet("{procedureId}")]
        public ObjectResult Get([FromRoute] string procedureId)
        {
            var result =_procedureRepository.Get(procedureId);
            return Ok(result.Procedure.ToJson());
        }

        [HttpGet("")]
        public ObjectResult GetAllAsync([FromQuery] string? subjectId)
        {
            var result = new List<PatientNavigation.Common.Models.ProcedureResource>();
            if (string.IsNullOrEmpty(subjectId))
                result = _procedureRepository.Get();
            else
                result = _procedureRepository.GetProcedureFromSubject(subjectId);
            var searchResponse = new Bundle();
            searchResponse.Type = Bundle.BundleType.Searchset;

            // adding some metadata
            searchResponse.Id = Guid.NewGuid().ToString();
            searchResponse.Meta = new Meta()
            {
                VersionId = "1",
                LastUpdatedElement = Instant.Now()
            };

            // TODO: ADD AFTER
            //searchResponse.SelfLink = new Uri();
            searchResponse.Total = result.Count;

            foreach (var r in result.Select(a => a.Procedure))
                searchResponse.AddSearchEntry(r, "", Bundle.SearchEntryMode.Match);
            
            return Ok(searchResponse.ToJson());
        }

        [HttpPost("syncFHIRServer/{procedureId}")]
        public async Task<ObjectResult> SyncFHIRServerAsync([FromRoute] string procedureId) 
        {
            var headers = new Confluent.Kafka.Headers { new Confluent.Kafka.Header("ACTION", Encoding.ASCII.GetBytes("SYNC"))};
            await _eventsHelper.Produce(_topicName, procedureId, headers);
            return Ok("");
        }
    }
}