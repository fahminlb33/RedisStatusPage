using Microsoft.Extensions.Options;
using RedisStatusPage.Core;
using RedisStatusPage.Core.Contracts;
using RedisStatusPage.Core.Entities;
using System.Diagnostics;

namespace RedisStatusPage.Services
{
    public class HealthCheckProbe : BackgroundService
    {
        private readonly PeriodicTimer _timer;
        private readonly MonitorOptions _monitorOptions;
        private readonly IStatisticsService _statsService;
        private readonly IIncidentsService _incidentService;
        private readonly INetProbe _prober;

        public HealthCheckProbe(IOptions<MonitorOptions> monitorOptions, IStatisticsService statsService, IIncidentsService incidentService, INetProbe prober)
        {
            _monitorOptions = monitorOptions.Value;
            _timer = new PeriodicTimer(TimeSpan.FromSeconds(_monitorOptions.ScrapeInterval));
            _statsService = statsService;
            _incidentService = incidentService;
            _prober = prober;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken))
            {
                Debug.Print("Running check probe...");

                // get parameters
                var now = DateTime.Now;
                var activeIncidents = await _incidentService.GetActive();

                await Parallel.ForEachAsync(_monitorOptions.Services, async (service, ct) =>
                {
                    Debug.Print($"Running probe: {service.ServiceName} on thread: {Thread.CurrentThread.ManagedThreadId}");

                    // run test
                    ProbeResult result;
                    if (service.TestMethod == ServiceTestMethod.HTTP)
                    {
                        result = await _prober.TestHttp(service.Host);
                    }
                    else
                    {
                        result = await _prober.TestPort(service.Host, service.Port);
                    }

                    Debug.Print($"Probe status: {service.ServiceName}, healthy? {result.Healthy}");

                    // save snapshot
                    await _statsService.Snapshot(now, service.ServiceName, result.Healthy, result.Latency);

                    // check if we can skip this
                    var incident = activeIncidents.FirstOrDefault(x => x.ServiceName == service.ServiceName);
                    if (result.Healthy && incident == null) return;

                    // if current incident is not null and the status is the same, skip updating 
                    var curentStatus = result.Healthy ? IncidentStatus.Resolved : IncidentStatus.Reported;
                    if (incident != null && curentStatus == incident.LastStatus) return;

                    // save new incident
                    if (!result.Healthy)
                    {
                        incident = new Incident
                        {
                            UnixTimestamp = DateTimeHelpers.ToUnixSeconds(now),
                            LastStatus = IncidentStatus.Reported,
                            ServiceName = service.ServiceName,
                            History = new List<IncidentResponse>()
                            {
                                 new IncidentResponse
                                 {
                                     UnixTimestamp = DateTimeHelpers.ToUnixSeconds(now),
                                     Status = IncidentStatus.Reported,
                                     Message = "Service is DOWN"
                                 }
                            }
                        };
                        await _incidentService.Add(incident);
                    }
                    else
                    {
                        // assert incident is not null (to suppress Roslyn warning)
                        Debug.Assert(incident != null);

                        // update incident
                        incident.LastStatus = IncidentStatus.Resolved;
                        incident.History.Add(new IncidentResponse
                        {
                            UnixTimestamp = DateTimeHelpers.ToUnixSeconds(now),
                            Status = IncidentStatus.Resolved,
                            Message = "Service is UP"
                        });

                        await _incidentService.Update(incident);
                    }

                    // publish
                    await _incidentService.Publish(incident);
                });
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _timer?.Dispose();
        }
    }
}
