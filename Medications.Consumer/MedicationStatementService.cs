using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using PatientNavigation.Common.Models;
using PatientNavigation.Common.Repositories;

namespace Medications.Consumer
{
    public class MedicationStatementService : IMedicationStatementService
    {
        private FhirClient _client;
        private ILogger<MedicationStatementService> _logger;
        private readonly IMedicationStatementRepository _medicationStatementRepository;

        public MedicationStatementService(
            ILogger<MedicationStatementService> logger,
            IConfiguration configuration,
            IMedicationStatementRepository medicationStatementRepository)
        {
            _logger = logger;
            _medicationStatementRepository = medicationStatementRepository;
            var settings = new FhirClientSettings
            {
                Timeout = 30,
                PreferredFormat = ResourceFormat.Json,
                VerifyFhirVersion = true,
                PreferredReturn = Prefer.RespondAsync
            };

            _client = new FhirClient(configuration.GetValue<string>("FhirServerUrl"), settings);
        }

        public async Task InsertMedicationStatement(string medicationStatementPayload)
        {
            var parser = new FhirJsonParser();
            var resource = parser.Parse<Hl7.Fhir.Model.MedicationStatement>(medicationStatementPayload);
             var medicationStatementResource = new MedicationStatementResource {
                MedicationStatement = resource,
                LastUpdated = DateTime.UtcNow
            };
            if (string.IsNullOrEmpty(resource.Id)) {
                
                _logger.LogError($"Medication Statement not created/updated on FHIR Server.");
                medicationStatementResource.Status = "FAILED";
                 _medicationStatementRepository.Update(resource.Id, medicationStatementResource);
                return;
            }
            var result = await _client.UpdateAsync(resource).ConfigureAwait(false);
           
            if (result != null)
            {
                _logger.LogInformation($"Medication Statement created/updated on FHIR Server with Id: {result.Id}");
                medicationStatementResource.Status = "CREATED";
            }
            else
            {
                _logger.LogError($"Medication Statement not created/updated on FHIR Server.");
                medicationStatementResource.Status = "FAILED";
            }
            _medicationStatementRepository.Update(resource.Id, medicationStatementResource);
        }

        public async Task SyncMedicationStatement(string medicationStatementId)
        {
            var result = await _client.SearchByIdAsync<Hl7.Fhir.Model.MedicationStatement>(medicationStatementId);
            if (result != null) 
            {
                var synchronizedResource = result?.Entry?.FirstOrDefault()?.Resource as Hl7.Fhir.Model.MedicationStatement;
                _logger.LogInformation($"Medication Statement Resource fetched from FHIR Server with ID {synchronizedResource?.Id}");
                var medicationStatementResource = new PatientNavigation.Common.Models.MedicationStatementResource {
                    MedicationStatement = synchronizedResource,
                    Status = "SYNCHRONIZED",
                    LastUpdated = DateTime.UtcNow
                };

                _medicationStatementRepository.Update(medicationStatementId, medicationStatementResource);
            }
        }
    }
}
