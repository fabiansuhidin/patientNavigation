using System.Text;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using PatientNavigation.Common.KakfaHelpers;
using PatientNavigation.Common.Repositories;

namespace Medications.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MedicationStatementController : ControllerBase
    {

        private readonly ILogger<MedicationStatementController> _logger;
        private readonly IMedicationStatementRepository _medicationStatementRepository;
        private EventsHelper _eventsHelper;
        private string _topicName;

        public MedicationStatementController(ILogger<MedicationStatementController> logger,
                                             IConfiguration configuration,
                                             IMedicationStatementRepository medicationStatementRepository)
        {
            _logger = logger;
            _medicationStatementRepository = medicationStatementRepository;
            var boostrapServer = configuration.GetValue<string>("KafkaConfig:Servers");
            _eventsHelper = new EventsHelper(boostrapServer);

            _topicName = configuration.GetValue<string>("KafkaConfig:MedicationStatementTopicName");
        }

        [HttpPost]
        public async Task<ObjectResult> Post([FromBody] object medicationStatement)
        {
            var parser = new FhirJsonParser();
            var resource = parser.Parse<Hl7.Fhir.Model.MedicationStatement>(medicationStatement.ToString());
            resource.Id = Guid.NewGuid().ToString();
            var result = _medicationStatementRepository.Create(new PatientNavigation.Common.Models.MedicationStatementResource {
                MedicationStatement = resource,
                LastUpdated = DateTime.UtcNow,
                Status = "PENDING"
            });
            await _eventsHelper.Produce(_topicName, resource.ToJson());
            return Ok(new { Id = resource.Id });
        }

        [HttpPut("{medicationStatementId}")]
        public async Task<ObjectResult> Post([FromRoute] string medicationStatementId, [FromBody] object medicationStatement)
        {
            var parser = new FhirJsonParser();
            var resource = parser.Parse<Hl7.Fhir.Model.MedicationStatement>(medicationStatement.ToString());
            await _eventsHelper.Produce(_topicName, resource.ToJson());
            return Ok("");
        }

        [HttpGet("{medicationStatementId}")]
        public ObjectResult Get([FromRoute] string medicationStatementId)
        {
            var result = _medicationStatementRepository.Get(medicationStatementId);
            return Ok(result.MedicationStatement.ToJson());
        }

        [HttpGet("")]
        public ObjectResult GetAllAsync([FromQuery] string? subjectId)
        {
            var result = new List<PatientNavigation.Common.Models.MedicationStatementResource>();
            if (string.IsNullOrEmpty(subjectId))
                result = _medicationStatementRepository.Get();
            else 
                result = _medicationStatementRepository.GetMedicationStatementFromSubjectId(subjectId);
            
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

            foreach (var r in result.Select(a => a.MedicationStatement))
                searchResponse.AddSearchEntry(r, "", Bundle.SearchEntryMode.Match);
            
            return Ok(searchResponse.ToJson());
        }

        [HttpPost("syncFHIRServer/{medicationStatementId}")]
        public async Task<ObjectResult> SyncFHIRServerAsync([FromRoute] string medicationStatementId) 
        {
            var headers = new Confluent.Kafka.Headers { new Confluent.Kafka.Header("ACTION", Encoding.ASCII.GetBytes("SYNC"))};
            await _eventsHelper.Produce(_topicName, medicationStatementId, headers);
            return Ok("");
        }
    }
}