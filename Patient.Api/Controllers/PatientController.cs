using System.Text;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using PatientNavigation.Common.KakfaHelpers;
using PatientNavigation.Common.Repositories;

namespace Patient.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly ILogger<PatientController> _logger;
        private readonly IPatientRepository _patientRepository;
        private readonly string _topicName;
        //private string _topicNameAction;
        private readonly EventsHelper _eventsHelper;

        public PatientController(ILogger<PatientController> logger, IConfiguration configuration, IPatientRepository patientRepository)
        {
            _logger = logger;
            var boostrapServer = configuration.GetValue<string>("KafkaConfig:Servers");
            _eventsHelper = new EventsHelper(boostrapServer);

            _patientRepository = patientRepository;
            _topicName = configuration.GetValue<string>("KafkaConfig:TopicName");
        }

        [HttpPost]
        public async Task<ObjectResult> PostAsync([FromBody] Hl7.Fhir.Model.Patient patient)
        {
            patient.Id = Guid.NewGuid().ToString();
            var result = _patientRepository.Create(new PatientNavigation.Common.Models.PatientResource
            {
                Patient = patient,
                LastUpdated = DateTime.UtcNow,
                Status = "PENDING"
            });

            await _eventsHelper.Produce(_topicName, patient.ToJson());
            return Ok(new { Id = patient.Id });
        }

        [HttpPut("{patientId}")]
        public async Task<ObjectResult> PostAsync([FromRoute] string patientId, [FromBody] Hl7.Fhir.Model.Patient patient)
        {
            await _eventsHelper.Produce(_topicName, patient.ToJson());
            return Ok("");
        }

        [HttpGet("{patientId}")]
        public ObjectResult GetAsync([FromRoute] string patientId)
        {
            var result = _patientRepository.Get(patientId);
            return Ok(result.Patient.ToJson());
        }

        [HttpGet("")]
        public ObjectResult GetAllAsync()
        {
            var result = _patientRepository.Get();
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

            foreach (var r in result.Select(a => a.Patient))
            {
                //var full_url = r?.ResourceBase.ToString() + r?.TypeName.ToString() + r?.Id;
                searchResponse.AddSearchEntry(r, "", Bundle.SearchEntryMode.Match);
            }
            return Ok(searchResponse.ToJson());
        }

        [HttpPost("syncFHIRServer/{patientId}")]
        public async Task<ObjectResult> SyncFHIRServerAsync([FromRoute] string patientId)
        {
            var headers = new Confluent.Kafka.Headers { new Confluent.Kafka.Header("ACTION", Encoding.ASCII.GetBytes("SYNC")) };
            await _eventsHelper.Produce(_topicName, patientId, headers);
            return Ok("");
        }
    }
}