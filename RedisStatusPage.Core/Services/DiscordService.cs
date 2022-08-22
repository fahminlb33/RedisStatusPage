using System.Text.Json;

namespace RedisStatusPage.Core.Services
{
    public interface IDiscordService
    {
        Task SendMessage(PubSubMessage message);
    }

    public class DiscordService : IDiscordService
    {
        private readonly HttpClient _httpClient;
        private readonly string _webhookUrl;

        public DiscordService(HttpClient httpClient, string webhookUrl)
        {
            _httpClient = httpClient;
            _webhookUrl = webhookUrl;
        }

        public async Task SendMessage(PubSubMessage message)
        {
            var body = new
            {
                content = $"Service health alert! 🔥\n**{message.ServiceName}** health changed to **{message.Status}**."
            };
            var content = new StringContent(JsonSerializer.Serialize(body));
            var result = await _httpClient.PostAsync(_webhookUrl, content);
            result.EnsureSuccessStatusCode();
        }
    }
}
