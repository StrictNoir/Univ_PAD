namespace Subscriber.Grpc.CLI
{
    public class CommandProcessor
    {
        public async Task<bool> ProcessCommandAsync(string input, GrpcSubscriberClient subscriber)
        {
            var parts = input.Split([' '], 2, StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower();
            var argument = parts.Length > 1 ? parts[1].Trim() : null;

            switch (command)
            {
                case "exit":
                    return true;
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
                case "ack":
                    await HandleAckCommand(argument, subscriber);
                    break;
                case "pending":
                    subscriber.ListPendingAcknowledgments();
                    break;
                default:
                    Console.WriteLine("Unknown command. Type \"help\" for a list of commands.");
                    break;
            }
            return false;
        }

        private async Task HandleAddCommand(string? subject, GrpcSubscriberClient subscriber)
        {
            if (string.IsNullOrEmpty(subject))
            {
                Console.WriteLine("Subject cannot be empty.");
            }
            else
            {
                await subscriber.SubscribeAsync(subject);
            }
        }

        private void HandleRemoveCommand(string? subject, GrpcSubscriberClient subscriber)
        {
            if (string.IsNullOrEmpty(subject))
            {
                Console.WriteLine("Subject cannot be empty.");
            }
            else if (subject.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                subscriber.UnsubscribeAll();
            }
            else
            {
                subscriber.Unsubscribe(subject);
            }
        }

        private async Task HandleAckCommand(string? messageId, GrpcSubscriberClient subscriber)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                Console.WriteLine("Message ID cannot be empty. Usage: ack <message_id>");
            }
            else
            {
                await subscriber.ManualAcknowledgeAsync(messageId);
            }
        }

        private void PrintHelp()
        {
            Console.WriteLine(@"Commands:
  add <subject>       Start subscribing to <subject>.
  remove <subject>    Stop subscribing to <subject>. Use ""remove all"" to stop all.
  list                Show currently active subscriptions.
  ack <message_id>    Manually acknowledge a message (when auto-ack is disabled).
  pending             Show all pending acknowledgments.
  help                Show this help message.
  exit                Disconnect and exit.");
        }
    }
}