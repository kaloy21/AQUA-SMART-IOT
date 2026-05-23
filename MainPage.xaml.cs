using AQUA_SMART_IOT.Services;
using Microsoft.Maui.Storage;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;

namespace AQUA_SMART_IOT;

public partial class MainPage : ContentPage
{
    private readonly FirebaseService _firebase = new();

    bool tempHighNotified = false;
    bool tempLowNotified = false;

    bool turbHighNotified = false;
    bool turbLowNotified = false;

    bool phHighNotified = false;
    bool phLowNotified = false;

    bool doHighNotified = false;
    bool doLowNotified = false;
    public MainPage()
    {
        InitializeComponent();



        LoadDataAsync();

        Device.StartTimer(TimeSpan.FromSeconds(5), () =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadDataAsync();
            });

            return true;
        });
    }
    private async Task ShowNotification(string title, string message)
    {
        var request = new NotificationRequest
        {
            NotificationId = new Random().Next(1000, 9999),
            Title = title,
            Description = message,
            Android =
        {
            ChannelId = "alerts",
            Priority = AndroidPriority.High
        }
        };

        await LocalNotificationCenter.Current.Show(request);
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        SubscribeToFirebase();
        _firebase.StartListeners();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _firebase.StopListeners();
    }

    // =========================
    // REAL-TIME SUBSCRIPTIONS
    // =========================
    private void SubscribeToFirebase()
    {
        _firebase.TemperatureCChanged += v =>
            MainThread.BeginInvokeOnMainThread(() =>
                TempCLabel.Text = $"Celsius: {v:0.0} °C");

        _firebase.TemperatureFChanged += v =>
            MainThread.BeginInvokeOnMainThread(() =>
                TempFLabel.Text = $"Fahrenheit: {v:0.0} °F");

        _firebase.PhChanged += v =>
            MainThread.BeginInvokeOnMainThread(() =>
                PhLabel.Text = $"PH: {v:0.00}");

        _firebase.DoMgLChanged += v =>
            MainThread.BeginInvokeOnMainThread(() =>
                DoMgLabel.Text = $"DO mg/L: {v:0.00}");

        _firebase.TurbidityChanged += v =>
            MainThread.BeginInvokeOnMainThread(() =>
                TurbidityLabel.Text = $"Turbidity: {v}");

        _firebase.TurbiditySensorValueChanged += v =>
            MainThread.BeginInvokeOnMainThread(() =>
                TurbidityRawLabel.Text = $"Sensor: {v}");

        _firebase.TurbidityStatusChanged += v =>
            MainThread.BeginInvokeOnMainThread(() =>
                TurbidityStatusLabel.Text = $"Status: {v}");
      
        _firebase.CirculationPumpStatusChanged += v =>
            MainThread.BeginInvokeOnMainThread(() =>
                CirculationPumpLabel.Text = $"Circulation Pump {v}");

        _firebase.HeaterStatusChanged += v =>
            MainThread.BeginInvokeOnMainThread(() =>
                HeaterLabel.Text = $"Heater: {v}");

        _firebase.RefillPumpStatusChanged += v =>
            MainThread.BeginInvokeOnMainThread(() =>
                RefillPumpLabel.Text = $"Refill Pump: {v}");


    }

    // =========================
    // UI HELPERS
    // =========================






    // =========================
    // ONE-TIME LOAD
    // =========================
    private async Task LoadDataAsync()
    {
        try
        {
            //{
            //  TempCLabel.Text = $"Celsius: {await _firebase.GetTemperatureC():0.0} °C";
            //TempFLabel.Text = $"Fahrenheit: {await _firebase.GetTemperatureF():0.0} °F";

            //PhLabel.Text = $"PH: {await _firebase.GetPH():0.00}";
            //DoMgLabel.Text = $"DO mg/L: {await _firebase.GetDoMgL():0.00}";

            //TurbidityLabel.Text = $"Turbidity: {await _firebase.GetTurbidity()}";
            //TurbidityRawLabel.Text = $"Sensor: {await _firebase.GetTurbiditySensorValue()}";
            //TurbidityStatusLabel.Text = $"Status: {await _firebase.GetTurbidityStatus()}";


            //HeaterLabel.Text = $"Heater: {await _firebase.GetHeaterStatus()}";
            //RefillPumpLabel.Text = $"Refill Pump: {await _firebase.GetRefillPumpStatus()}";
            //CirculationPumpLabel.Text = $"Circulation Pump: {await _firebase.GetCirculationPumpStatus()}";

            double tempC = await _firebase.GetTemperatureC();
            double tempF = await _firebase.GetTemperatureF();
            double ph = await _firebase.GetPH();
            double doMg = await _firebase.GetDoMgL();
            double turbidity = await _firebase.GetTurbidity();

            // DISPLAY VALUES
            TempCLabel.Text = $"Celsius: {tempC:0.0} °C";
            TempFLabel.Text = $"Fahrenheit: {tempF:0.0} °F";
            PhLabel.Text = $"PH: {ph:0.00}";
            DoMgLabel.Text = $"DO mg/L: {doMg:0.00}";
            TurbidityLabel.Text = $"Turbidity: {turbidity}";
            TurbidityRawLabel.Text = $"Sensor: {await _firebase.GetTurbiditySensorValue()}";
            TurbidityStatusLabel.Text = $"Status: {await _firebase.GetTurbidityStatus()}";

            HeaterLabel.Text = $" {await _firebase.GetHeaterStatus()}";
            RefillPumpLabel.Text = $" {await _firebase.GetRefillPumpStatus()}";
            CirculationPumpLabel.Text = $" {await _firebase.GetCirculationPumpStatus()}";


            // ================= TEMPERATURE =================
            if (tempC > 32)
            {
                if (!tempHighNotified)
                {
                    await ShowNotification("Temperature Alert", "Temperature is HIGH");
                    tempHighNotified = true;
                    tempLowNotified = false;
                }
            }
            else if (tempC < 26)
            {
                if (!tempLowNotified)
                {
                    await ShowNotification("Temperature Alert", "Temperature is LOW");
                    tempLowNotified = true;
                    tempHighNotified = false;
                }
            }
            else
            {
                tempHighNotified = false;
                tempLowNotified = false;
            }


            // ================= TURBIDITY =================
            if (turbidity > 50)
            {
                if (!turbHighNotified)
                {
                    await ShowNotification("Turbidity Alert", "Turbidity is HIGH");
                    turbHighNotified = true;
                    turbLowNotified = false;
                }
            }
            else if (turbidity < 0)
            {
                if (!turbLowNotified)
                {
                    await ShowNotification("Turbidity Alert", "Turbidity is LOW");
                    turbLowNotified = true;
                    turbHighNotified = false;
                }
            }
            else
            {
                turbHighNotified = false;
                turbLowNotified = false;
            }


            // ================= PH =================
            if (ph > 8)
            {
                if (!phHighNotified)
                {
                    await ShowNotification("pH Alert", "The pH is HIGH");
                    phHighNotified = true;
                    phLowNotified = false;
                }
            }
            else if (ph < 6)
            {
                if (!phLowNotified)
                {
                    await ShowNotification("pH Alert", "The pH is LOW");
                    phLowNotified = true;
                    phHighNotified = false;
                }
            }
            else
            {
                phHighNotified = false;
                phLowNotified = false;
            }


            // ================= DISSOLVED OXYGEN =================
            if (doMg > 50)
            {
                if (!doHighNotified)
                {
                    await ShowNotification("Dissolved Oxygen Alert", "The dissolved oxygen is HIGH");
                    doHighNotified = true;
                    doLowNotified = false;
                }
            }
            else if (doMg < 5)
            {
                if (!doLowNotified)
                {
                    await ShowNotification("Dissolved Oxygen Alert", "The dissolved oxygen is LOW");
                    doLowNotified = true;
                    doHighNotified = false;
                }
            }
            else
            {
                doHighNotified = false;
                doLowNotified = false;
            }
        }
        catch
        {
            DisplayAlert("Error!","No Internet Connection.","Ok");
        }
    }
        
    // =========================
    // THEME
    // =========================
    private void ThemeToggle(object sender, ToggledEventArgs e)
    {
        Application.Current.UserAppTheme =
            e.Value ? AppTheme.Dark : AppTheme.Light;
    }
}