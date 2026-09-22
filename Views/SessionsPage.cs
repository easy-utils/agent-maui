using System.Collections.ObjectModel;
using Easy.Agent.Services;

namespace Easy.Agent.Views;

public partial class SessionsPage : ContentPage
{
    private readonly AgentApi _api;
    private readonly CollectionView _list;
    private readonly ObservableCollection<string> _sessions = new();
    private readonly Label _status;
    private readonly AppNav.NavStore _nav;
    private readonly string _username;

    public SessionsPage(AgentApi api, AppNav.NavStore nav, string username)
    {
        _api = api;
        _nav = nav;
        _username = username;
        Title = "Sessions";
        BackgroundColor = Color.FromArgb("#0d1117");

        _list = new CollectionView
        {
            ItemsSource = _sessions,
            BackgroundColor = Color.FromArgb("#0d1117"),
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label
                {
                    TextColor = Color.FromArgb("#e6edf3"),
                    VerticalOptions = LayoutOptions.Center,
                };
                label.SetBinding(Label.TextProperty, ".");
                var card = new Border
                {
                    BackgroundColor = Color.FromArgb("#161b22"),
                    StrokeThickness = 0,
                    Padding = 14,
                    Margin = new Thickness(8, 4),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle(),
                    Content = label,
                };
                // Swipe right → Fork (without opening). MAUI has no cross-platform
                // long-press context menu, so swipe is the native affordance.
                var swipe = new SwipeView { Content = card };
                var forkItem = new SwipeItem
                {
                    Text = "Fork",
                    BackgroundColor = Color.FromArgb("#2563eb"),
                };
                forkItem.Invoked += async (_, _) =>
                {
                    if (!string.IsNullOrEmpty(label.Text)) await ForkAsync(label.Text);
                };
                swipe.RightItems = new SwipeItems { forkItem };
                return swipe;
            }),
        };
        _list.SelectionChanged += OnSelected;

        var newBtn = new Button { Text = "New", BackgroundColor = Color.FromArgb("#21262d"), TextColor = Color.FromArgb("#e6edf3") };
        newBtn.Clicked += OnNew;
        var refreshBtn = new Button { Text = "Refresh", BackgroundColor = Color.FromArgb("#21262d"), TextColor = Color.FromArgb("#e6edf3") };
        refreshBtn.Clicked += async (_, _) => await RefreshAsync();
        var settingsBtn = new Button { Text = "Settings", BackgroundColor = Color.FromArgb("#21262d"), TextColor = Color.FromArgb("#e6edf3") };
        settingsBtn.Clicked += async (_, _) =>
        {
            _nav.Tab = "config";
            await Navigation.PushAsync(new ConfigPage(_api, _nav, _username));
        };

        _status = new Label { TextColor = Color.FromArgb("#8b98a9"), LineBreakMode = LineBreakMode.WordWrap };

        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
            },
            Children =
            {
                MakeRow(0, new HorizontalStackLayout
                {
                    Padding = new Thickness(12, 8),
                    Spacing = 6,
                    Children =
                    {
                        new Label
                        {
                            Text = "Sessions",
                            TextColor = Color.FromArgb("#e6edf3"),
                            FontAttributes = FontAttributes.Bold,
                            VerticalOptions = LayoutOptions.Center,
                        },
                        newBtn,
                        refreshBtn,
                        settingsBtn,
                    },
                }),
                Place(_list, 1),
                Place(_status, 2),
            },
        };

        Loaded += async (_, _) => await RefreshAsync();
    }

    private static View MakeRow(int row, View v)
    {
        Grid.SetRow(v, row);
        return v;
    }

    private static View Place(View v, int row)
    {
        Grid.SetRow(v, row);
        return v;
    }

    private async Task RefreshAsync()
    {
        try
        {
            var list = await _api.ListSessionsAsync();
            _sessions.Clear();
            foreach (var name in list) _sessions.Add(name);
            _status.Text = "";
        }
        catch (Exception ex)
        {
            _status.Text = $"Refresh failed: {ex.Message}";
        }
    }

    private async void OnNew(object? sender, EventArgs e)
    {
        try
        {
            await _api.CreateSessionAsync($"easy-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _status.Text = $"Create failed: {ex.Message}";
        }
    }

    private async void OnSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not string id) return;
        _list.SelectedItem = null;
        _nav.Tab = "chat";
        _nav.ActiveSessionId = id;
        _nav.Push("chat_session");
        await Navigation.PushAsync(new ChatPage(_api, id));
    }

    /// <summary>Fork a session WITHOUT opening it (swipe action).</summary>
    private async Task ForkAsync(string id)
    {
        try
        {
            await _api.ForkAsync(id, $"fork-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _status.Text = $"Fork failed: {ex.Message}";
        }
    }
}
