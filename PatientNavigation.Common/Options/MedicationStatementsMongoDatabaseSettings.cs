namespace PatientNavigation.Common.Options
{
    public class MedicationStatementsMongoDatabaseSettings : MongoDatabaseSettings, IMedicationStatementsMongoDatabaseSettings
    {
    }

    public interface IMedicationStatementsMongoDatabaseSettings : IMongoDatabaseSettings
    {
    }
}