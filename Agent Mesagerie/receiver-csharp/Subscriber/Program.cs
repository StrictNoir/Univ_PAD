using Subscriber;


class Program
{
    static string ipAddress = "127.0.0.1";
    static int port = 5000;

    static async Task Main(string[] args)
    {
        SubscriberSocket? subscriber = null;

        Console.WriteLine("Type 'connect' to connect to broker, 'disconnect' to disconnect, 'exit' to quit.");

        while (true)
        {
            string input = Console.ReadLine()?.Trim().ToLower() ?? string.Empty;

            if (string.IsNullOrEmpty(input))
                continue;

            switch (input)
            {
                case "connect":
                    if (subscriber != null && subscriber.IsConnected)
                    {
                        Console.WriteLine("Already connected.");
                        break;
                    }

                    subscriber = new SubscriberSocket(ipAddress, port);
                    await subscriber.ConnectAsync();
                    if (subscriber.IsConnected)
                    {
                        Console.WriteLine("Subscriber connected. Type commands: chat.*, ping, disconnect, exit");
                    }
                    break;

                case "disconnect":
                    if (subscriber != null && subscriber.IsConnected)
                    {
                        subscriber.Close();
                        Console.WriteLine("Subscriber disconnected.");
                    }
                    else
                    {
                        Console.WriteLine("Subscriber is not connected.");
                    }
                    break;

                case "ping":
                    if (subscriber != null && subscriber.IsConnected)
                    {
                        await subscriber.PingAsync();
                    }
                    else
                    {
                        Console.WriteLine("Not connected. Use 'connect' first.");
                    }
                    break;

                case "exit":
                    if (subscriber != null && subscriber.IsConnected)
                        subscriber.Close();
                    return;

                default:
                    if (input.StartsWith("chat"))
                    {
                        if (subscriber != null && subscriber.IsConnected)
                        {
                            await subscriber.SubscribeAsync(input);
                        }
                        else
                        {
                            Console.WriteLine("Not connected. Use 'connect' first.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unknown command. Type 'connect', 'disconnect', 'ping', chat.*, or 'exit'");
                    }
                    break;
            }
        }
    }
}
