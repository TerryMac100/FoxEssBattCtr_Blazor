namespace BlazorBattControl.Octopus;

public static class OctopusClientApiBuilder
{   
    public static IHostBuilder AddOctopusClientApi(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices(services =>
        {
            services.AddTransient<OctopusApiClient>();
        });

        return hostBuilder;
    }
}

