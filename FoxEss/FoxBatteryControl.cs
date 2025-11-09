using BlazorBattControl.FoxEss.FoxApiClient;
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

    private readonly INetDaemonScheduler m_scheduler;

    public FoxBatteryControl(
        IHaContext ha,
        INetDaemonScheduler scheduler,
        FoxEssMain foxEssMain,
        FoxSettings foxSettings,
        ILogger<FoxBatteryControl> logger)
    {
        m_ha = ha;
        m_scheduler = scheduler;
        m_foxEssMain = foxEssMain;
        m_settings = foxSettings;
        m_logger = logger;

        InitialiseMonitor();

        scheduler.RunEvery(TimeSpan.FromSeconds(m_schedualRateSeconds), () =>
        {
            RunMonitor();
            //monitor = false;
        });
    }

    private void InitialiseMonitor()
    {
        m_monitorState = MonitorSchedule.Reset;
        m_logger.LogInformation($"FoxESS Monitor - Starting");
    }

#if DEBUG
    // The name of the enable flag is presided by "dev_" in the debug build
    private bool m_callsEnabled => m_ha.Entity("input_boolean.dev_netdaemon_blazor_batt_control_fox_ess_fox_battery_control").State == "on";
#else
    private string m_callsEnabled => m_ha.Entity("input_boolean.netdaemon_blazor_batt_control_fox_ess_fox_battery_control").State;
#endif

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

        // Don't make any change in the first minutes of the period
        // This is to let the two inputs settle
        if ((dateTimeNow.Minute == 0) ||
            (dateTimeNow.Minute == 30))
            return;

        var seg = GetSegment(dateTimeNow);

        MonitorState = CheckScheduleState(MonitorState, seg);
    }

    public int GetSegment(DateTime dateTime)
    {
        var seg = dateTime.Hour * 2;
        if (dateTime.Minute >= 30)
            seg += 1;

        return seg;
    }

    private bool backupActive = false;
    private bool chargeActive = false;
    private bool dischargeActive = false;
    private bool feedActive = false;
    private bool m_foxChargeActive => new Entity(m_ha, m_settings.OffPeakFlagEntityID).State == "on";
    private bool m_foxBackupActive => new Entity(m_ha, m_settings.BackupFlagEntityID).State == "on";
    private bool m_foxFeedInActive => new Entity(m_ha, m_settings.FeedInPriorityFlagEntityID).State == "on";
    private bool m_foxDischargeActive => new Entity(m_ha, m_settings.DischargeFlagEntityID).State == "on";

    public MonitorSchedule CheckScheduleState(MonitorSchedule currentState, int seg)
    {
        var modes = m_foxEssMain.GetModesFromDb();

        if (m_foxChargeActive)
            if (chargeActive)
                return MonitorSchedule.ChargePeriod;        // Charge already set so just return
            else
            {
                chargeActive = SendModes(modes, seg, MonitorSchedule.ChargePeriod);
                if (chargeActive)
                {
                    return MonitorSchedule.ChargePeriod;
                }
            }

        if (m_foxBackupActive)
            if (backupActive)
                return MonitorSchedule.BackupPeriod;        // Charge already set so just return
            else
            {
                backupActive = SendModes(modes, seg, MonitorSchedule.BackupPeriod);
                if (backupActive)
                {
                    return MonitorSchedule.BackupPeriod;
                }
            }

        if (m_foxFeedInActive)
            if (feedActive)
                return MonitorSchedule.FeedInPeriod;        // Charge already set so just return
            else
            {
                feedActive = SendModes(modes, seg, MonitorSchedule.FeedInPeriod);
                if (feedActive)
                {
                    return MonitorSchedule.FeedInPeriod;
                }
            }

        if (m_foxDischargeActive)
            if (dischargeActive)
                return MonitorSchedule.DischargePeriod;        // Charge already set so just return
            else
            {
                dischargeActive = SendModes(modes, seg, MonitorSchedule.DischargePeriod);
                if (dischargeActive)
                {
                    return MonitorSchedule.DischargePeriod;
                }
            }

        // If the saved schedule is different to the current schedule - update the schedule
        if (modes[seg] != m_foxEssMain.LatestModes[seg])
        {
            m_foxEssMain.SetScheduleFromModes(modes);
        }

        chargeActive = false;
        backupActive = false;
        feedActive = false;
        dischargeActive = false;

        m_lastSegment = seg;

        return modes[seg];
    }

    int m_lastSegment = -1;

    private bool m_overrideInProgress
    {
        get
        {
            return (chargeActive || backupActive || feedActive || dischargeActive);
        }
    }

    private bool SendModes(MonitorSchedule[] modes, int seg, MonitorSchedule flagState)
    {
        switch (modes[seg])
        {
            // If the schedule requires a charge or discharge don't do a flag override
            case MonitorSchedule.ChargePeriod:
            case MonitorSchedule.DischargePeriod:
                return false;

            // If self use, feed in or backup and a flag is asking for something else
            // Update the schedule to the flag state
            default:
                if (modes[seg] != flagState)
                {
                    modes[seg] = flagState;
                    m_foxEssMain.SetScheduleFromModes(modes);
                    return true;
                }
                return false;
        }
    }

    private MonitorSchedule m_monitorState;
    private MonitorSchedule MonitorState
    {
        set
        {
            switch (value)
            {
                case MonitorSchedule.Reset:
                    m_settings.StatusMessage = "Unknown State";
                    break;

                case MonitorSchedule.ChargePeriod:
                    m_settings.StatusMessage = "Battery Charging";
                    break;

                case MonitorSchedule.BackupPeriod:
                    m_settings.StatusMessage = "Battery Disabled";
                    break;

                case MonitorSchedule.SelfUsePeriod:
                    m_settings.StatusMessage = "Battery Enabled";
                    break;

                case MonitorSchedule.FeedInPeriod:
                    m_settings.StatusMessage = "Feed In Priority";
                    break;

                case MonitorSchedule.DischargePeriod:
                    m_settings.StatusMessage = "Force Discharge";
                    break;
            }

            if (m_monitorState == value)
                return;

            m_monitorState = value;

            switch (m_monitorState)
            {
                case MonitorSchedule.Reset:
                    m_settings.StatusMessage = "Unknown State";
                    m_logger.LogInformation($"FoxESS - Monitor Reset");
                    break;

                case MonitorSchedule.ChargePeriod:
                    m_settings.StatusMessage = "Battery Charging";
                    m_logger.LogInformation($"FoxESS - Battery Charging");
                    break;

                case MonitorSchedule.BackupPeriod:
                    m_settings.StatusMessage = "Battery Disabled";
                    m_logger.LogInformation($"FoxESS - Battery Disabled");
                    break;

                case MonitorSchedule.SelfUsePeriod:
                    m_settings.StatusMessage = "Battery Enabled";
                    m_logger.LogInformation($"FoxESS - Battery Enabled");
                    break;

                case MonitorSchedule.FeedInPeriod:
                    m_settings.StatusMessage = "Feed In Priority";
                    m_logger.LogInformation($"FoxESS - Feed In Priority");
                    break;

                case MonitorSchedule.DischargePeriod:
                    m_settings.StatusMessage = "Force Discharge";
                    m_logger.LogInformation($"FoxESS - Battery Force Discharging");
                    break;

            }
        }
        get { return m_monitorState; }
    }

#if DEBUG
    private const int m_schedualRateSeconds = 2;
#else
    private const int m_schedualRateSeconds = 10;
#endif
}
