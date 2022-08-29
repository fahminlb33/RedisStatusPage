using Redis.OM;
using RedisStatusPage.Core.Contracts;
using RedisStatusPage.Core.Entities;
using StackExchange.Redis;

namespace RedisStatusPage.Core.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly ConnectionMultiplexer _cn;
        private readonly RedisConnectionProvider _cnProvider;

        private const string FirstStartupKey = "STATISTICS:FIRST_STARTUP";
        private const string ServiceLastStatusKey = "STATISTICS:LAST_STATUS";

        public int GraphLastSecond { get; set; } = 900;

        public StatisticsService(ConnectionMultiplexer cn, RedisConnectionProvider cnProvider)
        {
            _cn = cn;
            _cnProvider = cnProvider;
        }

        public async Task CreateIndexIfNotExists()
        {
            await _cnProvider.Connection.CreateIndexAsync(typeof(MonitoringSnapshot));

            var db = _cn.GetDatabase();
            if (!await db.KeyExistsAsync(FirstStartupKey))
            {
                await db.StringSetAsync(FirstStartupKey, DateTimeHelpers.ToUnixFormat(DateTime.Now));
            }
        }

        public async Task<MonitoringStatistics> GetDashboard()
        {
            var db = _cn.GetDatabase();

            var firstStartup = await db.StringGetAsync(FirstStartupKey);
            var services = await db.HashGetAllAsync(ServiceLastStatusKey);

            return new MonitoringStatistics
            {
                FirstStartup = DateTimeHelpers.FromUnixFormat(firstStartup),
                ReadyCount = services.Count(x => x.Value == "1"),
                UnreachableCount = services.Count(x => x.Value == "0"),
                ServiceCount = services.Length,
            };
        }

        public async Task Snapshot(DateTime timestamp, string serviceName, bool healthy, int latency)
        {
            // save to redis using OM
            var collection = _cnProvider.RedisCollection<MonitoringSnapshot>();
            await collection.InsertAsync(new MonitoringSnapshot
            {
                UnixTimestamp = timestamp.ToUnixSeconds(),
                ServiceName = serviceName,
                Healthy = healthy,
                Latency = latency
            });

            // get redis db
            var db = _cn.GetDatabase();

            // set current status
            await db.HashSetAsync(ServiceLastStatusKey, serviceName, healthy);
        }

        public Task<ChartData> GetChartData()
        {
            var now = DateTime.Now;
            var nowUnixEpoch = DateTimeHelpers.ToUnixSeconds(now) - GraphLastSecond;

            // get collection
            var collection = _cnProvider.RedisCollection<MonitoringSnapshot>();

            // query all data
            var query = collection
                .Where(x => x.UnixTimestamp > nowUnixEpoch)
                .ToList();
            var timestamps = query
                .GroupBy(x => x.ServiceName)
                .First()
                .OrderBy(x => x.UnixTimestamp)
                .Select(x => DateTimeHelpers.FromUnixSeconds((long)x.UnixTimestamp))
                .ToList();
            var serviceLatency = query
                .GroupBy(x => x.ServiceName)
                .ToDictionary(x => x.Key, y => y.OrderBy(i => i.UnixTimestamp).Select(p => p.Latency).ToList());

            // sanity check to fill missing data
            foreach (var service in serviceLatency.Keys)
            {
                if (serviceLatency[service].Count != timestamps.Count)
                {
                    var missingCount = timestamps.Count - serviceLatency[service].Count;
                    serviceLatency[service].InsertRange(0, Enumerable.Range(0, missingCount).Select(x => -1));
                }
            }

            return Task.FromResult(new ChartData(timestamps, serviceLatency));
        }

        public async Task<List<ServiceStatus>> GetLatestStatus()
        {
            var db = _cn.GetDatabase();
            var result = await db.HashGetAllAsync(ServiceLastStatusKey);
            return result
                .Select(x => new ServiceStatus(x.Name!, x.Value == "1"))
                .ToList();
        }
    }
}
