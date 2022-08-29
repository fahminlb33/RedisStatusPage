using RedisStatusPage.Core.Entities;

namespace RedisStatusPage.Core.Contracts
{
    public interface IStatisticsService
    {
        int GraphLastSecond { get; set; }

        Task CreateIndexIfNotExists();
        Task<ChartData> GetChartData();
        Task<MonitoringStatistics> GetDashboard();
        Task<List<ServiceStatus>> GetLatestStatus();
        Task Snapshot(DateTime timestamp, string serviceName, bool healthy, int latency);
    }
}
