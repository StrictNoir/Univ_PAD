using Subscriber.Grpc;
using Subscriber.Grpc.CLI;


namespace Subscriber
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = ReadConfiguration();
            if (config == null)
                return;

            var subscriber = CreateSubscriber(config);

            if (!ConnectSubscriber(subscriber))
                return;

            await SubscribeInitialSubjects(subscriber, config);

            await RunConsoleLoop(subscriber);

            await ShutdownSubscriber(subscriber);
        }

        static SubscriberConfiguration? ReadConfiguration()
        {
            var reader = new InputReader();
            return reader.ReadInitialConfiguration();
        }

        static GrpcSubscriberClient CreateSubscriber(SubscriberConfiguration config)
        {
            return new GrpcSubscriberClient(
                config.Host,
                config.Port
        
            );
        }

        static bool ConnectSubscriber(GrpcSubscriberClient subscriber)
        {
            try
            {
                subscriber.Connect();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
              
                return false;
            }
        }

        static async Task SubscribeInitialSubjects(GrpcSubscriberClient subscriber, SubscriberConfiguration config)
        {
            if (config.InitialSubjects.Count == 0)
            {
                Console.WriteLine("No initial subjects specified.");
                return;
            }

            Console.WriteLine($"Initial subjects: {string.Join(", ", config.InitialSubjects)}");

            foreach (var subject in config.InitialSubjects)
            {
                await subscriber.SubscribeAsync(subject);
            }
        }

        static async Task RunConsoleLoop(GrpcSubscriberClient subscriber)
        {
            Console.WriteLine("Type \"help\" for a list of commands.");

            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (HandleExitCommand(input))
                    break;

                await HandleCommand(input, subscriber);
            }
        }

        static bool HandleExitCommand(string input)
        {
            return input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase);
        }

        static async Task HandleCommand(string input, GrpcSubscriberClient subscriber)
        {
            var parts = input.Split([' '], 2, StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower();
            var argument = parts.Length > 1 ? parts[1].Trim() : null;

            switch (command)
            {
                case "help":
                    PrintHelp();
                    break;

                case "list":
                    subscriber.ListSubscriptions();
                    break;

                case "add":
                    await HandleAddCommand(argument, subscriber);
                    break;

                case "remove":
                    HandleRemoveCommand(argument, subscriber);
                    break;

                default:
                    Console.WriteLine("Unknown command. Type 'help' for instructions.");
                    break;
            }
        }

        static async Task HandleAddCommand(string? subject, GrpcSubscriberClient subscriber)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                Console.WriteLine("Subject cannot be empty.");
                return;
            }

            await subscriber.SubscribeAsync(subject);
        }

        static void HandleRemoveCommand(string? subject, GrpcSubscriberClient subscriber)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                Console.WriteLine("Subject cannot be empty.");
                return;
            }

            if (subject.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                subscriber.UnsubscribeAll();
            }
            else
            {
                subscriber.Unsubscribe(subject);
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine(@"Commands:
      add <subject>    Start subscribing to <subject>.
      remove <subject> Stop subscribing to <subject>. Use ""remove all"" to stop all.
      list             Show currently active subscriptions.
      help             Show this help message.
      exit             Disconnect and exit.");
        }

        static async Task ShutdownSubscriber(GrpcSubscriberClient subscriber)
        {
            Console.WriteLine("Disconnecting...");
            await subscriber.Shutdown();
            Console.WriteLine("Goodbye!");
        }
    }

}
