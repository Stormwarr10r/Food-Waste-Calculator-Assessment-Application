using Microsoft.Extensions.Logging;
using assesment.Services;

namespace assesment
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            //Adds Json storage
            builder.Services.AddSingleton<IFoodWasteStore, FoodWasteStore>();

            return builder.Build();
        }
    }
}
