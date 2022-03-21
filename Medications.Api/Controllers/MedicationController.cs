using System.Text;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using PatientNavigation.Common.KakfaHelpers;
using PatientNavigation.Common.Repositories;

namespace Medications.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MedicationController : ControllerBase
    {
        private readonly ILogger<MedicationController> _logger;
        private readonly IMedicationRepository _medicationRepository;
        private readonly EventsHelper _eventsHelper;
        private readonly string _topicName;

        public MedicationController(ILogger<MedicationController> logger,
                                    IConfiguration configuration,
                                    IMedicationRepository medicationRepository)
        {
            _logger = logger;
            _medicationRepository = medicationRepository;
            var boostrapServer = configuration.GetValue<string>("KafkaConfig:Servers");
            _eventsHelper = new EventsHelper(boostrapServer);

            _topicName = configuration.GetValue<string>("KafkaConfig:MedicationTopicName");
        }

        [HttpPost]
        public async Task<ObjectResult> Post([FromBody] object medication)
        {
            var parser = new FhirJsonParser();
            var resource = parser.Parse<Hl7.Fhir.Model.Medication>(medication.ToString());
            resource.Id = Guid.NewGuid().ToString();
            var result = _medicationRepository.Create(new PatientNavigation.Common.Models.MedicationResource {
                Medication = resource,
                LastUpdated = DateTime.UtcNow,
                Status = "PENDING"
            });
            
            await _eventsHelper.Produce(_topicName, resource.ToJson());
            return Ok(new { Id = resource.Id });
        }

        [HttpPut("{medicationId}")]
        public async Task<ObjectResult> Put([FromRoute] string medicationId, [FromBody] object medication)
        {
            var parser = new FhirJsonParser();
            var resource = parser.Parse<Hl7.Fhir.Model.Medication>(medication.ToString());
            await _eventsHelper.Produce(_topicName, resource.ToJson());
            return Ok("");
        }

        [HttpGet("{medicationId}")]
        public ObjectResult Get([FromRoute] string medicationId)
        {
            var result = _medicationRepository.Get(medicationId);
            return Ok(result.Medication.ToJson());
        }

        [HttpGet("")]
        public ObjectResult GetAllAsync()
        {
            var result = _medicationRepository.Get();
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

            foreach (var r in result.Select(a => a.Medication))
                searchResponse.AddSearchEntry(r, "", Bundle.SearchEntryMode.Match);
            
            return Ok(searchResponse.ToJson());
        }

        [HttpPost("syncFHIRServer/{medicationId}")]
        public async Task<ObjectResult> SyncFHIRServerAsync([FromRoute] string medicationId) 
        {
            var headers = new Confluent.Kafka.Headers { new Confluent.Kafka.Header("ACTION", Encoding.ASCII.GetBytes("SYNC"))};
            await _eventsHelper.Produce(_topicName, medicationId, headers);
            return Ok("");
        }
    }
}
