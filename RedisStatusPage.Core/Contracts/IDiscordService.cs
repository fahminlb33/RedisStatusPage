using RedisStatusPage.Core.Entities;

namespace RedisStatusPage.Core.Contracts
{
    public interface IDiscordService
    {
        Task SendMessage(PubSubMessage message);
    }
}
