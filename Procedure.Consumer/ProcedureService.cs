using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using PatientNavigation.Common.Models;
using PatientNavigation.Common.Repositories;

namespace Procedure.Consumer
{
    public class ProcedureService : IProcedureService
    {
        private readonly ILogger<ProcedureService> _logger;
        private readonly IProcedureRepository _procedureRepository;
        private readonly FhirClient _client;

        public ProcedureService(ILogger<ProcedureService> logger, IConfiguration configuration, IProcedureRepository procedureRepository)
        {
            _logger = logger;
            _procedureRepository = procedureRepository;
            var settings = new FhirClientSettings
            {
                Timeout = 30,
                PreferredFormat = ResourceFormat.Json,
                VerifyFhirVersion = true,
                PreferredReturn = Prefer.RespondAsync
            };

            _client = new FhirClient(configuration.GetValue<string>("FhirServerUrl"), settings);
        }

        public async Task InsertProcedure(string procedurePayload)
        {
            var parser = new FhirJsonParser();
            var resource = parser.Parse<Hl7.Fhir.Model.Procedure>(procedurePayload);
            var procedureResource = new ProcedureResource
            {
                Procedure = resource,
                LastUpdated = DateTime.UtcNow
            };

            if (string.IsNullOrEmpty(resource.Id) || resource.Subject.Reference.Split("/")[1].Length == 0)
            {
                _logger.LogError($"Procedure not created/updated on FHIR Server.");
                procedureResource.Status = "FAILED";
                _procedureRepository.Update(resource.Id, procedureResource);
                return;
            }

            var result = await _client.UpdateAsync(resource).ConfigureAwait(false);


            if (result != null)
            {
                _logger.LogInformation($"Procedure created/updated on FHIR Server with Id: {result.Id}");
                procedureResource.Status = "CREATED";
            }
            else
            {
                _logger.LogError($"Procedure not created/updated on FHIR Server.");
                procedureResource.Status = "FAILED";
            }
            _procedureRepository.Update(resource.Id, procedureResource);
        }

        public async Task SyncProcedure(string procedureId)
        {
            var result = await _client.SearchByIdAsync<Hl7.Fhir.Model.Procedure>(procedureId);
            if (result != null)
            {
                var synchronizedResource = result?.Entry?.FirstOrDefault()?.Resource as Hl7.Fhir.Model.Procedure;
                _logger.LogInformation($"Prodecure Resource fetched from FHIR Server with ID {synchronizedResource?.Id}");
                var procedureResource = new PatientNavigation.Common.Models.ProcedureResource
                {
                    Procedure = synchronizedResource,
                    Status = "SYNCHRONIZED",
                    LastUpdated = DateTime.UtcNow
                };

                _procedureRepository.Update(procedureId, procedureResource);
            }
        }
    }
}
