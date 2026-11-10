using BlazorBattControl.FoxEss;
using BlazorBattControl.Models;
using BlazorBattControl.Octopus;
using BlazorBattControl.Octopus.Models;
using Microsoft.EntityFrameworkCore;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace BlazorBattControl.Data
{
    public class AgileRateValues
    {
        private readonly IDbContextFactory<BlazorBattControlContext> m_dbFactory;
        private readonly OctopusApiClient m_octopusApiClient;

        public AgileRateValues(OctopusApiClient octopusApiClient, IDbContextFactory<BlazorBattControlContext> dbFactory) 
        {
            m_octopusApiClient = octopusApiClient;
            m_dbFactory = dbFactory;
        }

        public void RefreshRates(DateTime now)
        {
            var seg = FoxBatteryControl.GetSegment(now);

            if (seg != currentSeg || export.Count == 0)
            { 
                currentSeg = seg;

                export = m_octopusApiClient.GetAgileExport(now);
                import = m_octopusApiClient.GetAgileImport(now);

                SetAgileValues(now);
            }
        }

        private void SetAgileValues(DateTime now)
        {
            for (int row = 0; row < 48; row++)
            {
                var slotDate = now.Date.AddDays(row < currentSeg ? 1 : 0).AddMinutes(row * 30);
                var exportSlot = export.FirstOrDefault(rate => rate.valid_from.ToLocalTime() == slotDate);
                var importSlot = import.FirstOrDefault(rate => rate.valid_from.ToLocalTime() == slotDate);

                agileExport[row] = exportSlot?.value_inc_vat.ToString("0.00");
                agileImport[row] = importSlot?.value_inc_vat.ToString("0.00");
            }
        }
              
        public void RefreshModes()
        {
            Modes.Clear();

            using var context = m_dbFactory.CreateDbContext();
            var modes = context.Mode.Where(m => m.SchedualId == settings.SeletedScheduleId).ToList();
            Modes.AddRange(modes);
        }

        public int GetModeForTimeSlot(int timeSlot)
        {
            if (Modes == null || !Modes.Any())
            {
                RefreshModes();
            }

            var mode = Modes.FirstOrDefault(m => m.TimeSlot == timeSlot);
            return mode?.BattMode ?? 2; // Default to Auto if not found
        }

        public AppDbSettings settings
        {
            get
            {
                
                using var context = m_dbFactory.CreateDbContext();
                AppDbSettings m_appDSsettings = context.AppDbSettings.OrderBy(x => x.Id).FirstOrDefault();

                if (m_appDSsettings is null)
                {
                    m_appDSsettings = new AppDbSettings();
                    context.AppDbSettings.Add(m_appDSsettings);
                    context.SaveChanges();
                }
               
                return m_appDSsettings;
            }
        }

        /// <summary>
        /// Gets or sets the minimum export price for the current day. 
        /// This value is used to determine whether to export energy based on the current market price.
        /// Saved here as this is a singleton value.
        /// </summary>
        public float m_minExportPrice;
        public float m_maxImportPrice;

        public int currentSeg = -1;

        public List<CostItem> export = new List<CostItem>();
        public List<CostItem> import = new List<CostItem>();

        public string?[] agileExport = new string?[48];
        public string?[] agileImport = new string?[48];

        public List<BatteryMode> Modes { get; set; } = new List<BatteryMode>();
        public bool[,] buttonStates = new bool[5, 48];
    }
}
