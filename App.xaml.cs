using Microsoft.Extensions.DependencyInjection;

namespace AQUA_SMART_IOT
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new LoadingScreen();
        }

       // protected override Window CreateWindow(IActivationState? activationState)
        //{
          //  return new Window(new AppShell());
        //}
    }
}