
using Grpc.Core;

namespace Subscriber.Grpc
{
    public class MessageReceiver
    {
        private readonly MessageHandler _messageHandler;
        private readonly bool _autoAck;
        private readonly Broker.BrokerClient _brokerClient;
        private readonly string _consumerGroup;

        public MessageReceiver(
             MessageHandler messageHandler,
             bool autoAck,
             Broker.BrokerClient brokerClient,
             string consumerGroup = "")
        {
            _messageHandler = messageHandler;
            _autoAck = autoAck;
            _brokerClient = brokerClient;
            _consumerGroup = consumerGroup;
        }
        public async Task ReceiveMessagesAsync(string subject, AsyncServerStreamingCall<Envelope> call, CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var envelope in call.ResponseStream.ReadAllAsync(cancellationToken))
                {
                    _messageHandler.HandleEnvelope(envelope);

                    if (_autoAck && !string.IsNullOrEmpty(envelope.MessageId))
                    {
                        await AcknowledgeAsync(envelope.Subject, envelope.MessageId);
                    }
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                // Expected when unsubscribing
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in subscription for \"{subject}\": {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"Subscription for \"{subject}\" ended.");
            }
        }
        private async Task AcknowledgeAsync(string subject, string messageId)
        {
            try
            {
                var ackReply = await _brokerClient.AckAsync(new AckRequest
                {
                    Subject = subject,
                    MessageId = messageId,
                    ConsumerGroup = _consumerGroup
                });

                Console.WriteLine($"  acked: {ackReply.Acknowledged} (message_id={messageId})");
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"  ACK ERROR {ex.StatusCode}: {ex.Status.Detail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ACK ERROR: {ex.Message}");
            }
        }

    }
}
