using Subscriber.Models;

namespace Subscriber.Grpc.CLI
{
    public class InputReader
    {
        public SubscriberConfiguration? ReadInitialConfiguration()
        {
            Console.WriteLine("=== gRPC Subscriber Configuration ===");
            Console.WriteLine("Type \"exit\" at any prompt to quit.");
            Console.WriteLine();

            // Get host
            Console.Write("host [localhost]> ");
            var hostInput = Console.ReadLine();
            if (IsExitCommand(hostInput))
            {
                Console.WriteLine("Goodbye!");
                return null;
            }
            var host = string.IsNullOrWhiteSpace(hostInput) ? "localhost" : hostInput.Trim();

            // Get port
            Console.Write("port [5000]> ");
            var portInput = Console.ReadLine();
            if (IsExitCommand(portInput))
            {
                Console.WriteLine("Goodbye!");
                return null;
            }
            int port = 5000;
            if (!string.IsNullOrWhiteSpace(portInput) && !int.TryParse(portInput.Trim(), out port))
            {
                Console.WriteLine("Invalid port number, using default: 500");
                port = 5000;
            }

            // Get subjects
            Console.Write("subjects (comma separated, optional)> ");
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

            Console.WriteLine();

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
