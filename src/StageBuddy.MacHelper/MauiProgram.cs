using MauiReactor;
using MauiReactor.HotReload;
#if DEBUG
using Microsoft.Maui.DevFlow.Agent;
#endif
using StageBuddy.MacHelper.Pages;
using StageBuddy.Core.Sync;
using StageBuddy.Sync.Multipeer;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;

namespace StageBuddy.MacHelper
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiReactorApp<HelperHomePage>(app =>
                    {
                        app.UseTheme<ApplicationTheme>();
                    },
                    unhandledExceptionAction: e =>
                    {
                        System.Diagnostics.Debug.WriteLine(e.ExceptionObject);
                    })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("FluentSystemIcons-Filled.ttf", "FluentIcons");
                });

#if DEBUG
            builder.AddMauiDevFlowAgent();
#endif

            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<IPairingTokenStore>(
                _ => new NSUserDefaultsPairingTokenStore("StageBuddy.AcceptedPairingToken"));
            builder.Services.AddSingleton<ISnippetReceiver>(sp => new MultipeerSnippetReceiver(
                Environment.MachineName,
                sp.GetRequiredService<IPairingTokenStore>(),
                action => MainThread.BeginInvokeOnMainThread(action),
                sp.GetRequiredService<TimeProvider>()));

            return builder.Build();
        }
    }
}
