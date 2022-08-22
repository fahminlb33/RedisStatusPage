using RedisStatusPage.Core.Services;
using StackExchange.Redis;

Console.WriteLine("Initializing Discord notification server...");

// load settings from environment variable
var redisUri = Environment.GetEnvironmentVariable("REDIS_URI");
if (string.IsNullOrWhiteSpace(redisUri))
{
    Console.WriteLine("REDIS_URI is not set. Exiting...");
    return;
}

var discordWebhookUri = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URI");
if (string.IsNullOrWhiteSpace(discordWebhookUri))
{
    Console.WriteLine("DISCORD_WEBHOOK_URI is not set. Exiting...");
    return;
}

// initialize services
var httpClient = new HttpClient();
var discordService = new DiscordService(httpClient, discordWebhookUri);
var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(redisUri);

// subscribe to channel
var subscriber = connectionMultiplexer.GetSubscriber();
subscriber.Subscribe(PubSubMessage.ChannelName, async (channel, message) =>
{
    // check if message contains a valid data
    if (!message.HasValue)
    {
        Console.WriteLine("Received an empty message. Ignoring...");
        return;
    }

    // deserialize message
    var notification = PubSubMessage.Deserialize(message);
    Console.WriteLine("Received a new notification!");
    Console.WriteLine($"{notification.Timestamp}: {notification.ServiceName} = {notification.Status}");

    try
    {
        // send notification
        await discordService.SendMessage(notification);
    }
    catch (Exception ex)
    {
        // print error
        Console.WriteLine("Error sending message:" + ex.Message);
    }
});

// hook to CTRL+C event
Console.CancelKeyPress += Console_CancelKeyPress;
void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    // dispose all
    Console.WriteLine("Termination signal received, exiting...");
    httpClient.Dispose();
    subscriber.UnsubscribeAll();
    connectionMultiplexer.Close();

    // exit app
    Environment.Exit(0);
}

// wait for keypress and exit
Console.WriteLine("Server started! Listening for messages...");
Console.WriteLine();
Console.WriteLine("Press any key to exit.");
Console.Read();

// dispose all
Console.WriteLine("Unsubscribing...");
httpClient.Dispose();
subscriber.UnsubscribeAll();
connectionMultiplexer.Close();
