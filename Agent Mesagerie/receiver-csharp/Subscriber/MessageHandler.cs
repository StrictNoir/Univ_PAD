

using System.Text.Json;

namespace Subscriber
{
    public class MessageHandler
    {
        private readonly string _address;
        private readonly int _port;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public MessageHandler(string address, int port)
        {
            _address = address;
            _port = port;
        }

        public void HandleMessage(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                string? op = doc.RootElement.GetProperty("op").GetString();

                switch (op)
                {
                    case "DELIVER":
                        HandleDeliveryMessage(json);
                        break;
                    case "SUBSCRIBED":
                        HandleSubscribedMessage(json);
                        break;
                    case "PONG":
                        Console.WriteLine("Received PONG response from broker");
                        break;
                    default:
                        Console.WriteLine($"Received unknown op: {op}");
                        Console.WriteLine($"Raw JSON: {json}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse frame: {ex.Message}");
            }
        }

        private void HandleDeliveryMessage(string json)
        {
            try
            {
                var delivery = JsonSerializer.Deserialize<DeliveryMessage>(json, _jsonOptions);
                if (delivery == null) return;

                Console.WriteLine($"{delivery.Topic}: {JsonSerializer.Serialize(delivery.Message)}");

                if (!string.IsNullOrEmpty(delivery.StoreId))
                {
                    CheckpointCreator.SaveCheckpoint(_address, _port, delivery.Topic, delivery.StoreId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse DeliveryMessage: {ex.Message}");
            }
        }

        private void HandleSubscribedMessage(string json)
        {
            Console.WriteLine($"=== SUBSCRIBED RESPONSE ===");
            Console.WriteLine($"Raw JSON: {json}");

            try
            {
                var subscribed = JsonSerializer.Deserialize<SubscribedMessage>(json, _jsonOptions);
                if (subscribed == null)
                {
                    Console.WriteLine("Could not subscribe - null response");
                }
                else
                {
                    Console.WriteLine($"Successfully subscribed to: {subscribed.Topic}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse SUBSCRIBED message: {ex.Message}");
            }
        }
    }
}
