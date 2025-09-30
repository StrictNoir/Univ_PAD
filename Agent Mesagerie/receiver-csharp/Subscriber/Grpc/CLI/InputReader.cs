namespace Subscriber.Grpc.CLI
{
    public class InputReader
    {
        private const int DefaultPort = 50051;

        public SubscriberConfiguration? ReadInitialConfiguration()
        {
            var host = ReadHost();
            if (host == null) return null;

            var port = ReadPort();
            if (port == null) return null;

            var subjects = ReadSubjects();
            if (subjects == null) return null;

            var consumerGroup = ReadConsumerGroup();
            if (consumerGroup == null) return null;

            return new SubscriberConfiguration
            {
                Host = host,
                Port = port.Value,
                ConsumerGroup = consumerGroup,
                AutoAck = true,
                InitialSubjects = subjects
            };
        }

        private string? ReadHost()
        {
            Console.Write("host> ");
            var input = Console.ReadLine()?.Trim();

            if (IsExitCommand(input))
            {
                Console.WriteLine("Goodbye!");
                return null;
            }

            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("Host cannot be empty.");
                return null;
            }

            return input;
        }

        private int? ReadPort()
        {
            Console.Write("port> ");
            var input = Console.ReadLine()?.Trim();

            if (IsExitCommand(input))
            {
                Console.WriteLine("Goodbye!");
                return null;
            }

            if (string.IsNullOrEmpty(input) || !int.TryParse(input, out int port))
            {
                Console.WriteLine($"Invalid port. Using default {DefaultPort}.");
                return DefaultPort;
            }

            if (port < 1 || port > 65535)
            {
                Console.WriteLine($"Port out of range. Using default {DefaultPort}.");
                return DefaultPort;
            }

            return port;
        }

        private List<string>? ReadSubjects()
        {
            Console.Write("subjects (comma separated)> ");
            var input = Console.ReadLine();

            if (IsExitCommand(input))
            {
                Console.WriteLine("Goodbye!");
                return null;
            }

            return ParseSubjects(input);
        }

        private string? ReadConsumerGroup()
        {
            Console.Write("consumer group (optional)> ");
            var input = Console.ReadLine();

            if (IsExitCommand(input))
            {
                Console.WriteLine("Goodbye!");
                return null;
            }

            return input?.Trim() ?? "";
        }

        private List<string> ParseSubjects(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<string>();

            return input
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList();
        }

        private static bool IsExitCommand(string? input)
        {
            return input?.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }
}