using MauiReactor;
using MauiReactor.HotReload;
using StageBuddy.App.Pages;
using StageBuddy.App.Resources.Styles;
using StageBuddy.App.Services;
using StageBuddy.Core.Storage;
using StageBuddy.Core.Sync;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;

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
            builder.Services.AddSingleton<ISnippetSender, LocalClipboardSnippetSender>();

            return builder.Build();
        }
    }
}
