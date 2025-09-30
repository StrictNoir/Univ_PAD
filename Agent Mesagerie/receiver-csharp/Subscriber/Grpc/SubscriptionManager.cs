
using System.Collections.Concurrent;

namespace Subscriber.Grpc
{
    public class SubscriptionManager
    {
        private readonly ConcurrentDictionary<string, SubscriptionEntry> _activeSubscriptions;

        public SubscriptionManager()
        {
            _activeSubscriptions = new ConcurrentDictionary<string, SubscriptionEntry>();
        }

        public bool HasSubscription(string subject)
        {
            return _activeSubscriptions.ContainsKey(subject);
        }
        public SubscriptionEntry CreateSubscription(string subject)
        {
            var entry = new SubscriptionEntry
            {
                Subject = subject,
                CancellationTokenSource = new CancellationTokenSource()
            };

            if (!_activeSubscriptions.TryAdd(subject, entry))
            {
                throw new InvalidOperationException($"Subscription for {subject} already exists");
            }

            return entry;
        }
        public bool RemoveSubscription(string subject)
        {
            return _activeSubscriptions.TryRemove(subject, out _);
        }

        public bool RemoveSubscription(string subject, out SubscriptionEntry? entry)
        {
            return _activeSubscriptions.TryRemove(subject, out entry);
        }

        public List<string> GetAllSubscriptions()
        {
            return _activeSubscriptions.Keys.ToList();
        }
    }
}
