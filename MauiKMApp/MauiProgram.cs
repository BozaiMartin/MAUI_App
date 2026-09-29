using CommunityToolkit.Maui;
using Plugin.Maui.OCR;
using CommunityToolkit.Maui.Camera;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;

namespace KmKiolvasasMaui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            SQLitePCL.Batteries_V2.Init();
            MauiAppBuilder builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseOcr()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitCamera()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
