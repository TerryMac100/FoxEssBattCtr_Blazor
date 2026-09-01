using BlazorBattControl.Data;

namespace BlazorBattControl.Octopus;

public static class OctopusClientApiBuilder
{   
    public static IHostBuilder AddOctopusClientApi(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<AgileRateValues>();
            services.AddTransient<ChargePlan>();
            services.AddTransient<OctopusApiClient>();
        });

        return hostBuilder;
    }
}

