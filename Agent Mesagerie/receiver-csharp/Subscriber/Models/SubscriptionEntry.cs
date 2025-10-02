using Broker.V1;
using Grpc.Core;

namespace Subscriber.Models
{
    public class SubscriptionEntry
    {
        public string Subject { get; set; } = string.Empty;
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
        public AsyncServerStreamingCall<Envelope>? Call { get; set; }
        public Task? Task { get; set; }

    }
}
