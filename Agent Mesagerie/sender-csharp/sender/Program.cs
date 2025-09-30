using Grpc.Net.Client;
using Sender;

namespace sender;

internal static class Program
{
    private static async Task Main()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        
        Console.Write("Enter broker address (e.g. http://localhost:5001): ");
        var address = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(address))
        {
            Console.WriteLine("Please enter a valid broker address.");
            return;
        }

        using var channel = GrpcChannel.ForAddress(address);
        var client = new Broker.BrokerClient(channel);

        while (true)
        {
            var command = Console.ReadLine();
            switch (command)
            {
                case "exit":
                    return;
                case "ping":
                    var pong = await client.PingAsync(new PingRequest());
                    Console.WriteLine(pong.Message);
                    break;
                case "publish":
                    Console.Write("Enter topic: ");
                    var topic = "chat." + Console.ReadLine()?.Trim();

                    Console.Write("Enter title: ");
                    var title = Console.ReadLine();

                    Console.Write("Enter content: ");
                    var content = Console.ReadLine();

                    var reply = await client.PublishAsync(new PublishRequest
                    {
                        Topic = topic,
                        Title = title,
                        Content = content
                    });

                    Console.WriteLine($"Publish status: {reply.Status}");
                    break;
            }
        }
    }
}