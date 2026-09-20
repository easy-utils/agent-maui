using Easy.Agent.Services;

namespace Easy.Agent.Views;

public partial class ConnectPage : ContentPage
{
    private const string DefaultBase = "https://agent.agent.10.199.64.20.nip.io";

    private readonly Entry _base;
    private readonly Entry _token;
    private readonly Label _status;
    private readonly Button _connect;

    public ConnectPage()
    {
        Title = "Easy Agent";
        BackgroundColor = Color.FromArgb("#0d1117");

        _base = new Entry
        {
            Text = DefaultBase,
            TextColor = Color.FromArgb("#e6edf3"),
            PlaceholderColor = Color.FromArgb("#8b98a9"),
        };
        _token = new Entry
        {
            IsPassword = true,
            TextColor = Color.FromArgb("#e6edf3"),
            Placeholder = "tenant token",
            PlaceholderColor = Color.FromArgb("#8b98a9"),
        };
        _status = new Label
        {
            TextColor = Color.FromArgb("#d97706"),
            LineBreakMode = LineBreakMode.WordWrap,
        };
        _connect = new Button
        {
            Text = "Connect",
            BackgroundColor = Color.FromArgb("#2563eb"),
            TextColor = Colors.White,
        };
        _connect.Clicked += OnConnect;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 10,
                Children =
                {
                    new Label
                    {
                        Text = "Easy Agent",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#e6edf3"),
                        HorizontalOptions = LayoutOptions.Center,
                    },
                    new Label { Text = "Gateway URL", TextColor = Color.FromArgb("#8b98a9") },
                    _base,
                    new Label { Text = "Token", TextColor = Color.FromArgb("#8b98a9") },
                    _token,
                    _connect,
                    _status,
                },
            },
        };
    }

    private async void OnConnect(object? sender, EventArgs e)
    {
        var baseUrl = _base.Text?.Trim() ?? "";
        var token = _token.Text?.Trim() ?? "";
        if (baseUrl.Length == 0 || token.Length == 0)
        {
            _status.Text = "Enter both a gateway URL and a token.";
            return;
        }

        _connect.IsEnabled = false;
        _status.Text = "Connecting…";
        try
        {
            var api = AgentApp.CreateApi(baseUrl, token);
            await api.HealthAsync();
            _status.Text = "";
            await Navigation.PushAsync(new SessionsPage(api));
        }
        catch (Exception ex)
        {
            _status.Text = $"Connect failed: {ex.Message}";
        }
        finally
        {
            _connect.IsEnabled = true;
        }
    }
}
