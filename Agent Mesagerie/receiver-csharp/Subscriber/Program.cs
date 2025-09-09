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
            Console.WriteLine("Enter subscriber ID (ex: sub-1):");
            string subsriberId = Console.ReadLine() ?? "sub-1";

            string[] subjects = { "order.*" };
            await subscriber.SubscribeAsync(subjects, subsriberId);

            await subscriber.PingAsync();

        }

        Console.ReadLine();
    }
}