using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Subzy.Helpers;
using Subzy.Services;
using Subzy.Services.Interfaces;
using Subzy.ViewModels;

namespace Subzy;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register services
		builder.Services.AddSingleton<ILoggingService, LoggingService>();
		builder.Services.AddSingleton<SettingsService>();
		builder.Services.AddSingleton<IImageProcessor, ImageProcessorService>();
		builder.Services.AddSingleton<ChangeDetectorService>();
#if ANDROID
        builder.Services.AddSingleton<IOcrService, Platforms.Android.Services.TesseractOcrService>();
#else
		// No OCR service available for other platforms
#endif
        builder.Services.AddSingleton<ITranslationService, AzureOpenAITranslationService>();
		builder.Services.AddSingleton<ITtsService, AzureTtsService>();
		builder.Services.AddSingleton<ISpeechToSpeechService, SpeechToSpeechService>();
		builder.Services.AddSingleton<ForegroundAppDetector>();
		builder.Services.AddSingleton<ColorProfileManager>();
		builder.Services.AddSingleton<ColorPickerService>();
		builder.Services.AddSingleton<WorkflowOrchestrator>();
		builder.Services.AddSingleton<PermissionHelper>();

		// Register ViewModels
		builder.Services.AddTransient<MainViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<OnboardingViewModel>();
		builder.Services.AddTransient<DebugViewModel>();

		// Register Pages
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<OnboardingPage>();
		builder.Services.AddTransient<DebugPage>();

		return builder.Build();
	}
}
