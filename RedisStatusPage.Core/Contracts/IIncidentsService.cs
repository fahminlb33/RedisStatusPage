using RedisStatusPage.Core.Entities;

namespace RedisStatusPage.Core.Contracts
{
    public interface IIncidentsService
    {
        Task CreateIndexIfNotExists();
        Task Add(Incident incident);
        Task Update(Incident incident);
        Task Publish(Incident incident);
        Task<Incident> Get(string id);
        Task<IList<Incident>> GetActive();
        Task<IList<Incident>> GetAll();
        Task<int> Count();
    }
}
