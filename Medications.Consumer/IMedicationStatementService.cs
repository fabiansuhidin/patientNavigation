namespace Medications.Consumer
{
    public interface IMedicationStatementService
    {
        Task InsertMedicationStatement(string medicationStatementPayload);
        Task SyncMedicationStatement(string medicationStatementId);
    }
}