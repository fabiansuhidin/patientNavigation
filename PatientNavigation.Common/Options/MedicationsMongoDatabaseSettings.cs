namespace PatientNavigation.Common.Options
{
    public class MedicationsMongoDatabaseSettings : MongoDatabaseSettings, IMedicationsMongoDatabaseSettings
    {
    }

    public interface IMedicationsMongoDatabaseSettings : IMongoDatabaseSettings
    {
    }
}