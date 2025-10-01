
using Broker.V1;
using System.Text;

namespace Subscriber.Grpc
{
    public class MessageHandler
    {

        public void HandleEnvelope(Envelope envelope)
        {
            PrintEnvelope(envelope);
        }

        private void PrintEnvelope(Envelope envelope)
        {
            var payload = Encoding.UTF8.GetString(envelope.Payload.ToByteArray());

            Console.WriteLine($"RECEIVED subject={envelope.Subject} message_id={envelope.MessageId}");

            if (string.IsNullOrEmpty(payload))
            {
                Console.WriteLine("  (empty payload)");
            }
            else
            {
                Console.WriteLine($"  payload: {payload}");
            }

            if (envelope.Headers != null && envelope.Headers.Count > 0)
            {
                Console.WriteLine("  headers:");
                foreach (var header in envelope.Headers)
                {
                    Console.WriteLine($"    {header.Key}: {header.Value}");
                }
            }
        }
    }
}
