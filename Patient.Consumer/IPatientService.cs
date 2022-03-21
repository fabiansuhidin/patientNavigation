
namespace Patient.Consumer
{
    public interface IPatientService
    {
        Task InsertPatient(string patientPayload);
        Task SyncPatient(string patientId);
    }
}