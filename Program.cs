using BlazorBattControl.Components;
using BlazorBattControl.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetDaemon.AppModel;
using NetDaemon.Extensions.Logging;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.Extensions.Tts;
using NetDaemon.Runtime;
using System.Reflection;

namespace BlazorBattControl
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host
            .UseNetDaemonAppSettings()
            .UseNetDaemonDefaultLogging()
            .UseNetDaemonRuntime()
            .UseNetDaemonTextToSpeech()
            .ConfigureServices((_, services) =>
                services
                    .AddAppsFromAssembly(Assembly.GetExecutingAssembly())
                    .AddNetDaemonStateManager()
                    .AddNetDaemonScheduler()
            );

            builder.Services.AddDbContextFactory<BlazorBattControlContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("BlazorBattControlContext") ?? throw new InvalidOperationException("Connection string 'BlazorBattControlContext' not found.")));

            builder.Services.AddQuickGridEntityFrameworkAdapter();

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

#if DEBUG
            // Do nothing let VS create the host environment
#else
            // Release build runs as a service
            // The following lines will make this App run as a Windows Service or a Linux Daemon
            builder.Services.AddWindowsService();

            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenAnyIP(5005);   // The required http port
            });
#endif
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts(); 
                app.UseMigrationsEndPoint();
            }

            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
