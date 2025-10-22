# SmartProxy Ruby Tools

The legacy C# helper utilities have been rewritten in Ruby so they can be
exercised directly from the SmartProxy test harness. Two command line scripts
are available in `bin/`:

- `http_client_demo` runs the employee conflict resolution scenario against the
  SmartProxy servers. This mirrors the behaviour that previously lived in the
  .NET `HttpClientDemo` console application.
- `proxy_routes` prints a readable summary of the SmartProxy proxy configuration
  (`ocelot.json`).

Both scripts respect the same environment variables as the test suite:

- `SMART_PROXY_SERVER1_URL`
- `SMART_PROXY_SERVER2_URL`
- `SMART_PROXY_WAIT_FOR_SYNC`
- `SMART_PROXY_WAIT_FOR_RESOLUTION`
- `SMART_PROXY_PROXY_CONFIG`

To execute either script you only need Ruby 3.x available:

```bash
cd SmartProxy/Tools
bin/http_client_demo
bin/proxy_routes
```

Use `--help` for the list of available options.

## Test harness

The Ruby test suite that exercises these tools now lives under `test/` within
this directory. After installing dependencies with Bundler you can run targeted
or full suites via the included CLI:

```bash
cd SmartProxy/Tools/test
bundle install
ruby cli.rb [command]
```
