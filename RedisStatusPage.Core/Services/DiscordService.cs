using System.Text.Json;
using RedisStatusPage.Core.Contracts;
using RedisStatusPage.Core.Entities;

namespace RedisStatusPage.Core.Services
{
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
                content = $"Service health alert! 🔥\n**{message.ServiceName}** health changed to **{message.Status}**.",
                embeds = Array.Empty<string>(),
                attachments = Array.Empty<string>()
            };
            
            var bodyString = JsonSerializer.Serialize(body);
            var content = new StringContent(bodyString, System.Text.Encoding.UTF8, "application/json");
            var result = await _httpClient.PostAsync(_webhookUrl, content);
            result.EnsureSuccessStatusCode();
        }
    }
}
