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

# Git Workflow – Team Rules

## Basic Rules
- Everyone works on a **separate branch** (`feature/name`, `fix/name`).
- Do not commit directly to `main`.
- Commits should be **clear and short** (`git commit -m "Implement broker routing"`).
- Before opening a Pull Request, run `git pull --rebase origin main`.

## Daily Work Commands

### Initial Configuration
- `git config --global user.name "Full Name"` – sets the author name.
- `git config --global user.email "email@domain.com"` – sets the author email.
- `git config --list` – check the current settings.

### Cloning and Remote
- `git clone <url>` – clones the repo.
- `git remote -v` – shows the remote repos.
- `git remote add origin <url>` – sets the origin remote.

### Branching
- `git branch` – lists local branches.
- `git branch -r` – lists remote branches.
- `git checkout -b feature/name` – creates and switches to a new branch.
- `git switch main` – quickly switch to the main branch.
- `git branch -d name` – deletes a local branch.

### Commit & Stage
- `git status` – shows changes compared to the repo.
- `git add <file>` – stages a file for commit.
- `git add .` – stages all modified files.
- `git commit -m "message"` – saves changes locally.
- `git commit --amend` – modifies the last commit.

### Synchronization
- `git fetch` – fetches changes from remote without merging them.
- `git pull` – fetches + merges remote changes.
- `git pull --rebase` – rewrites local history on top of the latest remote version (avoids unnecessary merge commits).
## Git Push Commands

| Command | Description | When to Use |
|---------|-------------|-------------|
| `git push` | Pushes commits from the current branch to its **default remote** (usually `origin`) and the branch it is tracking. | Simple case: you already have a remote branch set up (e.g., after a clone). |
| `git push origin main` | Pushes the local `main` branch to the remote `origin` repository’s `main` branch. | Explicitly push a specific branch. Useful if no upstream is set. |
| `git push -u origin main` | Pushes `main` and **sets the upstream** (link) between local `main` and `origin/main`. | First push of a new branch. After this, just `git push` works. |
| `git push origin feature-branch` | Pushes the branch `feature-branch` to `origin`. | Share a feature branch with teammates. |
| `git push origin --delete feature-branch` | Deletes the remote branch `feature-branch`. | Clean up remote branches no longer needed. |
| `git push --force` | **Overwrites** the remote branch with your local commits, discarding any divergent remote history. | Use with caution (e.g., after rewriting history with `git rebase`). |
| `git push --force-with-lease` | Safer force push: only overwrites if the remote branch hasn’t changed since you last fetched. | Preferred over plain `--force`. |

👉 Rule of thumb: use `-u` on the first push of a branch, use `--force-with-lease` instead of `--force`, and always double-check the branch you’re pushing to.


| Role           | Direction    | JSON frame                                                         | Notes                                                                                                                                                                                                    |
| -------------- | ------------ | ------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Publisher**  | → Broker     | `{"op":"PUBLISH","message":{…}}`                                   | The `message` object **must** contain `id`, `type`, `payload`, and ISO‑8601 `timestamp`. Invalid JSON or missing fields yield `{"op":"ERROR","code":"BadRequest","detail":"invalid message"}`            |
| **Subscriber** | → Broker     | `{"op":"SUBSCRIBE","subjects":["order.*"],"subscriberId":"sub-1"}` | `subjects` = array of subject patterns; `subscriberId` = unique identifier. Broker responds with `{"op":"SUBSCRIBED","subjects":[...],"subscriberId":"sub-1"}` or an `ERROR` frame if fields are missing |
| **Any client** | → Broker     | `{"op":"PING"}`                                                    | Broker replies `{"op":"PONG"}`; useful for keep‑alive checks                                                                                                                                             |
| **Broker**     | → Subscriber | `{"op":"DELIVER","deliveryId":42,"message":{…}}`                   | Sent to all subscribers whose patterns match the message `type`                                                                                                                                          |
| **Broker**     | → Any client | `{"op":"ERROR","code":"…","detail":"…"}`                           | Signals malformed JSON, missing/invalid fields, or unknown operations                                                                                                                                    |

Connecting to the broker
* Open a persistent TCP connection to the broker’s host and port (0.0.0.0:5001 unless configured otherwise).
* All traffic is framed as length‑prefixed JSON:
* Before every JSON payload, send a 4‑byte big‑endian integer specifying payload length.
* Decode incoming frames using the same framing.

Subject/pattern rules
* Subjects and patterns use dot notation ("order.created").
* wildcard matches exactly one segment (order.* matches order.created but not order.created.email).
* Pattern length must equal subject length for a match.

Recommended client behavior
* For publishers: construct valid PUBLISH frames; no ACK is returned—monitor ERROR frames for failures.
* For subscribers: send SUBSCRIBE immediately after connecting; maintain a read loop to process DELIVER frames and handle ERROR notifications.
* For all clients: optionally send periodic PING frames to detect broken connections and reconnect as needed.

### Inspection
- `git log --oneline --graph --decorate --all` – displays history nicely.
- `git diff` – shows differences before committing.
- `git show <commit>` – shows details about a commit.

### Revert & Reset
- `git restore <file>` – reverts to the last saved version of the file.
- `git checkout <commit> -- <file>` – retrieves a file version from a commit.
- `git revert <commit>` – creates a commit that cancels the effects of the specified commit.
- `git reset --hard <commit>` – completely resets to a commit (warning: local data loss).
- `git clean -fd` – deletes untracked files.

### Other Useful Flags
- `git stash` – temporarily saves uncommitted changes.
- `git stash pop` – restores stashed changes.
- `git blame <file>` – shows who modified each line.
- `git tag v1.0.0` – marks a release.

---

## Recommended Workflow
1. `git checkout -b feature/name`
2. Work and run `git add . && git commit -m "Clear description"`
3. `git pull --rebase origin main` (synchronize)
4. `git push origin feature/name`
5. Create a Pull Request on GitHub
