using RedisStatusPage.Core.Contracts;
using RedisStatusPage.Core.Entities;
using StackExchange.Redis;

namespace RedisStatusPage.Core.Services
{
    public class StatisticsServiceV2 : IStatisticsService
    {
        private const string FirstStartupKey = "STATISTICS:FIRST_STARTUP";    // string
        private const string ServiceLastStatusKey = "STATISTICS:LAST_STATUS"; // hash
        private const string ServicesSetKey = "STATISTICS:SERVICES";          // set
        private const string TimestampKey = "STATISTICS:{0}:TIMESTAMPS:{1}";  // list
        private const string HealthKey = "STATISTICS:{0}:HEALTH:{1}";         // list
        private const string LatencyKey = "STATISTICS:{0}:LATENCY:{1}";       // list

        private readonly ConnectionMultiplexer _cn;

        public StatisticsServiceV2(ConnectionMultiplexer cn)
        {
            _cn = cn;
        }

        public int GraphLastSecond { get; set; } = 900;

        public async Task CreateIndexIfNotExists()
        {
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
            // get today date
            var timestampDate = timestamp.ToString("yyyy-MM-dd");

            // save to redis using OM
            var timestampKey = string.Format(TimestampKey, serviceName, timestampDate);
            var latencyKey = string.Format(LatencyKey, serviceName, timestampDate);
            var healthKey = string.Format(HealthKey, serviceName, timestampDate);

            // get redis db
            var db = _cn.GetDatabase();

            // set current status
            await db.HashSetAsync(ServiceLastStatusKey, serviceName, healthy);

            // add to set
            await db.SetAddAsync(ServicesSetKey, serviceName);

            // add timestamp, health, and latency status
            await db.ListRightPushAsync(timestampKey, DateTimeHelpers.ToUnixSeconds(timestamp));
            await db.ListRightPushAsync(latencyKey, latency);
            await db.ListRightPushAsync(healthKey, healthy);
        }

        public async Task<ChartData> GetChartData()
        {
            // get today timestamp
            var now = DateTime.Now;
            var nowDate = now.ToString("yyyy-MM-dd");

            // get redis db
            var db = _cn.GetDatabase();

            // get all available services
            var services = (await db.SetMembersAsync(ServicesSetKey))
                .Select(x => x.ToString())
                .ToList();

            // query all data
            var timestampKey = string.Format(TimestampKey, services.First(), nowDate);
            var timestampValues = await db.ListRangeAsync(timestampKey);
            var timestamps = timestampValues
                .Select(x => DateTimeHelpers.FromUnixSeconds(Convert.ToInt64(x)))
                .ToList();

            // get latency data from all services
            var latencyDict = new Dictionary<string, List<int>>();
            foreach (var service in services)
            {
                // get latency history
                var latencyKey = string.Format(LatencyKey, service, nowDate);
                var latencyHistory = (await db.ListRangeAsync(latencyKey))
                    .Select(x => Convert.ToInt32(x))
                    .ToList();

                // sanity check to fill missing data/remove too much data
                // not the best solution, but necessary to keep the X-axis
                // of the chart aligned perfectly
                // more appropiate way might be using interpolation instead of this hack
                if (latencyHistory.Count > timestamps.Count)
                {
                    var removeCount = latencyHistory.Count - timestamps.Count;
                    latencyHistory.RemoveRange(0, removeCount);
                }
                else if (latencyHistory.Count < timestamps.Count)
                {
                    var missingCount = timestamps.Count - latencyHistory.Count;
                    var fillerData = Enumerable.Range(0, missingCount).Select(x => -1);
                    latencyHistory.InsertRange(0, fillerData);
                }

                // add to dict
                latencyDict.Add(service, latencyHistory);
            }

            // return data
            return new ChartData(timestamps, latencyDict);
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
