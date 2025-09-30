using Subscriber;
using System.Net;

class Program
{
    static string ipAddress = "192.168.170.244";
    static int port = 5001;
    static async Task Main(string[] args)
    {
        var subscriber = new SubscriberSocket(ipAddress,port);


        await subscriber.ConnectAsync();

        if (subscriber.IsConnected)
        {
            Console.WriteLine("Subscriber connected. Type commands: (chat.* to subscrie to a topic)");

            while (true)
            {
                string input = Console.ReadLine()?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(input)) continue;

                if (input.StartsWith("chat"))
                {
                    await subscriber.SubscribeAsync(input);
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

}
