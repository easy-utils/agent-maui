using System.Collections.ObjectModel;
using Easy.Agent.Services;

namespace Easy.Agent.Views;

public partial class SessionsPage : ContentPage
{
    private readonly AgentApi _api;
    private readonly CollectionView _list;
    private readonly ObservableCollection<string> _sessions = new();
    private readonly Label _status;

    public SessionsPage(AgentApi api)
    {
        _api = api;
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
        _list.SelectionChanged += OnSelected;

        var newBtn = new Button { Text = "New", BackgroundColor = Color.FromArgb("#21262d"), TextColor = Color.FromArgb("#e6edf3") };
        newBtn.Clicked += OnNew;
        var refreshBtn = new Button { Text = "Refresh", BackgroundColor = Color.FromArgb("#21262d"), TextColor = Color.FromArgb("#e6edf3") };
        refreshBtn.Clicked += async (_, _) => await RefreshAsync();

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
        await Navigation.PushAsync(new ChatPage(_api, id));
    }
}
