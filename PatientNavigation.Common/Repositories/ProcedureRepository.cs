using MongoDB.Driver;
using PatientNavigation.Common.Models;
using PatientNavigation.Common.Options;
using System.Collections.Generic;
using System.Linq;

namespace PatientNavigation.Common.Repositories
{
    public interface IProcedureRepository
    {
        ProcedureResource Create(ProcedureResource procedureResource);
        List<ProcedureResource> Get();
        ProcedureResource Get(string id);
        void Remove(ProcedureResource procedureResourceIn);
        void Remove(string id);
        void Update(string id, ProcedureResource procedureResourceIn);
        List<ProcedureResource> GetProcedureFromSubject(string subjectId);
    }

    public class ProcedureRepository : IProcedureRepository
    {
        private readonly IMongoCollection<ProcedureResource> _procedureResources;

        public ProcedureRepository(IMongoDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _procedureResources = database.GetCollection<ProcedureResource>(settings.CollectionName);
        }

        public List<ProcedureResource> Get() =>
            _procedureResources.Find(procedureResource => true).ToList();
        public List<ProcedureResource> GetProcedureFromSubject(string subjectId) =>
            _procedureResources.Find(procedureResource => procedureResource.Procedure != null && procedureResource.Procedure.Subject.Reference.Contains(subjectId)).ToList();

        public ProcedureResource Get(string id) =>
            _procedureResources.Find<ProcedureResource>(procedureResource => procedureResource.Procedure!.Id == id).FirstOrDefault();

        public ProcedureResource Create(ProcedureResource procedureResource)
        {
            _procedureResources.InsertOne(procedureResource);
            return procedureResource;
        }

        public void Update(string id, ProcedureResource procedureResourceIn)
        {

            var update = Builders<ProcedureResource>.Update
                .Set(r => r.Status, procedureResourceIn.Status)
                .Set(r => r.LastUpdated, procedureResourceIn.LastUpdated);

            if (procedureResourceIn.Procedure != null)
                update = update.Set(r => r.Procedure, procedureResourceIn.Procedure);

            _procedureResources.UpdateOne(procedureResource => procedureResource.Procedure!.Id == id, update);
        }

        public void Remove(ProcedureResource procedureResourceIn) =>
            _procedureResources.DeleteOne(procedureResource => procedureResource.Id == procedureResourceIn.Id);

        public void Remove(string id) =>
            _procedureResources.DeleteOne(procedureResource => procedureResource.Id == id);
    }
}
