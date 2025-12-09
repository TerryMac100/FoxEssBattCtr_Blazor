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
        });
    }

    private void InitialiseMonitor()
    {
        m_monitorState = MonitorSchedule.Reset;
        m_logger.LogInformation($"FoxESS Monitor - Starting");
    }

    /// <summary>
    /// The main monitor loop runs periodically to check for flag state changes but is delayed when an override is active
    /// </summary>
    private void RunMonitor()
    {
        var dateTimeNow = DateTime.Now;

        // When an override is active don't check the flag states in the first minute
        // of the half and full hour to allow for the input flags to stabilize
        // Also skip the 29th and 59th minute as it not worth setting a segment that is just about to end
        if (dateTimeNow.Minute == 0 || dateTimeNow.Minute == 30 ||
            dateTimeNow.Minute == 29 || dateTimeNow.Minute == 59)
        {
            //LookAheadMonitor(dateTimeNow);
            return;
        }

        var seg = GetSegment(dateTimeNow);

        MonitorState = CheckForScheduleStateChanges(seg);
    }

    /// <summary>
    /// Looks ahead to the next segment to see if a schedule change is needed
    /// </summary>
    /// <param name="dateTimeNow"></param>
    private void LookAheadMonitor(DateTime dateTimeNow)
    {
        // Only look ahead on the minutes that are the last of the half and full hour
        // Nothing happens in the first minute of the half and full hour
        if (dateTimeNow.Minute == 0 || dateTimeNow.Minute == 30)
            return;

        var segment = GetSegment(dateTimeNow);
        
        // Look ahead to the next segment
        segment += 1;
        if (segment >= 48)
            segment = 0;

        var modes = m_foxEssMain.GetModesFromDb();

        // Check to see if a schedule change is needed for the next segment
        if (modes[segment] != m_settings.LatestModes[segment])
        {
            m_foxEssMain.SetScheduleFromModes(modes);
        }
    }

    private int GetSegment(DateTime dateTime)
    {
        var seg = dateTime.Hour * 2;
        if (dateTime.Minute >= 30)
            seg += 1;

        return seg;
    }

    private bool m_foxChargeActive => new Entity(m_ha, m_settings.OffPeakFlagEntityID).State == "on";
    private bool m_foxBackupActive => new Entity(m_ha, m_settings.BackupFlagEntityID).State == "on";
    private bool m_foxFeedInActive => new Entity(m_ha, m_settings.FeedInPriorityFlagEntityID).State == "on";
    private bool m_foxDischargeActive => new Entity(m_ha, m_settings.DischargeFlagEntityID).State == "on";

    /// <summary>
    /// Determines the current schedule state for the specified segment and updates the schedule if a state change is
    /// detected.
    /// </summary>
    /// <remarks>This method evaluates the current state of the system based on active flags and updates the
    /// schedule if the state for the specified segment has changed. If a state change is detected, the schedule is
    /// updated, and the override flag is set to <see langword="true"/>; otherwise, the override flag is set to <see
    /// langword="false"/>.</remarks>
    /// <param name="segment">The index of the schedule segment to evaluate.</param>
    /// <returns>The updated <see cref="MonitorSchedule"/> state for the specified segment.</returns>
    public MonitorSchedule CheckForScheduleStateChanges(int segment)
    {
        var modes = m_foxEssMain.GetModesFromDb();
        if (m_foxChargeActive)
            modes[segment] = MonitorSchedule.ChargePeriod;
        else if (m_foxBackupActive && modes[segment] != MonitorSchedule.ChargePeriod) // Charge has priority over backup
            modes[segment] = MonitorSchedule.BackupPeriod;
        else if (m_foxFeedInActive)
            modes[segment] = MonitorSchedule.FeedInPeriod;
        else if (m_foxDischargeActive)
            modes[segment] = MonitorSchedule.DischargePeriod;

        // Check for a state change for the current segment
        if (modes[segment] != m_settings.LatestModes[segment])
        {
            m_foxEssMain.SetScheduleFromModes(modes);
        }
        
        return modes[segment];
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
