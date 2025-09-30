
using Broker.V1;
using Google.Protobuf;
using Grpc.Net.Client;

namespace sender;

internal static class Program
{
    private static async Task Main()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        
        Console.Write("address> ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("Please enter a valid broker address.");
            return;
        }
        if (!input.Contains("://")) { input = "http://" + input; }
        var client = CreateClient(input);

        while (true)
        {
            Console.Write("> ");
            var command = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(command)) { continue; }
            switch (command.ToLower())
            {
                case "exit":
                    return;
                case "publish":
                    var envelope = BuildEnvelope();
                    var reply = await client.PublishAsync(envelope);
                    Console.WriteLine($"Publish status: {reply.Accepted}, detail: {reply.Detail}");
                    break;
            }
        }
    }

    private static Envelope BuildEnvelope()
    {
        Console.Write("topic> ");
        var topic = "chat." + Console.ReadLine()?.Trim();

        Console.Write("title> ");
        var title = Console.ReadLine();

        Console.Write("message> ");
        var content = Console.ReadLine();

        var envelope = new Envelope
        {
            Subject = topic,
            Payload = ByteString.CopyFromUtf8(
                System.Text.Json.JsonSerializer.Serialize(new {
                    title,
                    content
                })
            ),
            MessageId = Guid.NewGuid().ToString(),
            TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        envelope.Headers["op"] = "PUBLISH";

        return envelope;
    }
    
    private static Broker.V1.Broker.BrokerClient CreateClient(string address)
    {
        var channel = GrpcChannel.ForAddress(address);
        return new Broker.V1.Broker.BrokerClient(channel);
    }
}