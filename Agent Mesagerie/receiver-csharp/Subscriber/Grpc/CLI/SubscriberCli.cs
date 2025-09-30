
namespace Subscriber.Grpc.CLI
{
    public class SubscriberCli
    {
        private readonly string[] _args;
        private readonly InputReader _inputReader;
        private readonly CommandProcessor _commandProcessor;

        public SubscriberCli(string[] args)
        {
            _args = args;
            _inputReader = new InputReader();
            _commandProcessor = new CommandProcessor();
        }

        public async Task RunAsync()
        {
            if (!ValidateArguments())
            {
                return;
            }

            var config = await _inputReader.ReadInitialConfigurationAsync();
            if (config == null)
            {
                return;
            }

            var subscriber = new GrpcSubscriberClient(
                config.Host,
                config.Port,
                config.ConsumerGroup,
                config.AutoAck
            );

            if (!await ConnectAndSubscribeAsync(subscriber, config))
            {
                return;
            }

            await RunInteractiveLoopAsync(subscriber);
            await subscriber.Shutdown();
        }

        private bool ValidateArguments()
        {
            if (_args.Length < 2)
            {
                Console.WriteLine($"usage: {Environment.GetCommandLineArgs()[0]} HOST PORT");
                return false;
            }

            if (!int.TryParse(_args[1], out _))
            {
                Console.WriteLine("Invalid port number");
                return false;
            }

            return true;
        }

        private async Task<bool> ConnectAndSubscribeAsync(GrpcSubscriberClient subscriber, SubscriberConfiguration config)
        {
            try
            {
                subscriber.Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
                return false;
            }

            // Subscribe to initial subjects
            if (config.InitialSubjects.Count == 0)
            {
                Console.WriteLine("No initial subjects specified.");
            }
            else
            {
                Console.WriteLine($"Initial subjects: {string.Join(", ", config.InitialSubjects.Select(s => $"\"{s}\""))}");
                foreach (var subject in config.InitialSubjects)
                {
                    await subscriber.SubscribeAsync(subject);
                }
            }

            Console.WriteLine("Type \"help\" for a list of commands.");
            return true;
        }

        private async Task RunInteractiveLoopAsync(GrpcSubscriberClient subscriber)
        {
            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var commandLoop = new CommandLoop(_commandProcessor, subscriber);
            await commandLoop.RunAsync(cts.Token);
        }
    }
}
