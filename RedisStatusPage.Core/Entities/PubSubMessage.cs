using System.Text.Json;

namespace RedisStatusPage.Core.Entities
{
    public class PubSubMessage
    {
        public const string ChannelName = "NOTIFICATION_CHAN";

        public DateTime Timestamp { get; set; }
        public string ServiceName { get; set; }
        public string Status { get; set; }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static PubSubMessage Deserialize(string message)
        {
            return JsonSerializer.Deserialize<PubSubMessage>(message);
        }

        public override string ToString()
        {
            return Serialize();
        }
    }
}
