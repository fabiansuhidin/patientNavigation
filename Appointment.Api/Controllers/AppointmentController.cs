using System.Text;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using PatientNavigation.Common.KakfaHelpers;
using PatientNavigation.Common.Repositories;

namespace Appointment.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AppointmentController : ControllerBase
    {
        private readonly ILogger<AppointmentController> _logger;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly EventsHelper _eventsHelper;
        private readonly string _topicName;

        public AppointmentController(ILogger<AppointmentController> logger, IConfiguration configuration, IAppointmentRepository appointmentRepository)
        {
            _logger = logger;
            _appointmentRepository = appointmentRepository;
            var boostrapServer = configuration.GetValue<string>("KafkaConfig:Servers");
            _eventsHelper = new EventsHelper(boostrapServer);

            _topicName = configuration.GetValue<string>("KafkaConfig:TopicName");
        }

        [HttpPost]
        public async Task<ObjectResult> Post([FromBody] Hl7.Fhir.Model.Appointment appointment)
        {
            appointment.Id = Guid.NewGuid().ToString();
            var result = _appointmentRepository.Create(new PatientNavigation.Common.Models.AppointmentResource
            {
                Appointment = appointment,
                LastUpdated = DateTime.UtcNow,
                Status = "PENDING"
            });
            
            await _eventsHelper.Produce(_topicName, appointment.ToJson());
            return Ok(new {Id = appointment.Id});
        }

        [HttpPut("{appointmentId}")]
        public async Task<ObjectResult> Put([FromRoute] string appointmentId, [FromBody] Hl7.Fhir.Model.Appointment appointment)
        {
            await _eventsHelper.Produce(_topicName, appointment.ToJson());
            return Ok("");
        }

        [HttpGet("{appointmentId}")]
        public ObjectResult Get([FromRoute] string appointmentId)
        {
            var resource = _appointmentRepository.Get(appointmentId);
            return Ok(resource.Appointment.ToJson());
        }
        
        [HttpGet("")]
        public ObjectResult GetAllAsync([FromQuery] string? subject)
        {   
            var result = new List<PatientNavigation.Common.Models.AppointmentResource>();
            if (string.IsNullOrEmpty(subject))
                result = _appointmentRepository.Get();
            else 
                result = _appointmentRepository.GetAppointmentsOfSubject(subject);
                
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

            foreach (var r in result.Select(a => a.Appointment))
                searchResponse.AddSearchEntry(r, "", Bundle.SearchEntryMode.Match);
            
            return Ok(searchResponse.ToJson());
        }

        [HttpPost("syncFHIRServer/{appointmentId}")]
        public async Task<ObjectResult> SyncFHIRServerAsync([FromRoute] string appointmentId) 
        {
            var headers = new Confluent.Kafka.Headers { new Confluent.Kafka.Header("ACTION", Encoding.ASCII.GetBytes("SYNC"))};
            await _eventsHelper.Produce(_topicName, appointmentId, headers);
            return Ok("");
        }
    }
}