using System.Collections.ObjectModel;
using Easy.Agent.Services;

namespace Easy.Agent.Views;

/// <summary>Config tab root + drill-in sub-pages (appearance/backends/presets/
/// tools) and the providers list — mirrors the other clients' config surface.</summary>
public partial class ConfigPage : ContentPage
{
    private readonly AgentApi _api;
    private readonly AppNav.NavStore _nav;
    private readonly string _username;
    private readonly ObservableCollection<string> _rows = new();
    private readonly List<string> _keys = new();

    public ConfigPage(AgentApi api, AppNav.NavStore nav, string username)
    {
        _api = api;
        _nav = nav;
        _username = username;
        Title = "Settings";
        BackgroundColor = Color.FromArgb("#0d1117");

        foreach (var id in AppNav.ConfigSubIds)
        {
            _keys.Add("config_sub_" + id);
            _rows.Add(id);
        }
        _keys.Add("providers_list");
        _rows.Add("providers");

        var list = new CollectionView
        {
            ItemsSource = _rows,
            BackgroundColor = Color.FromArgb("#0d1117"),
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label { TextColor = Color.FromArgb("#e6edf3"), VerticalOptions = LayoutOptions.Center };
                label.SetBinding(Label.TextProperty, ".");
                return new Border
                {
                    BackgroundColor = Color.FromArgb("#161b22"),
                    StrokeThickness = 0,
                    Padding = 14,
                    Margin = new Thickness(8, 4),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle(),
                    Content = label,
                };
            }),
        };
        list.SelectionChanged += async (s, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is not string row) return;
            list.SelectedItem = null;
            var idx = _rows.IndexOf(row);
            var key = idx >= 0 ? _keys[idx] : "config_root";
            if (key == "providers_list")
            {
                _nav.Push(key);
                await Navigation.PushAsync(new ProvidersPage(_api, _nav));
            }
            else if (key == "config_sub_presets")
            {
                _nav.Push(key);
                await Navigation.PushAsync(new PresetFormPage(_api, _nav));
            }
            else
            {
                _nav.Push(key);
            }
        };

        Content = list;
    }
}

public partial class ProvidersPage : ContentPage
{
    private readonly AgentApi _api;
    private readonly AppNav.NavStore _nav;
    private readonly ObservableCollection<string> _rows = new();

    public ProvidersPage(AgentApi api, AppNav.NavStore nav)
    {
        _api = api;
        _nav = nav;
        Title = "Providers";
        BackgroundColor = Color.FromArgb("#0d1117");
        var list = new CollectionView
        {
            ItemsSource = _rows,
            BackgroundColor = Color.FromArgb("#0d1117"),
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label { TextColor = Color.FromArgb("#e6edf3") };
                label.SetBinding(Label.TextProperty, ".");
                return new Border
                {
                    BackgroundColor = Color.FromArgb("#161b22"),
                    StrokeThickness = 0,
                    Padding = 14,
                    Margin = new Thickness(8, 4),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle(),
                    Content = label,
                };
            }),
        };
        Content = list;
        Loaded += async (_, _) =>
        {
            var rows = await _api.ListProvidersAsync();
            _rows.Clear();
            foreach (var r in rows) _rows.Add(r);
        };
    }
}

public partial class PresetFormPage : ContentPage
{
    public PresetFormPage(AgentApi api, AppNav.NavStore nav)
    {
        Title = "New preset";
        BackgroundColor = Color.FromArgb("#0d1117");
        Content = new Label { Text = "Create / edit a preset", TextColor = Color.FromArgb("#8b98a9"), Padding = 16 };
    }
}

public partial class MailboxPage : ContentPage
{
    private readonly ObservableCollection<string> _rows = new();

    public MailboxPage(AgentApi api, string sessionId)
    {
        Title = "Mailbox";
        BackgroundColor = Color.FromArgb("#0d1117");
        var list = new CollectionView
        {
            ItemsSource = _rows,
            BackgroundColor = Color.FromArgb("#0d1117"),
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label { TextColor = Color.FromArgb("#e6edf3") };
                label.SetBinding(Label.TextProperty, ".");
                return new Border
                {
                    BackgroundColor = Color.FromArgb("#161b22"),
                    StrokeThickness = 0,
                    Padding = 14,
                    Margin = new Thickness(8, 4),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle(),
                    Content = label,
                };
            }),
        };
        Content = list;
        Loaded += async (_, _) =>
        {
            var (rows, _hasMore) = await api.MailboxAsync(sessionId, limit: 30);
            _rows.Clear();
            foreach (var r in rows) _rows.Add(r);
        };
    }
}
