using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agentsdk;
using EasyRpc;
using Easyrpc.Agent.V1;
using Google.Protobuf;

namespace Easy.Agent.Services;

/// <summary>
/// Thin RPC facade over the generated AgentServiceClient and the easy-rpc
/// HttpClientTransport. Mirrors the other Easy Agent clients: same POST paths,
/// same application/connect+proto framing, same streamed turn events.
/// </summary>
public sealed class AgentApi
{
    private readonly AgentServiceClient _rpc;

    public AgentApi(string baseUrl, string token, string? caPem = null)
    {
        var t = new HttpClientTransport(baseUrl, new Version(2, 0),
            System.Net.Http.HttpVersionPolicy.RequestVersionOrLower, caPem);
        _rpc = new AgentServiceClient(
            new AuthTransport(t, $"Bearer {token}"));
    }

    public async Task HealthAsync(CancellationToken ct = default)
    {
        await _rpc.health(new HealthRequest());
    }

    /// <summary>The caller's resolved identity (tenant id/name + role).</summary>
    public async Task<(string Tenant, string TenantName, string Role)> IdentityAsync(
        CancellationToken ct = default)
    {
        var r = await _rpc.getIdentity(new GetIdentityRequest());
        return (r.Tenant, r.TenantName, r.Role);
    }

    /// <summary>Best-effort display name for the saved-backend list.</summary>
    public async Task<string> ResolveUsernameAsync(CancellationToken ct = default)
    {
        try
        {
            var (tenant, name, _) = await IdentityAsync(ct);
            return string.IsNullOrEmpty(name) ? tenant : name;
        }
        catch
        {
            return "";
        }
    }

    /// <summary>Only model/preset/locale/variant are client-editable (proto v0.18).</summary>
    public async Task SettingsAsync(string id, string model = "", string preset = "",
        string locale = "", string variant = "", CancellationToken ct = default)
    {
        await _rpc.updateSettings(new UpdateSettingsRequest
        {
            Id = id,
            Model = model,
            Preset = preset,
            Locale = locale,
            Variant = variant,
        });
    }

    public async Task<List<string>> ListPresetsAsync(string locale = "",
        CancellationToken ct = default)
    {
        var r = await _rpc.listPresets(new ListPresetsRequest { Locale = locale });
        return r.Presets.Select(p => p.Id).ToList();
    }

    public async Task<List<string>> ListProvidersAsync(CancellationToken ct = default)
    {
        var r = await _rpc.listProviders(new ListProvidersRequest());
        return r.Providers.Select(p => $"{p.ProviderId} · {p.Capability}").ToList();
    }

    public async Task<List<string>> ListToolsAsync(string locale = "",
        CancellationToken ct = default)
    {
        var r = await _rpc.listTools(new ListToolsRequest { Locale = locale });
        return r.Tools.Select(t => t.Name).ToList();
    }

    public async Task<string> GetConfigAsync(string key, CancellationToken ct = default)
    {
        var r = await _rpc.getConfig(new GetConfigRequest { Key = key });
        return r.Value;
    }

    public async Task SetConfigAsync(string key, string value, CancellationToken ct = default)
    {
        await _rpc.setConfig(new SetConfigRequest { Key = key, Value = value });
    }

    /// <summary>One page of the mailbox (NEWEST-FIRST, paged backward).</summary>
    public async Task<(List<string> Entries, bool HasMore)> MailboxAsync(string id,
        string before = "", int limit = 0, CancellationToken ct = default)
    {
        var r = await _rpc.mailbox(new MailboxRequest { Id = id, Before = before, Limit = limit });
        var entries = r.Mailbox
            .Select(m => $"{MailboxLabel(m.MsgType, m.Source)} · {m.Status}")
            .ToList();
        return (entries, r.HasMore);
    }

    /// <summary>(msgType, source) → a human label (mirrors the other clients).</summary>
    private static string MailboxLabel(string msgType, string source)
    {
        if (msgType == "interrupt") return "Interrupt";
        if (msgType != "trigger") return "Event";
        if (source == "user") return "Message";
        if (source.StartsWith("session:")) return $"From session · {source["session:".Length..]}";
        if (source.StartsWith("system:")) return $"From system · {source["system:".Length..]}";
        return "Message";
    }

    /// <summary>Fork a session WITHOUT opening it.</summary>
    public async Task ForkAsync(string id, string branch, CancellationToken ct = default)
    {
        await _rpc.fork(new ForkRequest { Id = id, Name = branch });
    }

    public async Task<List<string>> ListSessionsAsync(CancellationToken ct = default)
    {
        var r = await _rpc.listSessions(new ListSessionsRequest());
        // Ordered most-recent-first (lastMessageAt -> updatedAt -> createdAt).
        return r.Sessions
            .OrderByDescending(SessionRecency)
            .Select(s => s.Name)
            .ToList();
    }

    /// <summary>Epoch-ms recency: lastMessageAt -> updatedAt -> createdAt.</summary>
    private static long SessionRecency(Session s)
    {
        foreach (var v in new[] { s.LastMessageAt, s.UpdatedAt, s.CreatedAt })
        {
            if (!string.IsNullOrEmpty(v) &&
                DateTimeOffset.TryParse(v, out var d))
            {
                return d.ToUnixTimeMilliseconds();
            }
        }
        return 0;
    }

    public async Task<string> CreateSessionAsync(string name, CancellationToken ct = default)
    {
        var r = await _rpc.createSession(new CreateSessionRequest { Name = name });
        return r.SessionName;
    }

    public async Task SetModelAsync(string id, string model, CancellationToken ct = default)
    {
        await _rpc.setModel(new SetModelRequest { Id = id, Model = model });
    }

    public async Task<List<string>> ListMessagesAsync(string id, int limit = 50,
        CancellationToken ct = default)
    {
        var r = await _rpc.listMessages(new ListMessagesRequest { Id = id, Limit = limit });
        return MessagesToLines(r.Messages);
    }

    /// <summary>Start a turn; returns once the server accepts it. Live events
    /// arrive on the WatchSession stream (see WatchSessionAsync).</summary>
    public async Task PromptAsync(string id, string prompt, CancellationToken ct = default)
    {
        await foreach (var _ in _rpc.prompt(new PromptRequest { Id = id, Prompt = prompt }))
        {
            // drain; the accepted frame carries the message id
        }
    }

    /// <summary>Long-lived session event stream; invoke onEvent per frame until
    /// ct is cancelled.</summary>
    public async Task WatchSessionAsync(string id,
        Action<string, IReadOnlyDictionary<string, string>> onEvent,
        CancellationToken ct = default)
    {
        await foreach (var ev in _rpc.watchSession(new WatchSessionRequest { Id = id }))
        {
            var dict = new Dictionary<string, string>();
            if (ev.Params != null)
            {
                foreach (var f in ev.Params.Fields)
                {
                    dict[f.Key] = f.Value.KindCase switch
                    {
                        Google.Protobuf.WellKnownTypes.Value.KindOneofCase.StringValue => f.Value.StringValue,
                        Google.Protobuf.WellKnownTypes.Value.KindOneofCase.NumberValue => f.Value.NumberValue.ToString(),
                        Google.Protobuf.WellKnownTypes.Value.KindOneofCase.BoolValue => f.Value.BoolValue.ToString(),
                        _ => f.Value.ToString(),
                    };
                }
            }
            onEvent(ev.Event, dict);
        }
    }

    private static List<string> MessagesToLines(IEnumerable<Message> messages)
    {
        var lines = new List<string>();
        foreach (var m in messages)
        {
            // A `session:{name}` hand-off is labelled with its origin.
            var who = m.Source.StartsWith("session:") ? $"[{m.Source["session:".Length..]}]"
                : m.Source.StartsWith("system:") ? $"[system:{m.Source["system:".Length..]}]"
                : m.Role switch
                {
                    "user" => "You",
                    "assistant" => "Agent",
                    _ => m.Role,
                };
            foreach (var p in m.Parts)
            {
                var d = ParseJson(p.Data);
                if (p.Type is "text" or "reasoning")
                {
                    var text = GetString(d, "text");
                    if (!string.IsNullOrWhiteSpace(text)) lines.Add($"{who}: {text}");
                }
                else if (p.Type == "tool")
                {
                    lines.Add($"[tool: {GetString(d, "name") ?? "tool"}]");
                }
            }
        }
        return lines;
    }

    private static System.Text.Json.JsonElement? ParseJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(s);
            return doc.RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }

    private static string? GetString(System.Text.Json.JsonElement? e, string key)
    {
        if (e is { ValueKind: System.Text.Json.JsonValueKind.Object } obj &&
            obj.TryGetProperty(key, out var v) &&
            v.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            return v.GetString();
        }
        return null;
    }
}
