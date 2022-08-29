using Redis.OM.Modeling;

namespace RedisStatusPage.Core.Entities
{
    [Document(StorageType = StorageType.Json)]
    public class MonitoringSnapshot
    {
        [Indexed]
        public double UnixTimestamp { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public bool Healthy { get; set; }
        public int Latency { get; set; }
    }
}
