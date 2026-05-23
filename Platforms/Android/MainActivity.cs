using Android.App;
using Android.Content.PM;
using Android.OS;
using Plugin.LocalNotification;

namespace AQUA_SMART_IOT
{
    [Activity(Theme = "@style/Maui.SplashTheme",
              MainLauncher = true,
              LaunchMode = LaunchMode.SingleTop,
              ConfigurationChanges = ConfigChanges.ScreenSize |
                                     ConfigChanges.Orientation |
                                     ConfigChanges.UiMode |
                                     ConfigChanges.ScreenLayout |
                                     ConfigChanges.SmallestScreenSize |
                                     ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Request notification permission (Android 13+)
            await LocalNotificationCenter.Current.RequestNotificationPermission();
        }
    }
}