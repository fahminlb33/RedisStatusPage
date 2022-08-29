namespace RedisStatusPage.Core.Entities
{
    public record MonitoringStatistics
    {
        public int ServiceCount { get; set; }
        public int ReadyCount { get; set; }
        public int UnreachableCount { get; set; }
        public DateTime FirstStartup { get; set; }
        public TimeSpan Uptime => DateTime.Now - FirstStartup;

        public static readonly MonitoringStatistics Empty = new MonitoringStatistics
        {
            ServiceCount = 0,
            ReadyCount = 0,
            UnreachableCount = 0,
            FirstStartup = DateTime.Now
        };
    }
}
