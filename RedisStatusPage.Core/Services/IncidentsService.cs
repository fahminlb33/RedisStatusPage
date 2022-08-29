using Redis.OM;
using RedisStatusPage.Core.Contracts;
using RedisStatusPage.Core.Entities;
using StackExchange.Redis;

namespace RedisStatusPage.Core.Services
{
    public class IncidentsService : IIncidentsService
    {
        private readonly ConnectionMultiplexer _cn;
        private readonly RedisConnectionProvider _cnProvider;

        public IncidentsService(RedisConnectionProvider cnProvider, ConnectionMultiplexer cn)
        {
            _cnProvider = cnProvider;
            _cn = cn;
        }

        public async Task CreateIndexIfNotExists()
        {
            await _cnProvider.Connection.CreateIndexAsync(typeof(Incident));
            await _cnProvider.Connection.CreateIndexAsync(typeof(IncidentResponse));
        }

        public async Task Add(Incident incident)
        {
            var collection = _cnProvider.RedisCollection<Incident>();
            await collection.InsertAsync(incident);
        }

        public async Task Update(Incident incident)
        {
            var collection = _cnProvider.RedisCollection<Incident>();
            await collection.UpdateAsync(incident);
        }

        public async Task Publish(Incident incident)
        {
            var message = new PubSubMessage
            {
                Timestamp = DateTimeHelpers.FromUnixSeconds((long)incident.UnixTimestamp),
                ServiceName = incident.ServiceName,
                Status = incident.LastStatus
            };

            var subscriber = _cn.GetSubscriber();
            await subscriber.PublishAsync(PubSubMessage.ChannelName, message.Serialize());
        }

        public async Task<Incident> Get(string id)
        {
            var collection = _cnProvider.RedisCollection<Incident>();
            return await collection.FindByIdAsync(id);
        }

        public async Task<IList<Incident>> GetAll()
        {
            var collection = _cnProvider.RedisCollection<Incident>();
            return await collection
                .OrderByDescending(x => x.UnixTimestamp)
                .Take(10)
                .ToListAsync();
        }

        public async Task<int> Count()
        {
            var collection = _cnProvider.RedisCollection<Incident>();
            return await collection.CountAsync();
        }

        public async Task<IList<Incident>> GetActive()
        {
            var collection = _cnProvider.RedisCollection<Incident>();
            return await collection
                .Where(x => x.LastStatus == IncidentStatus.Reported)
                .ToListAsync();
        }
    }
}
