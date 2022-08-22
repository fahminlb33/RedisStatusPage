using Redis.OM;
using Redis.OM.Modeling;
using StackExchange.Redis;

namespace RedisStatusPage.Core.Services
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

    public static class IncidentStatus
    {
        public const string Reported = "Reported";
        public const string Resolved = "Resolved";
    }

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

    [Document(StorageType = StorageType.Json)]
    public class Incident
    {
        [RedisIdField]
        public Ulid Id { get; set; } = Ulid.NewUlid();
        [Indexed(Sortable = true)]
        public double UnixTimestamp { get; set; }
        [Indexed]
        public string LastStatus { get; set; }
        [Indexed]
        public string ServiceName { get; set; }
        [Indexed]
        public List<IncidentResponse> History { get; set; } = new List<IncidentResponse>();
    }

    [Document(StorageType = StorageType.Json)]
    public class IncidentResponse
    {
        [Indexed]
        public double UnixTimestamp { get; set; }
        [Indexed]
        public string Status { get; set; } = string.Empty;
        [Indexed]
        public string Message { get; set; } = string.Empty;
    }
}
