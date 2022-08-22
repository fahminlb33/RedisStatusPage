using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RedisStatusPage.Core
{
    public record ProbeResult(bool Healthy, int Latency);

    public interface INetProbe : IDisposable
    {
        Task<ProbeResult> TestHttp(string uri);
        Task<ProbeResult> TestPort(string host, int port);
    }

    public class NetProbe : INetProbe
    {
        private readonly HttpClient _httpClient;
        private bool _disposedValue;

        public NetProbe(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ProbeResult> TestHttp(string uri)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await _httpClient.GetAsync(uri);
                stopwatch.Stop();

                return new ProbeResult(result.IsSuccessStatusCode, (int)stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                Debug.Print(ex.ToString());
                return new ProbeResult(false, (int)stopwatch.ElapsedMilliseconds);
            }
        }

        public async Task<ProbeResult> TestPort(string host, int port)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(host, port);
                stopwatch.Stop();

                return new ProbeResult(true, (int)stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                Debug.Print(ex.ToString());
                return new ProbeResult(false, (int)stopwatch.ElapsedMilliseconds);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _httpClient.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
