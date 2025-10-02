
namespace Subscriber.Grpc.CLI
{
    public class CommandLoop
    {
        private readonly CommandProcessor _commandProcessor;
        private readonly GrpcSubscriberClient _subscriber;

        public CommandLoop(CommandProcessor commandProcessor, GrpcSubscriberClient subscriber)
        {
            _commandProcessor = commandProcessor;
            _subscriber = subscriber;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.Write("command> ");

                var inputTask = Task.Run(() => Console.ReadLine(), cancellationToken);

                try
                {
                    var input = await inputTask;

                    if (input == null || InputReader.IsExitCommand(input))
                    {
                        break;
                    }

                    input = input.Trim();
                    if (string.IsNullOrEmpty(input))
                    {
                        continue;
                    }

                    var shouldExit = await _commandProcessor.ProcessCommandAsync(input, _subscriber);
                    if (shouldExit)
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
