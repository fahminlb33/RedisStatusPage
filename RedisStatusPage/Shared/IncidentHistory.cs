namespace RedisStatusPage.Shared
{
    public class IncidentHistory
    {
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
}
