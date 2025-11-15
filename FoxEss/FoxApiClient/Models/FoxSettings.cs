using BlazorBattControl.Data;
using BlazorBattControl.FoxEss.FoxApiClient;
using BlazorBattControl.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using static BlazorBattControl.FoxEss.FoxApiClient.FoxEssMain;

namespace NetDaemonMain.apps.FoxEss.FoxApiClient.Models;

public class FoxSettings : INotifyPropertyChanged
{
    private readonly IDbContextFactory<BlazorBattControlContext> m_dbFactory;
    private readonly ILogger<FoxEssMain> m_logger;

    public FoxSettings(IDbContextFactory<BlazorBattControlContext> dbFactory, 
        ILogger<FoxEssMain> logger)
    {
        m_dbFactory = dbFactory;
        m_logger = logger;

        // Initialize LatestModes to default value - self-use period
        foreach (var index in Enumerable.Range(0, 48))
        {
            LatestModes[index] = MonitorSchedule.SelfUsePeriod;
        }
    }
    
    public BatteryMode? GetMode(int index, int scheduleId)
    {
        using var dbContext = m_dbFactory.CreateDbContext();
        var mode = dbContext.Mode.FirstOrDefault(m => m.TimeSlot == index && m.SchedualId == scheduleId);

        return mode;
    }

    public MonitorSchedule GetModeValue(int index, int scheduleId)
    {
        var mode = GetMode(index, scheduleId);
        if (mode is null)
            return MonitorSchedule.SelfUsePeriod;

        return (MonitorSchedule)mode.BattMode;
    }

    public void SetMode(int index, int modeValue, int scheduleId)
    {
        using var dbContext = m_dbFactory.CreateDbContext();

        var mode = dbContext.Mode.FirstOrDefault(m => m.TimeSlot == index && m.SchedualId == scheduleId);
        if (mode is null)
        {
            mode = new BatteryMode
            {
                TimeSlot = index,
                BattMode = modeValue,
                SchedualId = SelectedScheduleId
            };
            dbContext.Mode.Add(mode);
        }
        else
        {
            mode.BattMode = modeValue;
            dbContext.Mode.Update(mode);
        }

        dbContext.SaveChanges();
    }

    public void UpdateSettings()
    {
        using var context = m_dbFactory.CreateDbContext();

        try
        {
            context.Attach(settings!).State = EntityState.Modified;         
            context.SaveChanges();
            m_logger.LogInformation($"Selected Schedule Id set to {settings.SeletedScheduleId}");
        }
        catch (DbUpdateConcurrencyException)
        {
            m_logger.LogError("Error updating AppDbSettings in database.");
        }

        m_logger.LogInformation("AppDbSettings updated successfully.");
    }

    private AppDbSettings? m_appDSsettings;
    public AppDbSettings settings
    {
        get
        {
            if (m_appDSsettings == null)
            {
                using var context = m_dbFactory.CreateDbContext();
                m_appDSsettings = context.AppDbSettings.OrderBy(x => x.Id).FirstOrDefault();

                if (m_appDSsettings is null)
                {
                    m_appDSsettings = new AppDbSettings();
                    context.AppDbSettings.Add(m_appDSsettings);
                    context.SaveChanges();
                }
            }

            return m_appDSsettings;
        }
    }

    // The user selected Schedule is here as this is the only singleton
    public int selectorId;

    public void SaveSelector()
    {
        SelectedScheduleId = selectorId;

        m_schedule = null; // Force reload of schedule

        UpdateSettings();
    }

    public int SelectedScheduleId
    {
        get 
        {
            return settings.SeletedScheduleId;
        }
        set
        {
            settings.SeletedScheduleId = value;
        }
    }

    public string OffPeakFlagEntityID
    {
        get
        {
            return settings.OffPeakFlagEntityID;
        }
        set
        {
            settings.OffPeakFlagEntityID = value;
        }
    }

    public string BackupFlagEntityID
    {
        get
        {
            return settings.BackupFlagEntityID;
        }
        set
        {
            settings.BackupFlagEntityID = value;
        }
    }
    
    public string FeedInPriorityFlagEntityID
    {
        get
        {
            return settings.FeedInPriorityFlagEntityID;
        }
        set
        {
            settings.FeedInPriorityFlagEntityID = value;
        }
    }

    public string DischargeFlagEntityID
    {
        get
        {
            return settings.DischargeFlagEntityID;
        }
        set
        {
            settings.DischargeFlagEntityID = value;
        }
    }

    public bool UseOffPeakFlag
    {
        get
        {
            return settings.UseOffPeakFlag;
        }
        set
        {
            settings.UseOffPeakFlag = value;
        }
    }

    private Schedule? m_schedule;

    public Schedule Schedule
    {
        get
        {
            if (m_schedule == null)
            {
                using var context = m_dbFactory.CreateDbContext();
                var schedule = context.Schedule.FirstOrDefault(s => s.Id == settings.SeletedScheduleId);
                if (schedule is null)
                    m_schedule = new Schedule();
                else
                    m_schedule = schedule;
            }
            return m_schedule;
        }
    }

    public List<Schedule> AllSchedules
    {
        get
        {
            using var context = m_dbFactory.CreateDbContext();
            return context.Schedule.ToList();
        }
    }

    public int MinSoc => Schedule.MinSoc;
    public int MinDischargeSoc => Schedule.MinDischargeSoc;
    public int DischargePower => Schedule.DischargePower;

    private string m_statusMessage = string.Empty;

    /// <summary>
    /// Gets or sets the collection of the latest schedules as MonitorSchedule array
    /// Saved here as this is the only singleton
    /// </summary>
    public MonitorSchedule[] LatestModes { get; set; } = new MonitorSchedule[48];

    public string StatusMessage
    {
        get
        {
            return m_statusMessage;
        }
        set
        {
            m_statusMessage = value;
            if (PropertyChanged != null)
            {

                PropertyChanged(this, new PropertyChangedEventArgs(nameof(StatusMessage)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
