
namespace Appointment.Consumer
{
    public interface IAppointmentService
    {
        Task InsertAppointment(string appointmentPayload);
        Task SyncAppointment(string appointmentPayload);
    }
}