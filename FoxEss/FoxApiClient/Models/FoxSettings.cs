using BlazorBattControl.Data;
using BlazorBattControl.FoxEss.FoxApiClient;
using BlazorBattControl.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace NetDaemonMain.apps.FoxEss.FoxApiClient.Models;

public class FoxSettings
{
    private readonly IDbContextFactory<BlazorBattControlContext> m_dbFactory;
    private readonly ILogger<FoxEssMain> m_logger;

    public FoxSettings(IDbContextFactory<BlazorBattControlContext> dbFactory, 
        ILogger<FoxEssMain> logger)
    {
        m_dbFactory = dbFactory;
        m_logger = logger;
    }

    private AppDbSettings? m_appDSsettings;
    private AppDbSettings settings
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
                    //context.Update(m_appDSsettings);
                    context.SaveChanges();
                }
            }

            return m_appDSsettings;
        }
    } 

    public int SelectedScheduleId
    {
        get 
        {
            //m_logger.LogInformation($"Getting Selected Schedule Id = {settings.SeletedScheduleId}");

            return settings.SeletedScheduleId;
        }
        set
        {
            settings.SeletedScheduleId = value;

            using var context = m_dbFactory.CreateDbContext();
            context.Attach(settings!).State = EntityState.Modified;

            try
            {
                context.SaveChanges();

                m_logger.LogInformation($"Selected Schedule Id set to {settings.SeletedScheduleId}");
            }
            catch (DbUpdateConcurrencyException)
            {

            }
            //using var context = m_dbFactory.CreateDbContext();
            //m_appDSsettings = context.AppDbSettings.OrderBy(x => x.Id).FirstOrDefault();
            //if (m_appDSsettings is not null)
            //{
            //    m_appDSsettings.SeletedScheduleId = value;
            //    context.SaveChangesAsync().Wait();
            //    m_schedule = null;      // Invalidate cached schedule
            //}
        }
    }

    public string ApiKey => settings.FoxApiKey;
    public string DeviceSN => settings.DeviceSN;
    public string OffPeakFlagEntityID => settings.OffPeakFlagEntityID;
    public bool UseOffPeakFlag => settings.UseOffPeakFlag;
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

    public int MinSoc => Schedule.MinSoc;
    public int MinDischargeSoc => Schedule.MinDischargeSoc;
    public int DischargePower => Schedule.DischargePower;
}
