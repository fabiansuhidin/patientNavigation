using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using PatientNavigation.Common.Repositories;

namespace Patient.Consumer
{
    public class PatientService : IPatientService
    {
        private readonly FhirClient _client;
        private readonly IPatientRepository _patientRepository;

        private ILogger<PatientService> _logger { get; }
        public PatientService(ILogger<PatientService> logger, IConfiguration configuration, IPatientRepository patientRepository)
        {
            _logger = logger;
            _patientRepository = patientRepository;
            var settings = new FhirClientSettings
            {
                Timeout = 30,
                PreferredFormat = ResourceFormat.Json,
                VerifyFhirVersion = true,
                PreferredReturn = Prefer.RespondAsync
            };

            _client = new FhirClient(configuration.GetValue<string>("FhirServerUrl"), settings);
        }

        public async Task InsertPatient(string patientPayload)
        {
            var parser = new FhirJsonParser();
            var resource = parser.Parse<Hl7.Fhir.Model.Patient>(patientPayload);
            var patientResource = new PatientNavigation.Common.Models.PatientResource
            {
                Patient = resource,
                LastUpdated = DateTime.UtcNow
            };

            if (string.IsNullOrWhiteSpace(resource.Id))
            {
                _logger.LogError($"Patient not created/updated on FHIR Server.");
                patientResource.Status = "FAILED";
                _patientRepository.Update(resource.Id, patientResource);
                return;
            }

            var result = await _client.UpdateAsync(resource).ConfigureAwait(false);
            

            if (result != null)
            {
                _logger.LogInformation($"Patient created/updated on FHIR Server with Id: {result.Id}");
                patientResource.Status = "CREATED";
            }
            else
            {
                _logger.LogError($"Patient not created/updated on FHIR Server.");
                patientResource.Status = "FAILED";
            }
            _patientRepository.Update(resource.Id, patientResource);
        }

        public async Task SyncPatient(string patientId)
        {
            var result = await _client.SearchByIdAsync<Hl7.Fhir.Model.Patient>(patientId);
            if (result != null)
            {
                var synchronizedResource = result?.Entry?.FirstOrDefault()?.Resource as Hl7.Fhir.Model.Patient;
                _logger.LogInformation($"Patient Resource fetched from FHIR Server with ID {synchronizedResource?.Id}");
                var patientResource = new PatientNavigation.Common.Models.PatientResource
                {
                    Patient = synchronizedResource,
                    Status = "SYNCHRONIZED",
                    LastUpdated = DateTime.UtcNow
                };

                _patientRepository.Update(patientId, patientResource);
            }
        }
    }
}
