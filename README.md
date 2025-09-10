# 📡 Laboratory Project – Messaging Agent (Broker, Sender, Receiver)

## 🎯 General Description
This project implements a **messaging agent (Message Broker)** for asynchronous communication between distributed components.

The structure is composed of three parts:
- **Broker (Ruby)** – manages connections, receives and routes messages.
- **Sender (C#)** – sends messages to the Broker.
- **Receiver (C#)** – receives messages from the Broker, based on subscriptions.

The work is carried out in two stages:
1. **Part 1 (Sockets)** – TCP + JSON framing + routing.
2. **Part 2 (RPC)** – gRPC / Thrift, with data representation using proto/thrift/avro.

---

## 🔗 Message Protocol
### Framing
- `[4 bytes length][JSON UTF-8]`


## 🟦 C# Publisher & Subscriber

### Broker endpoint
The Ruby broker listens on `0.0.0.0:5001` by default (see `Agent Mesagerie/broker-ruby/config/broker.yml`).
Use `localhost:5001` when running all components locally.

### Publisher flow
1. Establish a TCP connection to the broker endpoint.
2. Send `PUBLISH` frames with a topic and arbitrary JSON payload. Each frame is prefixed with a 4‑byte big‑endian length, e.g.
   ```json
   {"op":"PUBLISH","topic":"chat.general","message":{"text":"Hello world"}}
   ```
3. The broker forwards the message to all subscribers whose topic patterns match. Invalid frames trigger an `ERROR` response.
4. Optionally send periodic `PING` frames to keep the connection alive.

### Subscriber flow
1. Connect to the same broker endpoint.
2. Send a `SUBSCRIBE` frame with the topic pattern you want to receive. You may resume from a stored message id with `from`:
   ```json
   {"op":"SUBSCRIBE","topic":"chat.*","from":"42"}
   ```
3. Keep a read loop to process incoming frames. `DELIVER` frames contain published messages; `SUBSCRIBED` confirms the subscription and `ERROR` reports problems.
4. Subscribers may also send `PING`; the broker replies with `PONG`.

### How it works
Both C# clients use the same length‑prefixed JSON protocol as the Ruby test CLIs.
The broker compares each published `topic` against subscription patterns (supporting `*` as a single‑segment wildcard)
and forwards messages to matching subscribers, enabling asynchronous, decoupled communication between components.

# Git Workflow – Team Rules

## Basic Rules
- Everyone works on a **separate branch** (`feature/name`, `fix/name`).
- Do not commit directly to `main`.
- Commits should be **clear and short** (`git commit -m "Implement broker routing"`).
- Before opening a Pull Request, run `git pull --rebase origin main`.

| Role           | Direction    | JSON frame                                                         | Notes |
| -------------- | ------------ | ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Publisher**  | → Broker     | `{"op":"PUBLISH","topic":"chat.general","message":{…}}`          | Delivered to all subscribers whose topic pattern matches the `topic` field. Payload can be any JSON object. |
| **Subscriber** | → Broker     | `{"op":"SUBSCRIBE","topic":"chat.*","from":"42"}`              | `topic` = pattern to receive; optional `from` restarts delivery from a stored `storeId`. Broker responds with `SUBSCRIBED` or `ERROR`. |
| **Any client** | → Broker     | `{"op":"PING"}`                                                  | Broker replies `{"op":"PONG"}`; useful for keep‑alive checks |
| **Broker**     | → Subscriber | `{"op":"DELIVER","topic":"chat.general","storeId":"43","message":{…}}` | Sent to subscribers whose pattern matches. `storeId` can be saved as a checkpoint for resuming later. |
| **Broker**     | → Any client | `{"op":"ERROR","code":"…","detail":"…"}`                         | Signals malformed JSON, missing/invalid fields, or unknown operations |


Connecting to the broker
* Open a persistent TCP connection to the broker’s host and port (0.0.0.0:5001 unless configured otherwise).
* All traffic is framed as length‑prefixed JSON:
* Before every JSON payload, send a 4‑byte big‑endian integer specifying payload length.
* Decode incoming frames using the same framing.

Topic/pattern rules
* Topics use dot notation ("chat.general").
* `*` matches exactly one segment (`chat.*` matches `chat.general` but not `chat.general.news`).
* Pattern length must equal topic length for a match.

Recommended client behavior
* For publishers: construct valid PUBLISH frames; no ACK is returned—monitor ERROR frames for failures.
* For subscribers: send SUBSCRIBE immediately after connecting; maintain a read loop to process DELIVER, SUBSCRIBED, and ERROR frames.
* For all clients: optionally send periodic PING frames to detect broken connections and reconnect as needed.


## Recommended Workflow
1. `git checkout -b feature/name`
2. Work and run `git add . && git commit -m "Clear description"`
3. `git pull --rebase origin main` (synchronize)
4. `git push origin feature/name`
5. Create a Pull Request on GitHub
