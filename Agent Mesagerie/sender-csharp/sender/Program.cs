using Broker.V1;
using Google.Protobuf;
using Grpc.Net.Client;

namespace sender;

internal static class Program
{
    private static async Task Main()
    {
        Console.Write("address> ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("Please enter a valid address.");
            return;
        }
        
        if (!input.Contains("://"))
        {
            input = "http://" + input;
        }

        var client = CreateClient(input);

        while (true)
        {
            Console.Write("> ");
            var command = Console.ReadLine();
            switch (command)
            {
                case "exit":
                    return;
                case "publish":
                    Console.Write("topic> ");
                    var topic = Console.ReadLine()?.Trim();

                    Console.Write("message> ");
                    var message = Console.ReadLine();
                    
                    var envelope = new Envelope
                    {
                        Subject = topic,
                        Payload = ByteString.CopyFromUtf8(message),
                        TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };
                    envelope.Headers["op"] = "PUBLISH";
                    
                    var reply = await client.PublishAsync(envelope);
                    Console.WriteLine($"Publish status: \naccepted: {reply.Accepted}" 
                                      + (string.IsNullOrWhiteSpace(reply.Detail) ? "" : $"\ndetail: {reply.Detail}"));
                    break;
            }
        }
    }
    
    static Broker.V1.Broker.BrokerClient CreateClient(string address)
    {
        var channel = GrpcChannel.ForAddress(address);
        return new Broker.V1.Broker.BrokerClient(channel);
    }
}