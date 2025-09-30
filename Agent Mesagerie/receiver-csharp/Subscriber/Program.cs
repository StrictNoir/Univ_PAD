using DotNetEnv;
using Subscriber;
using System.Net;

class Program
{

    
    static async Task Main(string[] args)
    {
        Env.Load();
        string ipAddress = Environment.GetEnvironmentVariable("BROKER_IP") ?? "127.0.0.1";
        string portStr = Environment.GetEnvironmentVariable("BROKER_PORT") ?? "5001";

        if (!int.TryParse(portStr, out int port))
        {
            Console.WriteLine("Invalid port in .env, using default 5001");
            port = 5001;
        }

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
