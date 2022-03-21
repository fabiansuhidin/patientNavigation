using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json;
using PatientNavigation.Common.Models;
using PatientNavigation.Common.Repositories;

namespace Medications.Consumer
{
    public class MedicationService : IMedicationService
    {
        private FhirClient _client;
        private ILogger<MedicationService> _logger;
        private readonly IMedicationRepository _medicationRepository;

        public MedicationService(
            ILogger<MedicationService> logger,
            IConfiguration configuration,
            IMedicationRepository medicationRepository)

        {
            _logger = logger;
            _medicationRepository = medicationRepository;
            var settings = new FhirClientSettings
            {
                Timeout = 30,
                PreferredFormat = ResourceFormat.Json,
                VerifyFhirVersion = true,
                PreferredReturn = Prefer.RespondAsync
            };

            _client = new FhirClient(configuration.GetValue<string>("FhirServerUrl"), settings);
        }

        public async Task InsertMedication(string medicationPayload)
        {
            var parser = new FhirJsonParser();
            var resource = parser.Parse<Hl7.Fhir.Model.Medication>(medicationPayload);
            var medicationResource = new MedicationResource
            {
                Medication = resource,
                LastUpdated = DateTime.UtcNow
            };
            if (string.IsNullOrEmpty(resource.Id))
            {
                _logger.LogError($"Medication not created/updated on FHIR Server.");
                medicationResource.Status = "FAILED";
                _medicationRepository.Update(resource.Id, medicationResource);
                return;
            }


            var result = await _client.UpdateAsync(resource).ConfigureAwait(false);
            if (result != null)
            {
                _logger.LogInformation($"Medication created/updated on FHIR Server with Id: {result.Id}");
                medicationResource.Status = "CREATED";
            }
            else
            {
                _logger.LogError($"Medication not created/updated on FHIR Server.");
                medicationResource.Status = "FAILED";
            }
            _medicationRepository.Update(resource.Id, medicationResource);
        }

        public async Task SyncMedication(string medicationId)
        {
            var result = await _client.SearchByIdAsync<Hl7.Fhir.Model.Medication>(medicationId);
            if (result != null)
            {
                var synchronizedResource = result?.Entry?.FirstOrDefault()?.Resource as Hl7.Fhir.Model.Medication;
                var medicationResource = new PatientNavigation.Common.Models.MedicationResource
                { LastUpdated = DateTime.UtcNow };
                if (synchronizedResource != null)
                {
                    _logger.LogInformation($"Medication Resource fetched from FHIR Server with ID {synchronizedResource?.Id}");
                    medicationResource.Status = "SYNCHRONIZED";
                    medicationResource.Medication = synchronizedResource;
                }
                else
                {
                    medicationResource.Status = "FAILEDSYNCHRONIZED";
                }
                _medicationRepository.Update(medicationId, medicationResource);
            }
        }
    }
}
