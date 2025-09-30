# 🛰️ Part 2 – gRPC Messaging Guide

This document explains how to turn the existing C# sender and receiver console apps into drop-in replacements for the Ruby CLI tools that ship with the gRPC broker. Follow the sections below to regenerate gRPC types, point both apps at the Ruby endpoint, and expose the same interactive commands your teammates are already using in the Ruby tooling.

---

## 📑 Broker contract

| Item | Details |
| --- | --- |
| Proto | `Agent Mesagerie/broker-ruby/photo/broker.proto` defines the `Broker` service with `Publish`, `Subscribe`, and `Ack` RPCs. |
| Endpoint | The Ruby broker listens on `0.0.0.0:5001` (use `localhost:5001` when everything runs on one machine). |
| Transport | Development traffic is plaintext HTTP/2. The .NET client must explicitly enable unencrypted HTTP/2 support. |
| Message schema | `Envelope` carries `subject`, `payload`, optional `headers`, and `message_id`. `PublishAck` reports acceptance and any broker detail message. |

---

## 🛠️ One-time C# project setup

1. **Install gRPC dependencies**
   ```bash
   dotnet add "Agent Mesagerie/sender-csharp/sender/sender.csproj" package Grpc.Net.Client
   dotnet add "Agent Mesagerie/sender-csharp/sender/sender.csproj" package Google.Protobuf
   dotnet add "Agent Mesagerie/sender-csharp/sender/sender.csproj" package Grpc.Tools

   dotnet add "Agent Mesagerie/receiver-csharp/Subscriber/Subscriber.csproj" package Grpc.Net.Client
   dotnet add "Agent Mesagerie/receiver-csharp/Subscriber/Subscriber.csproj" package Google.Protobuf
   dotnet add "Agent Mesagerie/receiver-csharp/Subscriber/Subscriber.csproj" package Grpc.Tools
   ```
2. **Reference the proto in each project**
   ```xml
   <ItemGroup>
     <Protobuf Include="..\..\broker-ruby\photo\broker.proto" GrpcServices="Client" />
   </ItemGroup>
   ```
3. **Allow plaintext HTTP/2** – add at the top of `Main` in both programs:
   ```csharp
   AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
   ```
4. **Create a shared client helper** – use the same factory in both apps to mirror how the Ruby CLIs resolve IPv4 and connect:
   ```csharp
   static Broker.BrokerClient CreateClient(string host, int port)
   {
       var channel = GrpcChannel.ForAddress($"http://{host}:{port}");
       return new Broker.BrokerClient(channel);
   }
   ```

---

## 🚀 Rebuilding the C# publisher CLI

Match `bundle exec ruby tools/publisher_cli.rb HOST PORT`, which loops over `subject>` and `message>` prompts and prints the broker acknowledgement.

1. **Parse CLI arguments**
    * Accept optional `host`/`port` command-line arguments so teammates can run:  
      `dotnet run --project "Agent Mesagerie/sender-csharp/sender" -- localhost 5001`
    * Default to `localhost` and `5001` if no values are supplied.
2. **Replace the TCP REPL with a gRPC loop**
    * Remove the raw `TcpClient` usage.
    * Inside your interactive loop, prompt with `subject>` and `message>` exactly like the Ruby CLI. Treat `exit` (any casing) as quit.
    * For every pair, build an `Envelope`:
      ```csharp
      var envelope = new Envelope
      {
          Subject = subject,
          Payload = ByteString.CopyFromUtf8(message),
          TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
      };
      ```
    * Send it with `await client.PublishAsync(envelope);`.
3. **Mirror the Ruby output**
   After the call, print:
   ```csharp
   Console.WriteLine($"PUBLISHED subject={subject} message_id={ack.MessageId} accepted={ack.Accepted}");
   if (!string.IsNullOrWhiteSpace(ack.Detail))
   {
       Console.WriteLine($"  detail: {ack.Detail}");
   }
   ```
4. **Graceful exit** – trap `Ctrl+C` (optional) and dispose the gRPC channel just like the Ruby script prints “Goodbye!”.

**Run it:**
```bash
cd /workspace/Univ_PAD
bundle exec ruby "Agent Mesagerie/broker-ruby/broker.rb"   # terminal 1

dotnet run --project "Agent Mesagerie/sender-csharp/sender" -- localhost 5001  # terminal 2
subject> chat.games
message> Hello from C#
PUBLISHED subject=chat.games message_id=... accepted=True
```

---

## 📡 Rebuilding the C# subscriber CLI

Match `bundle exec ruby tools/subscriber_cli.rb HOST PORT`, which supports the commands `add`, `remove`, `remove all`, `list`, `help`, and `exit` while streaming envelopes.

1. **Argument parsing & connection**
    * Accept the same `host`/`port` arguments and default to `localhost:5001`.
    * Print the resolved endpoint (if you resolve DNS to IPv4) so it matches the Ruby script’s log lines.
2. **Maintain active subscriptions**
    * Keep a dictionary of subject → `{ call, cancellationTokenSource }` to emulate the Ruby `active_calls` hash.
    * When the user runs `add <subject>`, start an async task that calls:
      ```csharp
      var subscription = new Subscription
      {
          Subject = subject,
          ConsumerGroup = consumerGroup ?? string.Empty
      };
 
      using var call = client.Subscribe(subscription);
      await foreach (var envelope in call.ResponseStream.ReadAllAsync(ct))
      {
          var payload = envelope.Payload.ToStringUtf8();
          Console.WriteLine($"RECEIVED subject={envelope.Subject} message_id={envelope.MessageId} timestamp_ms={envelope.TimestampMs}");
          Console.WriteLine(string.IsNullOrEmpty(payload) ? "  (empty payload)" : $"  payload: {payload}");
 
          if (autoAck && !string.IsNullOrWhiteSpace(envelope.MessageId))
          {
              var ackReply = await client.AckAsync(new AckRequest
              {
                  Subject = envelope.Subject,
                  MessageId = envelope.MessageId,
                  ConsumerGroup = consumerGroup ?? string.Empty
              });
              Console.WriteLine($"  acked: {ackReply.Acknowledged}");
          }
      }
      ```
3. **Implement the CLI commands**
    * `add <subject>` – start a new subscription stream (ignore duplicates).
    * `remove <subject>` – cancel and dispose the running stream for that subject.
    * `remove all` – cancel every active stream.
    * `list` – print all currently subscribed subjects.
    * `help` – output the same help text as the Ruby CLI.
    * `exit` – cancel active streams and close the app.
4. **Error handling** – catch `RpcException` from `Subscribe`/`Ack` and print `ERROR <StatusCode>: <Message>` similar to the Ruby warnings.
5. **Auto-ack toggle**
    * Prompt once at startup: `auto-ack? [y/N]>`.
    * Use the reply to control whether you send `AckAsync` calls inside the streaming loop.

**Run it:**
```bash
cd /workspace/Univ_PAD
bundle exec ruby "Agent Mesagerie/broker-ruby/broker.rb"   # terminal 1

dotnet run --project "Agent Mesagerie/receiver-csharp/Subscriber" -- localhost 5001  # terminal 2
Type "exit" at any prompt to quit.
add chat.games
list
# -> chat.games
```
When the C# publisher (or the Ruby publisher CLI) sends a message, the subscriber prints the same `RECEIVED subject=...` log lines as the Ruby script.

---
