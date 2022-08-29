using Redis.OM.Modeling;

namespace RedisStatusPage.Core.Entities
{
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
