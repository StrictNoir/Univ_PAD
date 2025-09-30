
namespace Subscriber.Grpc.CLI
{
    public class InputReader
    {
        public async Task<SubscriberConfiguration?> ReadInitialConfigurationAsync()
        {
            Console.WriteLine("Type \"exit\" at any prompt to quit.");

            // Get subjects
            Console.Write("subjects (comma separated)> ");
            var subjectsInput = Console.ReadLine();
            if (IsExitCommand(subjectsInput))
            {
                Console.WriteLine("Goodbye!");
                return null;
            }

            var subjects = ParseSubjects(subjectsInput);

            // Get consumer group
            Console.Write("consumer group (optional)> ");
            var consumerGroup = Console.ReadLine();
            if (IsExitCommand(consumerGroup))
            {
                Console.WriteLine("Goodbye!");
                return null;
            }

            // Get auto-ack preference
            Console.Write("auto-ack? [y/N]> ");
            var autoAckInput = Console.ReadLine();
            if (IsExitCommand(autoAckInput))
            {
                Console.WriteLine("Goodbye!");
                return null;
            }

            bool autoAck = autoAckInput?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) ?? false;

            var args = Environment.GetCommandLineArgs();
            var host = args.Length > 1 ? args[1] : "localhost";
            var port = args.Length > 2 && int.TryParse(args[2], out int p) ? p : 50051;

            return new SubscriberConfiguration
            {
                Host = host,
                Port = port,
                ConsumerGroup = consumerGroup ?? "",
                AutoAck = autoAck,
                InitialSubjects = subjects
            };
        }

        private List<string> ParseSubjects(string? input)
        {
            return (input ?? "")
                .Replace(',', ' ')
                .Split([' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Distinct()
                .ToList();
        }

        public static bool IsExitCommand(string? input)
        {
            return input?.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }
}
