using BlazorBattControl.FoxEss.FoxApiClient;
using BlazorBattControl.Models;
using BlazorBattControl.Octopus;
using Microsoft.EntityFrameworkCore;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace BlazorBattControl.Data
{
    public class ChargePlan
    {
        private readonly IDbContextFactory<BlazorBattControlContext> m_dbFactory;
        private readonly AgileRateValues m_agileRateValues;
        private readonly OctopusApiClient m_octopusApiClient;
        private readonly IHaContext m_ha;
        private readonly ILogger<FoxEssMain> m_logger;

        public ChargePlan(IDbContextFactory<BlazorBattControlContext> dbFactory, 
            AgileRateValues agileRateValues,
            OctopusApiClient octopusApiClient,
            IHaContext ha,
            ILogger<FoxEssMain> logger)
        {
            m_dbFactory = dbFactory;
            m_agileRateValues = agileRateValues;
            m_octopusApiClient = octopusApiClient;
            m_ha = ha;
            m_logger = logger;
        }

        private AppDbSettings? m_appDSsettings;

        public float MinExportPrice
        {
            get => m_agileRateValues.m_minExportPrice;
            set
            {

                if (m_agileRateValues.m_minExportPrice != value)
                {
                    m_agileRateValues.m_minExportPrice = value;
                    var exportEntity = new Entity(m_ha, "input_number.agile_sell_rate");
                    exportEntity.CallService("set_value", new { value = m_agileRateValues.m_minExportPrice });
                }
            }
        }
        private void RefreshAgileRates()
        {
            if (m_agileRateValues.export != null && m_agileRateValues.import != null)
            {
                var importEntity = new Entity(m_ha, "input_number.agile_import_rate");
                importEntity.CallService("set_value", new { value = m_agileRateValues.import[m_agileRateValues.currentSeg].value_inc_vat });

                var exportEntity = new Entity(m_ha, "input_number.agile_export_rate");
                exportEntity.CallService("set_value", new { value = (m_agileRateValues.export[m_agileRateValues.currentSeg].value_inc_vat) });
            }
        }

        public void RefreshAgilePlan(DateTime dateTimeNow)
        {
            m_agileRateValues.RefreshRates(dateTimeNow);
            RefreshAgileRates();

            SetMinimumExportPrice(dateTimeNow);
        }

        public void SetMinimumExportPrice(DateTime today)
        {
            var exportPrices = m_agileRateValues.export.Where(x => x.valid_from.Day == today.Day).OrderByDescending(x => x.value_inc_vat).ToList();

            var segs = new Entity(m_ha, "input_select.agile_export_time");

            switch (segs.State)
            {
                case "0:30":
                    MinExportPrice = exportPrices[1].value_inc_vat;
                    break;
                case "1:00":
                    MinExportPrice = exportPrices[2].value_inc_vat;
                    break;
                case "1:30":
                    MinExportPrice = exportPrices[3].value_inc_vat;
                    break;
                case "2:00":
                    MinExportPrice = exportPrices[4].value_inc_vat;
                    break;
                case "2:30":
                    MinExportPrice = exportPrices[5].value_inc_vat;
                    break;
                case "3:00":
                    MinExportPrice = exportPrices[6].value_inc_vat;
                    break;
                case "3:30":
                    MinExportPrice = exportPrices[7].value_inc_vat;
                    break;
                case "4:00":
                    MinExportPrice = exportPrices[8].value_inc_vat;
                    break;
            }
        }

        public void SetAgileExportSlots(bool[,] buttonStates)
        {
            for (int row = 0; row < 48; row++)
            {
                if (row < m_agileRateValues.export.Count && m_agileRateValues.export[row].value_inc_vat > MinExportPrice)
                {
                    buttonStates[4, row] = true;
                }
            }
        }

        public void InitializedSchedule(int Id)
        {
            using (var dbContext = m_dbFactory.CreateDbContext())
            {
                for (int i = 0; i < 48; i++)
                {
                    if (!dbContext.Mode.Any(m => m.SchedualId == Id && m.TimeSlot == i))
                    {
                        var newMode = new BatteryMode
                        {
                            SchedualId = Id,
                            TimeSlot = i
                            // BattMode = "Default" = 2
                        };
                        dbContext.Mode.Add(newMode);
                    }
                    else
                    {
                        if (dbContext.Mode.Count(m => m.SchedualId == Id && m.TimeSlot == i) > 1)
                        {
                            // Handle the case where there are multiple entries for the same TimeSlot
                            // You can choose to delete duplicates or log a warning, depending on your requirements
                            var duplicateModes = dbContext.Mode.Where(m => m.SchedualId == Id && m.TimeSlot == i).ToList();
                            // For example, you could keep the first one and remove the rest:
                            var modeToKeep = duplicateModes.First();
                            foreach (var duplicate in duplicateModes.Skip(1))
                            {
                                dbContext.Mode.Remove(duplicate);
                            }
                        }
                    }
                }

                dbContext.SaveChanges();
            }
        }


        public string[] ButtonNames = { "Charge ", "Backup ", "SelfUse", "Feed_In", "Dis_Chg" };
    }
}
