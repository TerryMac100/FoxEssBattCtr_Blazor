using BlazorBattControl.FoxEss.FoxApiClient;
using NetDaemonMain.apps.FoxEss.FoxApiClient.Models;

namespace BlazorBattControl.FoxEss;

/// <summary>
/// FoxESS API Builder
/// </summary>
public static class FoxApiClientBuilder
{
    /// <summary>
    /// Create the services for the Fox API Client
    /// </summary>
    /// <param name="hostBuilder">Builder</param>
    public static IHostBuilder AddFoxApiClientBuilder(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices(services =>
        {
            services.AddTransient<FoxEssMain>();
            services.AddTransient<FoxSettings>();
        });
        return hostBuilder;
    }
}
