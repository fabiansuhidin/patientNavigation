
namespace Procedure.Consumer
{
    public interface IProcedureService
    {
        Task InsertProcedure(string procedurePayload);
        Task SyncProcedure(string procedureId);
    }
}