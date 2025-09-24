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

        await ConnectToBroker(client);        

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
                    break;
                case "publish":
                    await Publish(client);
                    break;
                case "connect":
                    await ConnectToBroker(client);
                    break;
            }
        }
    }
    
    private static async Task ReceiveMessages(TcpClient client)
    {
        var stream = client.GetStream();

        while (true)
        {
            try
            {
                var lengthBytes = await ReadExact(stream, 4);
                var len = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(lengthBytes, 0));

                if (len == 0) break;

                var bodyBytes = await ReadExact(stream, len);
                var jsonMessage = Encoding.UTF8.GetString(bodyBytes);
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
            catch (IOException)
            {
                Console.WriteLine("Disconnected.");
                client.Close();
                Environment.Exit(0);
            }
        }
    }

    private static async Task<byte[]> ReadExact(Stream stream, int length)
    {
        var buffer = new byte[length];
        var totalBytesRead = 0;

        while (totalBytesRead < length)
        {
            var bytesRead = await stream.ReadAsync(buffer, totalBytesRead, length - totalBytesRead);
            if (bytesRead == 0)
            {
                throw new EndOfStreamException();
            }
            totalBytesRead += bytesRead;
        }
        return buffer;
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
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Welcome to the Message Publisher!");
        Console.WriteLine("Please provide the following details to publish your message:");

        
        // Get Topic
        Console.Write("Enter the topic: ");
        Console.ForegroundColor = ConsoleColor.White;

        var topic = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(topic))
        {
            Console.WriteLine("Error: Topic not specified. Please try again.");
            return;
        }
        topic = "chat." + topic.Trim();

        
        // Get Title
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("Enter the title: ");
        Console.ForegroundColor = ConsoleColor.White;

        var title = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(title))
        {
            Console.WriteLine("Error: Title not specified. Please try again.");
            return;
        }
        

        // Get Content
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("Enter the content: ");
        Console.ForegroundColor = ConsoleColor.White;
        var content = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(content))
        {
            Console.WriteLine("Error: Content not specified. Please try again.");
            return;
        }

        
        // Create the message object
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
        
        try
        {
            await stream.WriteAsync(len, 0, len.Length);
            await stream.WriteAsync(body, 0, body.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not write to server. Exception: {ex.Message}");
        }
        Console.WriteLine("Message published.");
    }

    private static async Task ConnectToBroker(TcpClient client)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("Enter broker IP: ");
        Console.ForegroundColor = ConsoleColor.White;
        string brokerAddress = Console.ReadLine();

        if (!IPAddress.TryParse(brokerAddress, out _))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid IP address.");
            Console.ForegroundColor = ConsoleColor.White;
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("Enter broker port: ");
        Console.ForegroundColor = ConsoleColor.White;
        string portInput = Console.ReadLine();

        if (!int.TryParse(portInput, out int brokerPort) || brokerPort < 1 || brokerPort > 65535)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid port number.");
            Console.ForegroundColor = ConsoleColor.White;
            return;
        }
        
        try
        {
            await client.ConnectAsync(brokerAddress, brokerPort);
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Could not connect to the broker");
            Console.ForegroundColor = ConsoleColor.White;
        }
        Console.WriteLine("Connected to server.");
    }
}