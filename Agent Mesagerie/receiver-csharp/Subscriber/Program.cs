using Subscriber;
using System.Net;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Subscriber starting...");

        var subscriber = new SubscriberSocket();

        await subscriber.ConnectAsync(IPAddress.Loopback, 5000);

        if (subscriber.IsConnected)
        {
            string topic = RequestTopicFromUser();
            string checkpoint = RequestRecoverCheckpoint();

            await subscriber.SubscribeAsync(topic, checkpoint);

            await subscriber.PingAsync();

        }

        Console.ReadLine();
    }
    private static string RequestTopicFromUser()
    {
        Console.WriteLine("Enter topic: (like this chat.*)");
        string topic = Console.ReadLine() ?? string.Empty;

        while (string.IsNullOrEmpty(topic) || !topic.StartsWith("chat"))
        {
            Console.WriteLine("Enter topic again.");
            topic = Console.ReadLine() ?? string.Empty;
        }
        return topic;
    }
    private static string RequestRecoverCheckpoint()
    {
        Console.WriteLine("Enter a recovery checkpoint: (ex: 42)");
        string checkpoint = Console.ReadLine() ?? string.Empty;

        if (string.IsNullOrEmpty(checkpoint)) checkpoint = "1";

        return checkpoint;

    }
}