namespace Easy.Agent;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new NavigationPage(new Views.ConnectPage())
        {
            BarBackgroundColor = Color.FromArgb("#161b22"),
            BarTextColor = Color.FromArgb("#e6edf3"),
        });
    }
}
