using Redis.OM.Modeling;

namespace RedisStatusPage.Core.Entities
{
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
}
