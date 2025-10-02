

using Subscriber.Models;
using System.Collections.Concurrent;

namespace Subscriber.Grpc
{
    public static class ManualAcknowledgeHandler
    {
        private static readonly ConcurrentQueue<PendingMessage> _pendingMessages = new();
        private static readonly ConcurrentDictionary<string, PendingMessage> _messageIndex = new();

        public static void AddPendingMessage(string subject, string messageId)
        {
            var pending = new PendingMessage
            {
                Subject = subject,
                MessageId = messageId,
                ReceivedAt = DateTime.UtcNow
            };

            _pendingMessages.Enqueue(pending);
            _messageIndex.TryAdd(messageId, pending);
        }

        public static bool TryGetMessage(string messageId, out PendingMessage? message)
        {
            return _messageIndex.TryGetValue(messageId, out message);
        }

        public static void RemoveAcknowledged(string messageId)
        {
            _messageIndex.TryRemove(messageId, out _);
        }

        public static List<PendingMessage> GetAllPending()
        {
            return _messageIndex.Values.OrderBy(m => m.ReceivedAt).ToList();
        }

        public static int GetPendingCount()
        {
            return _messageIndex.Count;
        }

        public static void Clear()
        {
            while (_pendingMessages.TryDequeue(out _)) { }
            _messageIndex.Clear();
        }
    }
}
