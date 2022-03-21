using MongoDB.Driver;
using PatientNavigation.Common.Models;
using PatientNavigation.Common.Options;
using System.Collections.Generic;
using System.Linq;

namespace PatientNavigation.Common.Repositories
{
    public interface IMedicationStatementRepository
    {
        MedicationStatementResource Create(MedicationStatementResource medicationStatementResource);
        List<MedicationStatementResource> Get();
        MedicationStatementResource Get(string id);
        void Remove(MedicationStatementResource medicationStatementResourceIn);
        void Remove(string id);
        void Update(string id, MedicationStatementResource medicationStatementResourceIn);
        List<MedicationStatementResource> GetMedicationStatementFromSubjectId(string subjectId);
    }

    public class MedicationStatementRepository : IMedicationStatementRepository
    {
        private readonly IMongoCollection<MedicationStatementResource> _medicationStatementResources;

        public MedicationStatementRepository(IMedicationStatementsMongoDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _medicationStatementResources = database.GetCollection<MedicationStatementResource>(settings.CollectionName);
        }

        public List<MedicationStatementResource> Get() =>
            _medicationStatementResources.Find(medicationStatementResource => true).ToList();

        public List<MedicationStatementResource> GetMedicationStatementFromSubjectId(string subjectId)
           =>
            _medicationStatementResources.Find(medicationStatementResource => medicationStatementResource.MedicationStatement != null && medicationStatementResource.MedicationStatement.Subject.Reference.Contains(subjectId)).ToList();

        public MedicationStatementResource Get(string id) =>
            _medicationStatementResources.Find<MedicationStatementResource>(medicationStatementResource => medicationStatementResource.MedicationStatement!.Id == id).FirstOrDefault();

        public MedicationStatementResource Create(MedicationStatementResource medicationStatementResource)
        {
            _medicationStatementResources.InsertOne(medicationStatementResource);
            return medicationStatementResource;
        }

        public void Update(string id, MedicationStatementResource medicationStatementResourceIn)
        {

            var update = Builders<MedicationStatementResource>.Update
                .Set(r => r.Status, medicationStatementResourceIn.Status)
                .Set(r => r.LastUpdated, medicationStatementResourceIn.LastUpdated);

            if (medicationStatementResourceIn.MedicationStatement != null)
                update = update.Set(r => r.MedicationStatement, medicationStatementResourceIn.MedicationStatement);

            _medicationStatementResources.UpdateOne(medicationStatementResource => medicationStatementResource.MedicationStatement!.Id == id, update);
        }

        public void Remove(MedicationStatementResource medicationStatementResourceIn) =>
            _medicationStatementResources.DeleteOne(medicationStatementResource => medicationStatementResource.Id == medicationStatementResourceIn.Id);

        public void Remove(string id) =>
            _medicationStatementResources.DeleteOne(medicationStatementResource => medicationStatementResource.Id == id);
    }
}
