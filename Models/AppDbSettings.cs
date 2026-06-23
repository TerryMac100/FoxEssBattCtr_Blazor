using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorBattControl.Models;

public class AppDbSettings
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int SeletedScheduleId { get; set; }

    public string OffPeakFlagEntityID { get; set; } = string.Empty;

    public bool OffPeakEnabled { get; set; }

    public string BackupFlagEntityID { get; set; } = string.Empty;

    public bool BackupEnabled { get; set; }

    public string FeedInPriorityFlagEntityID { get; set; } = string.Empty;
    
    public bool FeedInPriorityEnabled { get; set; }

    public string DischargeFlagEntityID { get; set; } = string.Empty;

    public bool DischargeEnabled { get; set; }

    public string AgileDischargeFlagEntityID { get; set; } = string.Empty;

    public string AgileChargeFlagEntityID { get; set; } = string.Empty;

    public string AgileExportRateEntityID { get; set; } = string.Empty;

    public string AgileImportRateEntityID { get; set; } = string.Empty;

    public double AgileDischargeThreshold { get; set; }
    
    public double AgileChargeThreshold { get; set; }

    public bool UseOffPeakFlag { get; set; }

    public AppDbSettings Clone() 
    {
        return new AppDbSettings()
        {
            Id = Id,
            BackupFlagEntityID = BackupFlagEntityID,
            DischargeFlagEntityID = DischargeFlagEntityID,
            OffPeakFlagEntityID = OffPeakFlagEntityID,
            FeedInPriorityFlagEntityID = FeedInPriorityFlagEntityID,
            
            OffPeakEnabled = OffPeakEnabled,
            BackupEnabled = BackupEnabled,
            FeedInPriorityEnabled = FeedInPriorityEnabled,
            DischargeEnabled = DischargeEnabled,

            AgileChargeFlagEntityID = AgileChargeFlagEntityID,
            AgileChargeThreshold = AgileChargeThreshold,
            AgileDischargeFlagEntityID = AgileDischargeFlagEntityID,
            AgileDischargeThreshold = AgileDischargeThreshold
        };
    }
}
