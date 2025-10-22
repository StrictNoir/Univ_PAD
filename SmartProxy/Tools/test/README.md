# SmartProxy Ruby Test Suite

This directory contains a Ruby-based test harness for validating the SmartProxy ecosystem. It re-implements the scenarios that ship with the existing .NET tooling (`HttpClientDemo` and `ProxyTool`) so they can be executed quickly from the command line.

## Requirements

* Ruby 3.1+
* Bundler (`gem install bundler`)
* Running SmartProxy services:
  * **Server instances** reachable at `http://localhost:8080` and `http://localhost:8081` (defaults can be overridden via environment variables).
  * **Proxy** reachable at `http://localhost:9000`.

## Installation

```bash
cd SmartProxy/Tools/test
bundle install
```

## Usage

The test suite ships with a small CLI. Run it with Ruby:

```bash
ruby cli.rb [command] [options]
```

Available commands:

| Command | Description |
| ------- | ----------- |
| `all`   | Runs every spec (default). |
| `server` | Runs the server integration specs. |
| `proxy` | Runs the proxy configuration specs. |
| `tools` | Runs the Ruby tool specs (http client + helpers). |

### Example

```bash
ruby cli.rb server
```

### Filtering by RSpec tag

Any CLI invocation accepts `--tag`. For example, to run only the `integration` tests:

```bash
ruby cli.rb --tag integration
```

## Environment variables

| Variable | Default | Purpose |
| -------- | ------- | ------- |
| `SMART_PROXY_SERVER1_URL` | `http://localhost:8080/api/Employee` | Base URL for the primary server instance. |
| `SMART_PROXY_SERVER2_URL` | `http://localhost:8081/api/Employee` | Base URL for the secondary server instance. |
| `SMART_PROXY_PROXY_URL` | `http://localhost:9000/api/Employee` | Base URL for the proxy service. |
| `SMART_PROXY_WAIT_FOR_SYNC` | `3` | Seconds to wait after creating data before validating replication. |
| `SMART_PROXY_WAIT_FOR_RESOLUTION` | `5` | Seconds to wait after concurrent updates before checking conflict resolution. |
| `SMART_PROXY_PROXY_CONFIG` | `SmartProxy/Tools/config/ocelot.json` | Path to the proxy configuration file used by the Ruby tools. |

## Notes

* The server integration specs are designed to be non-destructive: they clean up any employee records they create.
* When the servers or proxy are unreachable the suite automatically skips the dependent tests with a clear explanation instead of failing outright.
