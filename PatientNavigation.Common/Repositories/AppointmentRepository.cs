using MongoDB.Driver;
using PatientNavigation.Common.Models;
using PatientNavigation.Common.Options;
using System.Collections.Generic;
using System.Linq;

namespace PatientNavigation.Common.Repositories
{
    public interface IAppointmentRepository
    {
        AppointmentResource Create(AppointmentResource appointmentResource);
        List<AppointmentResource> Get();
        AppointmentResource Get(string id);
        List<AppointmentResource> GetAppointmentsOfSubject(string subjectId);
        void Remove(AppointmentResource appointmentResourceIn);
        void Remove(string id);
        void Update(string id, AppointmentResource appointmentResourceIn);
    }

    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly IMongoCollection<AppointmentResource> _appointmentResources;

        public AppointmentRepository(IMongoDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _appointmentResources = database.GetCollection<AppointmentResource>(settings.CollectionName);
        }

        public List<AppointmentResource> Get() =>
            _appointmentResources.Find(appointmentResource => true).ToList();

        public List<AppointmentResource> GetAppointmentsOfSubject(string subjectId) =>
            _appointmentResources.Find(appointmentResource => appointmentResource.Appointment != null && appointmentResource.Appointment.Participant.Any(a => a.Actor.Reference.Contains(subjectId))).ToList();

        public AppointmentResource Get(string id) =>
            _appointmentResources.Find<AppointmentResource>(appointmentResource => appointmentResource.Appointment!.Id == id).FirstOrDefault();

        public AppointmentResource Create(AppointmentResource appointmentResource)
        {
            _appointmentResources.InsertOne(appointmentResource);
            return appointmentResource;
        }

        public void Update(string id, AppointmentResource appointmentResourceIn)
        {

            var update = Builders<AppointmentResource>.Update
                .Set(r => r.Status, appointmentResourceIn.Status)
                .Set(r => r.LastUpdated, appointmentResourceIn.LastUpdated);

            if (appointmentResourceIn.Appointment != null)
                update = update.Set(r => r.Appointment, appointmentResourceIn.Appointment);

            _appointmentResources.UpdateOne(appointmentResource => appointmentResource.Appointment!.Id == id, update);
        }

        public void Remove(AppointmentResource appointmentResourceIn) =>
            _appointmentResources.DeleteOne(appointmentResource => appointmentResource.Id == appointmentResourceIn.Id);

        public void Remove(string id) =>
            _appointmentResources.DeleteOne(appointmentResource => appointmentResource.Id == id);
    }
}
