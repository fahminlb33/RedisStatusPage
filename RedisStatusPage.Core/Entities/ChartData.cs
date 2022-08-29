namespace RedisStatusPage.Core.Entities
{
    public record ChartData(List<DateTime> Timestamps, Dictionary<string, List<int>> ServiceLatency);
}
