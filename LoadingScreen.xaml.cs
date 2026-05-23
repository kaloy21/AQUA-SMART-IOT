

namespace AQUA_SMART_IOT;

public partial class LoadingScreen : ContentPage
{
    private bool _isLoaded;

    public LoadingScreen()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isLoaded)
            return;

        _isLoaded = true;

        // Simulate loading (API calls, auth, etc.)
        await Task.Delay(3000);

        // Logo animation
        await logo.ScaleTo(1.05, 800, Easing.CubicInOut);
        await logo.ScaleTo(1, 800, Easing.CubicInOut);

        // Navigate to Main Page
        Application.Current!.MainPage = new MainPage();
    }
}