using NetDaemon.AppModel;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.HassModel;

namespace BlazorBattControl.NetDaemon;

[NetDaemonApp]
public class MainLoop
{
    public MainLoop(IHaContext ha, INetDaemonScheduler scheduler, IAppConfig<HelloConfig> config)
    {
        var count = 0;
        scheduler.RunEvery(TimeSpan.FromSeconds(5), () =>
        {
            var entity = ha.Entity("switch.ewelink_ck_bl702_swp_01_7020_switch");

            var state = config.Value.HelloMessage;

            if (entity.State == state)
            {
                //entity.CallService("turn_off");
            }
            else
            {
                //entity.CallService("turn_on");
            }

            if (count++ >= 3)
            {
                //ha.CallService("notify", "persistent_notification",
                //    data: new { message = "Main Loop stopped!", title = "Main Loop" });
            }
            else
            {
                //var mes = config.Value.HelloMessage;
                //ha.CallService("notify", "persistent_notification",
                //    data: new { message = mes, title = "Main Running" });
            }
        });
    }
}
