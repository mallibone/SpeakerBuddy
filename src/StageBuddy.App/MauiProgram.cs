using MauiReactor;
using MauiReactor.HotReload;
using StageBuddy.App.Pages;
using StageBuddy.App.Resources.Styles;
using StageBuddy.App.Services;
using StageBuddy.Core.Storage;
using StageBuddy.Core.Sync;
using StageBuddy.Sync.Multipeer;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using UIKit;

namespace StageBuddy.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiReactorApp<AppShell>(app =>
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
                });

            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<ITalkRepository>(_ => new FileTalkRepository(FileSystem.AppDataDirectory));
            builder.Services.AddSingleton<ITalkRunLogRepository>(_ => new FileTalkRunLogRepository(FileSystem.AppDataDirectory));
            builder.Services.AddSingleton<IPairingTokenStore>(
                _ => new NSUserDefaultsPairingTokenStore("StageBuddy.PairingToken"));
            builder.Services.AddSingleton<IRehearsalModeToggle, RehearsalModeToggle>();
            builder.Services.AddSingleton<ISnippetSender>(sp => new RehearsalAwareSnippetSender(
                liveSender: new MultipeerSnippetSender(
                    UIDevice.CurrentDevice.Name,
                    sp.GetRequiredService<IPairingTokenStore>(),
                    action => MainThread.BeginInvokeOnMainThread(action),
                    sp.GetRequiredService<TimeProvider>()),
                rehearsalSender: new LocalClipboardSnippetSender(),
                toggle: sp.GetRequiredService<IRehearsalModeToggle>()));

            return builder.Build();
        }
    }
}
