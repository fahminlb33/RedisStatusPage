namespace RedisStatusPage.Services
{
    public class MonitorOptions
    {
        public string RedisUri { get; set; }
        public int ScrapeInterval { get; set; }
        public int GraphLastSeconds { get; set; }
        public List<MonitorServiceOptions> Services { get; set; } = new List<MonitorServiceOptions>();
    }

    public class MonitorServiceOptions
    {
        public string ServiceName { get; set; } = string.Empty;
        public ServiceTestMethod TestMethod { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
    }

    public enum ServiceTestMethod
    {
        TCP,
        HTTP
    }
}
