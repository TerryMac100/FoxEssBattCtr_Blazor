using BlazorBattControl.FoxEss;
using BlazorBattControl.Octopus;
using BlazorBattControl.Octopus.Models;

namespace BlazorBattControl.Data
{
    public class AgileRateValues
    {
        public readonly OctopusApiClient m_octopusApiClient;

        public AgileRateValues(OctopusApiClient octopusApiClient) 
        {
            m_octopusApiClient = octopusApiClient;
        }

        public void RefreshRates(DateTime now)
        {
            var seg = FoxBatteryControl.GetSegment(now);

            if (seg != currentSeg || export.Count == 0)
            { 
                currentSeg = seg;

                export = m_octopusApiClient.GetAgileExport(now);
                import = m_octopusApiClient.GetAgileImport(now);

                SetAgileValues();
            }
        }

        private void SetAgileValues()
        {
            for (int row = 0; row < 48; row++)
            {
                if (row < export.Count)
                    agileExport[row] = export[row].value_inc_vat.ToString("0.00");

                if (row < import.Count)
                    agileImport[row] = import[row].value_inc_vat.ToString("0.00");
            }

            for (int row = 0; row < 48; row++)
            {
                var seg = row;
                if (row < currentSeg)
                {
                    seg += 48;
                }

                if (seg < export.Count)
                    agileExport[row] = export[seg].value_inc_vat.ToString("0.00");

                if (seg < import.Count)
                    agileImport[row] = import[seg].value_inc_vat.ToString("0.00");
            }
        }

        public int currentSeg = -1;

        public List<CostItem> export = new List<CostItem>();
        public List<CostItem> import = new List<CostItem>();

        public string?[] agileExport = new string?[48];
        public string?[] agileImport = new string?[48];

        public float AgileSellRate { get; set; }
        public float AgileBuyRate { get; set; }
    }
}
