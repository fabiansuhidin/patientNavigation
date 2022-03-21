using MongoDB.Driver;
using PatientNavigation.Common.Models;
using PatientNavigation.Common.Options;
using System.Collections.Generic;
using System.Linq;

namespace PatientNavigation.Common.Repositories
{
    public interface IMedicationRepository
    {
        MedicationResource Create(MedicationResource medicationResource);
        List<MedicationResource> Get();
        MedicationResource Get(string id);
        void Remove(MedicationResource medicationResourceIn);
        void Remove(string id);
        void Update(string id, MedicationResource medicationResourceIn);
    }

    public class MedicationRepository : IMedicationRepository
    {
        private readonly IMongoCollection<MedicationResource> _medicationResources;

        public MedicationRepository(IMedicationsMongoDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _medicationResources = database.GetCollection<MedicationResource>(settings.CollectionName);
        }

        public List<MedicationResource> Get() =>
            _medicationResources.Find(medicationResource => true).ToList();

        public MedicationResource Get(string id) =>
            _medicationResources.Find<MedicationResource>(medicationResource => medicationResource.Medication!.Id == id).FirstOrDefault();

        public MedicationResource Create(MedicationResource medicationResource)
        {
            _medicationResources.InsertOne(medicationResource);
            return medicationResource;
        }

        public void Update(string id, MedicationResource medicationResourceIn)  {

            var update = Builders<MedicationResource>.Update
                .Set(r => r.Status, medicationResourceIn.Status)
                .Set(r => r.LastUpdated, medicationResourceIn.LastUpdated);

            if (medicationResourceIn.Medication != null)
                update = update.Set(r => r.Medication, medicationResourceIn.Medication);

            _medicationResources.UpdateOne(medicationResource => medicationResource.Medication!.Id == id, update);
        }

        public void Remove(MedicationResource medicationResourceIn) =>
            _medicationResources.DeleteOne(medicationResource => medicationResource.Id == medicationResourceIn.Id);

        public void Remove(string id) =>
            _medicationResources.DeleteOne(medicationResource => medicationResource.Id == id);
    }
}
