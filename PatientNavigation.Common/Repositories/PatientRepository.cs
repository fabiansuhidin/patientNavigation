using MongoDB.Driver;
using PatientNavigation.Common.Models;
using PatientNavigation.Common.Options;
using System.Collections.Generic;
using System.Linq;

namespace PatientNavigation.Common.Repositories
{
    public interface IPatientRepository
    {
        PatientResource Create(PatientResource patientResource);
        List<PatientResource> Get();
        PatientResource Get(string id);
        void Remove(PatientResource patientResourceIn);
        void Remove(string id);
        void Update(string id, PatientResource patientResourceIn);
    }

    public class PatientRepository : IPatientRepository
    {
        private readonly IMongoCollection<PatientResource> _patientResources;

        public PatientRepository(IMongoDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _patientResources = database.GetCollection<PatientResource>(settings.CollectionName);
        }

        public List<PatientResource> Get() =>
            _patientResources.Find(patientResource => true).ToList();

        public PatientResource Get(string id) =>
            _patientResources.Find<PatientResource>(patientResource => patientResource.Patient!.Id == id).FirstOrDefault();

        public PatientResource Create(PatientResource patientResource)
        {
            _patientResources.InsertOne(patientResource);
            return patientResource;
        }

        public void Update(string id, PatientResource patientResourceIn)  {

            var update = Builders<PatientResource>.Update
                .Set(r => r.Status, patientResourceIn.Status)
                .Set(r => r.LastUpdated, patientResourceIn.LastUpdated);
            
            if (patientResourceIn.Patient != null)
                update = update.Set(r => r.Patient, patientResourceIn.Patient);

            _patientResources.UpdateOne(patientResource => patientResource.Patient!.Id == id, update);
        }

        public void Remove(PatientResource patientResourceIn) =>
            _patientResources.DeleteOne(patientResource => patientResource.Id == patientResourceIn.Id);

        public void Remove(string id) =>
            _patientResources.DeleteOne(patientResource => patientResource.Id == id);
    }
}
