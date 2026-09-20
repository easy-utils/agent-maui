namespace Easy.Agent;

// Navigation model — port of the Flutter/Compose/webui contract.
//
// Two side tabs, each with its own page stack. Page keys and config sub-ids are
// identical to every other Easy Agent client (guarded by tools/pages.py), so a
// page added here must be added everywhere.
public static class AppNav
{
    /// The side-tab set (page contract).
    public static readonly string[] SiderTabs = new[] { "chat", "config" };

    /// The config drill-in sub-pages (page contract).
    public static readonly string[] ConfigSubIds = new[] { "appearance", "backends", "presets", "tools" };

    /// The chat-tab overlays (page contract).
    public static readonly string[] SessionOverlays = new[] { "mailbox" };

    /// Each static page key -> the view that dispatches it (the dispatch table
    /// the guard checks).
    public static readonly Dictionary<string, string> PageViews = new()
    {
        ["chat_list"] = "SessionsPage",
        ["chat_session"] = "ChatPage",
        ["chat_overlay"] = "MailboxPage",
        ["config_root"] = "ConfigPage",
        ["providers_list"] = "ProvidersPage",
        ["preset_form_new"] = "PresetFormPage",
        ["provider_form"] = "ProviderFormPage",
    };

    public static string RootKey(string tab) => tab == "chat" ? "chat_list" : "config_root";

    /// Dispatch a page key to its view name (the switch-case table).
    public static string ViewFor(string key)
    {
        switch (key)
        {
            case "chat_list": return "SessionsPage";
            case "chat_session": return "ChatPage";
            case "chat_overlay": return "MailboxPage";
            case "config_root": return "ConfigPage";
            case "providers_list": return "ProvidersPage";
            case "preset_form_new": return "PresetFormPage";
            case "provider_form": return "ProviderFormPage";
            default: return "ConfigPage";
        }
    }

    /// Per-tab page stacks with the same push/pop semantics as the other clients.
    public sealed class NavStore
    {
        private readonly Dictionary<string, List<string>> _stacks = new();

        public NavStore()
        {
            _stacks.Add("chat", new List<string> { "chat_list" });
            _stacks.Add("config", new List<string> { "config_root" });
        }

        public string Tab { get; set; } = "chat";
        public string ActiveSessionId { get; set; } = "";

        public string Top()
        {
            var s = _stacks[Tab];
            return s.Count > 0 ? s[^1] : RootKey(Tab);
        }

        public void Push(string key) => _stacks[Tab].Add(key);

        public void Pop()
        {
            var s = _stacks[Tab];
            if (s.Count > 1) s.RemoveAt(s.Count - 1);
        }
    }
}
