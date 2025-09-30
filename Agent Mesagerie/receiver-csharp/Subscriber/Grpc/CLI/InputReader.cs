namespace Subscriber.Grpc.CLI
{
    public class InputReader
    {
        public SubscriberConfiguration? ReadInitialConfiguration()
        {
            // Ask for host
            Console.Write("host> ");
            var host = Console.ReadLine()?.Trim();
            if (IsExitCommand(host) || string.IsNullOrEmpty(host))
            {
                Console.WriteLine("Goodbye!");
                return null;
            }

            // Ask for port
            Console.Write("port> ");
            var portInput = Console.ReadLine()?.Trim();
            if (IsExitCommand(portInput) || string.IsNullOrEmpty(portInput))
            {
                Console.WriteLine("Goodbye!");
                return null;
            }

            if (!int.TryParse(portInput, out int port))
            {
                Console.WriteLine("Invalid port. Using default 50051.");
                port = 50051;
            }

            // Ask for subjects
            Console.Write("subjects (comma separated)> ");
            var subjectsInput = Console.ReadLine();
            if (IsExitCommand(subjectsInput))
            {
                Console.WriteLine("Goodbye!");
                return null;
            }
            var subjects = ParseSubjects(subjectsInput);

            // Ask for consumer group
            Console.Write("consumer group (optional)> ");
            var consumerGroup = Console.ReadLine();
            if (IsExitCommand(consumerGroup))
            {
                Console.WriteLine("Goodbye!");
                return null;
            }

            
            bool autoAck = true;

            return new SubscriberConfiguration
            {
                Host = host!,
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
