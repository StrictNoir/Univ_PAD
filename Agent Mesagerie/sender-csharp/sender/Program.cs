
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace sender;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var client = new TcpClient();
        string brokerAddress = "0.0.0.0";
        int brokerPort = 5001;

        try
        {
            await client.ConnectAsync(brokerAddress, brokerPort);
        }
        catch
        {
            Console.WriteLine("Could not connect to the broker");
            Environment.Exit(1);
        }
        var stream = client.GetStream();
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
                    var message = new
                    {
                        op = "PING",
                    };
                    string jsonMessage = JsonSerializer.Serialize(message);
                    byte[] data = Encoding.UTF8.GetBytes(jsonMessage);

                    try
                    {
                        await stream.WriteAsync(data, 0, data.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not write to server. Exception: {ex.Message}");
                        Environment.Exit(1);
                    }
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
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Disconnected.");
                    client.Close();
                    Environment.Exit(0);
                }

                string jsonMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
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

    private static async Task Publish(TcpClient client)
    {
        var stream = client.GetStream();
        
        Console.WriteLine("Type");
        var type = Console.ReadLine();
        if (type == null) { Console.WriteLine("Type not specified."); return; }
        
        Console.WriteLine("Title");
        var title = Console.ReadLine();
        
        Console.WriteLine("Content");
        var content = Console.ReadLine();

        var message = new
        {
            op = "PUBLISH",
            message = new
            {
                id = Guid.NewGuid(),
                type,
                payload = new
                {
                    title, 
                    content
                },
                timestamp = DateTime.UtcNow
            }
        };
        string jsonMessage = JsonSerializer.Serialize(message);
        byte[] data = Encoding.UTF8.GetBytes(jsonMessage);

        try
        {
            await stream.WriteAsync(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not write to server. Exception: {ex.Message}");
            client.Close();
            Environment.Exit(1);
        }
    }
}


