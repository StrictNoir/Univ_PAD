using Subscriber;
using System.Net;

class Program
{
    static async Task Main(string[] args)
    {
        var subscriber = new SubscriberSocket();
        string ipaddress = "192.168.52.244";


        await subscriber.ConnectAsync(IPAddress.Parse(ipaddress), 5001);

        if (subscriber.IsConnected)
        {
            Console.WriteLine("Subscriber connected. Type commands: (chat.* to subscrie to a topic)");

            while (true)
            {
                string input = Console.ReadLine()?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(input)) continue;

                if (input.StartsWith("chat"))
                {
                    string checkpoint = RequestRecoverCheckpoint();
                    await subscriber.SubscribeAsync(input, checkpoint);
                    Console.WriteLine($"Subscribed to topic {input} from checkpoint {checkpoint}");
                }
                else if (input.ToLower() == "ping")
                {
                    await subscriber.PingAsync();
                }
                else if (input.ToLower() == "exit")
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Unknown command. Type 'ping', a topic like 'chat.*', or 'exit'");
                }
            }
        }
    }

    private static string RequestRecoverCheckpoint()
    {
        Console.WriteLine("Enter a recovery checkpoint: (ex: 42)");
        string checkpoint = Console.ReadLine() ?? string.Empty;
        bool result = int.TryParse(checkpoint, out int parsedValue);

        while(!result || string.IsNullOrEmpty(checkpoint))
        {
            Console.WriteLine("Try to enter an int value.");
            checkpoint = Console.ReadLine() ?? string.Empty;
            result = int.TryParse(checkpoint, out parsedValue);
        }

        return checkpoint;
    }

}
