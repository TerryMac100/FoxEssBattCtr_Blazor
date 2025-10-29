using BlazorBattControl.FoxEss.FoxApiClient;
using BlazorBattControl.NetDaemon;
using NetDaemon.AppModel;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;
using NetDaemonMain.apps.FoxEss.FoxApiClient.Models;
using static BlazorBattControl.FoxEss.FoxApiClient.FoxEssMain;

namespace BlazorBattControl.FoxEss;

/// <summary>
/// FoxESS Battery control state machine
/// </summary>
[NetDaemonApp]
public class FoxBatteryControl
{
    private readonly IHaContext m_ha;
    private readonly FoxEssMain m_foxEssMain;
    private readonly FoxSettings m_settings;
    private readonly ILogger<FoxBatteryControl> m_logger;
    private readonly IAppConfig<FoxBatteryControlSettings> m_foxBatteryControlSettings;

    public FoxBatteryControl(IHaContext ha,
        INetDaemonScheduler scheduler,
        FoxEssMain foxEssMain,
        FoxSettings foxSettings,
        IAppConfig<FoxBatteryControlSettings> foxBatteryControlSettings,
        ILogger<FoxBatteryControl> logger)
    {
        m_ha = ha;
        m_foxEssMain = foxEssMain;
        m_settings = foxSettings;
        m_foxBatteryControlSettings = foxBatteryControlSettings;
        m_logger = logger;

        InitialiseMonitor();

        scheduler.RunEvery(TimeSpan.FromSeconds(m_schedualRateSeconds), async () =>
        {
            RunMonitor();
        });
    }

    private void InitialiseMonitor()
    {
        m_monitorState = MonitorSchedule.Reset;
        m_logger.LogInformation($"FoxESS - Re-starting");
    }

    private int m_currentSlot
    {
        get
        {
            var time = DateTime.Now;

            var segmentIndex = time.Hour * 2;
            if (time.Minute >= 30)
                segmentIndex += 1;

            return segmentIndex;
        }
    }

    /// <summary>
    /// When the Cheap rate period starts - send default schedule
    /// If the backup flag is active - disable discharging of the battery for the current segment
    /// If the charge flag is active - enable charging of the battery for the current segment
    /// If if the charge or backup flags change to false - send default schedule
    /// When a new segment starts - extend the current mode if charge or backup active are still active
    /// </summary>
    private void RunMonitor()
    {
        var dateTimeNow = DateTime.Now;

        // Don't make any change in the first and last minutes of the period
        // This is to let the two inputs settle
        if ((dateTimeNow.Minute == 29) ||
            (dateTimeNow.Minute == 59) ||
            (dateTimeNow.Minute == 0) ||
            (dateTimeNow.Minute == 30))
            return;

        MonitorState = m_foxEssMain.SendSchedule(MonitorState);
    }

    private MonitorSchedule m_monitorState;
    private MonitorSchedule MonitorState
    {
        set
        {
            if (MonitorState == value)
                return;

            m_monitorState = value;

            switch (m_monitorState)
            {
                case MonitorSchedule.Reset:
                    m_logger.LogInformation($"FoxESS - Monitor Reset");
                    break;

                case MonitorSchedule.ChargePeriod:
                    m_logger.LogInformation($"FoxESS - Battery Charging");
                    break;

                case MonitorSchedule.BackupPeriod:
                    m_logger.LogInformation($"FoxESS - Battery Disabled");
                    break;

                case MonitorSchedule.SelfUsePeriod:
                    m_logger.LogInformation($"FoxESS - Battery Enabled");
                    break;

                case MonitorSchedule.FeedInPeriod:
                    m_logger.LogInformation($"FoxESS - Feed In Priority");
                    break;

                case MonitorSchedule.DischargePeriod:
                    m_logger.LogInformation($"FoxESS - Battery Force Discharging");
                    break;

            }
        }
        get { return m_monitorState; }
    }

    private const int m_schedualRateSeconds = 10;
}
