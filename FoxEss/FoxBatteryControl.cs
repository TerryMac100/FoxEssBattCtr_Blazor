using BlazorBattControl.FoxEss.FoxApiClient;
using BlazorBattControl.NetDaemon;
using NetDaemon.AppModel;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;
using NetDaemonMain.apps.FoxEss.FoxApiClient.Models;

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
        m_lastSlot = m_currentSlot;
        m_monitorState = MonitorSchedule.Reset;
        m_logger.LogInformation($"FoxESS - Re-starting");
        m_requestDefaultSchedule.CallService("turn_off");
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

    private bool m_overNight
    {
        get
        {
            var time = DateTime.Now.TimeOfDay;

            if (time >= new TimeSpan(23, 30, 0) || time < new TimeSpan(5, 30, 0))
                return true;

            return false;
        }
    }

    private bool m_octupusOffPeak => new Entity(m_ha, m_settings.OffPeakFlagEntityID).State == "on";
    private bool m_foxBackupActive => new Entity(m_ha, m_settings.BackupFlagEntityID).State == "on";

    private Entity m_requestDefaultSchedule => m_ha.Entity("input_boolean.dev_netdaemon_fox_ess_default_schedule_active");

    private void SendSchedule(SetSchedule schedule)
    {
        m_foxEssMain.SetSchedule(schedule);
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
        if (m_requestDefaultSchedule.State == "on")
        {
            SendSchedule(m_foxEssMain.GetSelectedSchedule());
            m_requestDefaultSchedule.CallService("turn_off");       // TurnOff();
        }

        var dateTimeNow = DateTime.Now;

        // Don't make any change in the first and last minutes of the period
        // This is to let the two inputs settle
        if ((dateTimeNow.Minute == 29) ||
            (dateTimeNow.Minute == 59) ||
            (dateTimeNow.Minute == 0) ||
            (dateTimeNow.Minute == 30))
            return;

        // Protect the concurrency of current slot/last slot
        var currentSlot = m_currentSlot;

        switch (MonitorState)
        {
            case MonitorSchedule.Reset:
                m_logger.LogInformation($"FoxESS - Reset");
                SendSchedule(m_foxEssMain.GetSelectedSchedule());

                if (m_overNight)
                    MonitorState = MonitorSchedule.OverNight;
                else
                    MonitorState = MonitorSchedule.PeakDayRate;
                break;

            case MonitorSchedule.OverNight:
                if (m_overNight == false)
                {
                    m_logger.LogInformation($"FoxESS - Off peak end");
                    MonitorState = MonitorSchedule.PeakDayRate;
                }
                break;

            case MonitorSchedule.PeakDayRate:
                if (m_overNight)
                {
                    m_logger.LogInformation($"FoxESS - Off peak started, Setting up Default Schedule");
                    SendSchedule(m_foxEssMain.GetSelectedSchedule());
                    MonitorState = MonitorSchedule.OverNight;
                }
                else
                {
                    if (m_octupusOffPeak)
                    {
                        m_logger.LogInformation("Setting up Battery Charge segment");
                        SendSchedule(m_foxEssMain.GetChargeSegment(dateTimeNow));

                        MonitorState = MonitorSchedule.ChargePeriod;
                    }
                    else
                    {
                        if (m_foxBackupActive)
                        {
                            m_logger.LogInformation("Setting up Battery Backup segment");
                            SendSchedule(m_foxEssMain.GetBackupSegment(dateTimeNow));
                            MonitorState = MonitorSchedule.BackupPeriod;
                        }
                    }
                }
                break;

            case MonitorSchedule.ChargePeriod:
                if (m_overNight)
                {
                    m_logger.LogInformation($"FoxESS - Off peak started, Setting up Default Schedule");
                    SendSchedule(m_foxEssMain.GetSelectedSchedule());
                    MonitorState = MonitorSchedule.OverNight;
                }
                else
                {
                    if (m_octupusOffPeak == false)
                    {
                        m_logger.LogInformation($"FoxESS - Setting up Default Schedule");
                        SendSchedule(m_foxEssMain.GetSelectedSchedule());

                        MonitorState = MonitorSchedule.PeakDayRate;
                    }
                    else
                    {
                        if (currentSlot != m_lastSlot)      // New slot detected
                        {
                            m_logger.LogInformation($"FoxESS - Extending charge period");
                            SendSchedule(m_foxEssMain.GetChargeSegment(dateTimeNow));
                        }
                    }
                }
                break;

            case MonitorSchedule.BackupPeriod:
                if (m_overNight)
                {
                    m_logger.LogInformation($"FoxESS - Off peak started, Setting up Default Schedule");
                    SendSchedule(m_foxEssMain.GetSelectedSchedule());
                    MonitorState = MonitorSchedule.OverNight;
                }
                else
                {
                    if (m_foxBackupActive == false)
                    {
                        m_logger.LogInformation($"FoxESS - Setting up Default Schedule");
                        SendSchedule(m_foxEssMain.GetSelectedSchedule());

                        MonitorState = MonitorSchedule.PeakDayRate;
                    }
                    else
                    {
                        if (currentSlot != m_lastSlot)      // New slot detected
                        {
                            m_logger.LogInformation($"FoxESS - Extending Backup period");
                            SendSchedule(m_foxEssMain.GetBackupSegment(dateTimeNow));
                        }
                    }
                }
                break;
        }

        m_lastSlot = currentSlot;
    }

    private int m_lastSlot;

    private MonitorSchedule m_monitorState;
    private MonitorSchedule MonitorState
    {
        set
        {
            m_monitorState = value;

            switch (m_monitorState)
            {
                case MonitorSchedule.Reset:
                    m_logger.LogInformation($"FoxESS - Monitor Reset");
                    break;

                case MonitorSchedule.OverNight:
                    m_logger.LogInformation($"FoxESS - Cheap Rate");
                    break;

                case MonitorSchedule.PeakDayRate:
                    m_logger.LogInformation($"FoxESS - Peak Rate");
                    break;

                case MonitorSchedule.ChargePeriod:
                    m_logger.LogInformation($"FoxESS - Battery Charging");
                    break;
            }
        }
        get { return m_monitorState; }
    }

    private enum MonitorSchedule
    {
        Reset,
        PeakDayRate,
        OverNight,
        ChargePeriod,
        BackupPeriod
    }

    private const int m_schedualRateSeconds = 10;
}
