﻿using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using RFExporter.Data;
using CommunityToolkit.Maui;

namespace RFExporter.UI;

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
			});

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		builder.Services.AddSingleton<ScanningService>();

        builder.ConfigureLifecycleEvents(lifecycle => {

        });

        return builder.Build();
	}
}
