using BlazorBattControl.Data;
using BlazorBattControl.Models;
using Microsoft.EntityFrameworkCore;

namespace NetDaemonMain.apps.FoxEss.FoxApiClient.Models;

public class FoxSettings(IDbContextFactory<BlazorBattControlContext> dbFactory)
{
    private readonly IDbContextFactory<BlazorBattControlContext> m_dbFactory = dbFactory;
    private AppDbSettings? m_settings;
    private AppDbSettings settings
    {
        get
        {
            if (m_settings == null)
            {
                using var context = m_dbFactory.CreateDbContext();
                var settings = context.AppDbSettings.OrderBy(x => x.Id).FirstOrDefault();
            
                if (settings is null)
                    m_settings = new AppDbSettings();
                else
                    m_settings = settings;
            }

            return m_settings;
        }
    } 

    public string ApiKey => settings.FoxApiKey;
    public string DeviceSN => settings.DeviceSN;
    public int SeletedScheduleId => settings.SeletedScheduleId;
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
