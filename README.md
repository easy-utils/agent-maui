# agent-maui

**Easy Agent** — mobile/desktop client in C# using [.NET MAUI](https://learn.microsoft.com/dotnet/maui),
over the same easy-rpc (Connect) wire as every other Easy Agent client.

It consumes the generated
[`AgentSdkCsharp`](https://github.com/easy-utils/agent-sdk-csharp) client and the
[`EasyRpc`](https://github.com/easy-utils/easy-rpc-csharp) transport.

```bash
export JAVA_HOME=/opt/tools/mise/installs/java/17.0.2
export ANDROID_HOME=$HOME/Android/sdk
export GITHUB_TOKEN=$(gh auth token)     # GitHub Packages feed auth
dotnet build -f net10.0-android
```

Connect with the standalone agent's base URL + a tenant token, list sessions,
open a chat, and stream a prompt turn (`text-delta`, `reasoning-delta`,
`tool-call`).

## Targets

This repository targets **`net10.0-android`** on this build host (the Linux
MAUI desktop target is not installed). The `.csproj` automatically adds
`net10.0-ios` / `net10.0-maccatalyst` on macOS and `net10.0-windows*` on
Windows.

## Dev-cluster CA

The self-signed agent CA is bundled at `Resources/Raw/agent_ca.crt` and passed
to `HttpClientTransport`, so the app trusts `*.10.199.64.20.nip.io` gateways.
