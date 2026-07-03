using MauiReactor;
using MauiReactor.HotReload;
using StageBuddy.App.Components;
using StageBuddy.App.Resources.Styles;
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
                .UseMauiReactorApp<HomePage>(app =>
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


            return builder.Build();
        }
    }
}
