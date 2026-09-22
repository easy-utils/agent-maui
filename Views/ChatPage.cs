using System.Collections.ObjectModel;
using System.Threading;
using Easy.Agent.Services;

namespace Easy.Agent.Views;

public partial class ChatPage : ContentPage
{
    private readonly AgentApi _api;
    private readonly string _sessionId;
    private readonly ObservableCollection<string> _lines = new();
    private readonly CollectionView _list;
    private readonly Entry _composer;
    private readonly CancellationTokenSource _cts = new();

    public ChatPage(AgentApi api, string sessionId)
    {
        _api = api;
        _sessionId = sessionId;
        Title = sessionId;
        BackgroundColor = Color.FromArgb("#0d1117");

        _list = new CollectionView
        {
            ItemsSource = _lines,
            BackgroundColor = Color.FromArgb("#0d1117"),
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label
                {
                    TextColor = Color.FromArgb("#e6edf3"),
                    LineBreakMode = LineBreakMode.WordWrap,
                };
                label.SetBinding(Label.TextProperty, ".");
                return new Border
                {
                    BackgroundColor = Color.FromArgb("#161b22"),
                    StrokeThickness = 0,
                    Padding = 10,
                    Margin = new Thickness(8, 4),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle(),
                    Content = label,
                };
            }),
        };

        _composer = new Entry
        {
            Placeholder = "Message…",
            TextColor = Color.FromArgb("#e6edf3"),
            PlaceholderColor = Color.FromArgb("#8b98a9"),
            BackgroundColor = Color.FromArgb("#21262d"),
        };
        _composer.Completed += async (_, _) => await SendAsync();

        var send = new Button { Text = "Send", BackgroundColor = Color.FromArgb("#2563eb"), TextColor = Colors.White };
        send.Clicked += async (_, _) => await SendAsync();

        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
            },
            Children =
            {
                _list,
                MakeComposerRow(send),
            },
        };

        Loaded += OnLoaded;
    }

    private View MakeComposerRow(Button send)
    {
        var row = new Grid
        {
            Padding = 8,
            ColumnSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            },
        };
        Grid.SetColumn(_composer, 0);
        Grid.SetColumn(send, 1);
        row.Add(_composer);
        row.Add(send);
        Grid.SetRow(row, 1);
        return row;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        // History first, then the live event stream.
        try
        {
            var history = await _api.ListMessagesAsync(_sessionId, 50);
            foreach (var line in history) _lines.Add(line);
        }
        catch
        {
            // the live stream still populates
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _api.WatchSessionAsync(_sessionId, OnEvent, _cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() => Append($"\n[stream error: {ex.Message}]"));
            }
        }, _cts.Token);
    }

    private void OnEvent(string ev, IReadOnlyDictionary<string, string> p)
    {
        switch (ev)
        {
            case "message-added":
                // The server authored a message id/position: render the user
                // prompt (no client-optimistic bubble) with its origin.
                var role = p.GetValueOrDefault("role", "assistant");
                if (role == "user")
                {
                    var src = p.GetValueOrDefault("source", "");
                    var label = src.StartsWith("session:") ? $"[{src["session:".Length..]}]"
                        : src.StartsWith("system:") ? $"[system:{src["system:".Length..]}]"
                        : "You";
                    MainThread.BeginInvokeOnMainThread(() => Push($"{label}: "));
                }
                break;
            case "text-delta":
                MainThread.BeginInvokeOnMainThread(() => Append(p.GetValueOrDefault("text", "")));
                break;
            case "reasoning-delta":
                MainThread.BeginInvokeOnMainThread(() =>
                    Append($"[reasoning] {p.GetValueOrDefault("text", "")}"));
                break;
            case "tool-call":
                var name = p.GetValueOrDefault("toolName") ?? p.GetValueOrDefault("name") ?? "tool";
                MainThread.BeginInvokeOnMainThread(() => Push($"\n[tool: {name}]\n"));
                break;
        }
    }

    private async Task SendAsync()
    {
        var text = _composer.Text?.Trim() ?? "";
        if (text.Length == 0) return;
        _composer.Text = "";
        // The user bubble is now server-authored (message-added); show only the
        // Agent reply placeholder.
        Push("Agent: ");
        try
        {
            await _api.PromptAsync(_sessionId, text, _cts.Token);
        }
        catch (Exception ex)
        {
            Append($"\n[error: {ex.Message}]");
        }
    }

    private void Append(string text)
    {
        if (_lines.Count == 0) _lines.Add(text);
        else _lines[_lines.Count - 1] += text;
        ScrollToEnd();
    }

    private void Push(string line)
    {
        _lines.Add(line);
        ScrollToEnd();
    }

    private void ScrollToEnd()
    {
        if (_lines.Count > 0) _list.ScrollTo(_lines.Count - 1, position: ScrollToPosition.End, animate: false);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts.Cancel();
    }
}
