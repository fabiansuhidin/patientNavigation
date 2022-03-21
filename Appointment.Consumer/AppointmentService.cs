using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using PatientNavigation.Common.Models;
using PatientNavigation.Common.Repositories;

namespace Appointment.Consumer
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ILogger<AppointmentService> _logger;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly FhirClient _client;

        public AppointmentService(ILogger<AppointmentService> logger, IConfiguration configuration, IAppointmentRepository appointmentRepository)
        {
            _logger = logger;
            _appointmentRepository = appointmentRepository;
            var settings = new FhirClientSettings
            {
                Timeout = 30,
                PreferredFormat = ResourceFormat.Json,
                VerifyFhirVersion = true,
                PreferredReturn = Prefer.RespondAsync
            };

            _client = new FhirClient(configuration.GetValue<string>("FhirServerUrl"), settings);
        }

        public async Task InsertAppointment(string appointmentPayload)
        {
            var parser = new FhirJsonParser(new ParserSettings
            {
                AllowUnrecognizedEnums = true
            });
            var resource = parser.Parse<Hl7.Fhir.Model.Appointment>(appointmentPayload);
            var result = await _client.UpdateAsync(resource).ConfigureAwait(false);
            var appointmentResource = new AppointmentResource {
                Appointment = resource,
                LastUpdated = DateTime.UtcNow
            };

            if (result != null) {
                _logger.LogInformation($"Appointment created/updated on FHIR Server with Id: {result.Id}");
                appointmentResource.Status = "CREATED";
            }
            else {
                _logger.LogError($"Appointment not created/updated on FHIR Server.");
                appointmentResource.Status = "FAILED";
            }
            _appointmentRepository.Update(resource.Id, appointmentResource);
        }

        public async Task SyncAppointment(string appointmentId)
        {
            var result = await _client.SearchByIdAsync<Hl7.Fhir.Model.Appointment>(appointmentId);
            if (result != null) 
            {
                var synchronizedResource = result?.Entry?.FirstOrDefault()?.Resource as Hl7.Fhir.Model.Appointment;
                _logger.LogInformation($"Appointment Resource fetched from FHIR Server with ID {synchronizedResource?.Id}");
                var patientResource = new PatientNavigation.Common.Models.AppointmentResource {
                    Appointment = synchronizedResource,
                    Status = "SYNCHRONIZED",
                    LastUpdated = DateTime.UtcNow
                };

                _appointmentRepository.Update(appointmentId, patientResource);
            }
        }
    }
}
