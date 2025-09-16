
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace sender;

internal static class Program
{
    private static async Task Main()
    {
        var client = new TcpClient();
        var brokerAddress = "192.168.60.244";
        var brokerPort = 5001;

        try
        {
            await client.ConnectAsync(brokerAddress, brokerPort);
        }
        catch
        {
            Console.WriteLine("Could not connect to the broker");
            Environment.Exit(1);
        }
        Console.WriteLine("Connected to server.");

        _ = ReceiveMessages(client);

        while (true)
        {
            var command = Console.ReadLine();
            switch (command)
            {
                case "exit": 
                    Environment.Exit(0);
                    break;
                case "ping":
                    await Ping(client);
                    Console.WriteLine("Sent ping message.");
                    break;
                case "publish":
                    await Publish(client);
                    Console.WriteLine("Message published.");
                    break;
            }
        }
    }
    
    private static async Task ReceiveMessages(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();

            while (true)
            {
                var buffer = new byte[4];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Disconnected.");
                    client.Close();
                    Environment.Exit(0);
                }

                var jsonMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var receivedMessage = JsonNode.Parse(jsonMessage);

                switch (receivedMessage?["op"]?.ToString())
                {
                    case "PONG":
                        Console.WriteLine("PONG");
                        break;
                    case "ERROR":
                        Console.WriteLine($"Operation: {receivedMessage["op"]}");
                        Console.WriteLine($"Code: {receivedMessage["code"]}");
                        Console.WriteLine($"Detail: {receivedMessage["detail"]}");
                        break;
                    default:
                        Console.WriteLine($"Unknown message: {receivedMessage}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection error: {ex.Message}");
            client.Close();
            Environment.Exit(1);
        }
    }

    private static async Task Ping(TcpClient client)
    {
        var stream = client.GetStream();
        var message = new
        {
            op = "PING",
        };
                    
        var jsonMessage = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(jsonMessage);
        var len = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(body.Length));
                    
        try
        {
            await stream.WriteAsync(len, 0, len.Length);
            await stream.WriteAsync(body, 0, body.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not write to server. Exception: {ex.Message}");
            client.Close();
            Environment.Exit(1);
        }
    }

    private static async Task Publish(TcpClient client)
    {
        var stream = client.GetStream();
        
        Console.WriteLine("Topic");
        var topic = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(topic)) { Console.WriteLine("Topic not specified."); return; }
        
        Console.WriteLine("Title");
        var title = Console.ReadLine();
        
        Console.WriteLine("Content");
        var content = Console.ReadLine();
        
        var message = new
        {
            op = "PUBLISH",
            topic,
            message = new
            {
                title, 
                content
            }
        };
        var jsonMessage = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(jsonMessage);
        var len = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(body.Length));

        Console.WriteLine(jsonMessage);

        try
        {
            await stream.WriteAsync(len, 0, len.Length);
            await stream.WriteAsync(body, 0, body.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not write to server. Exception: {ex.Message}");
            client.Close();
            Environment.Exit(1);
        }
    }
}


