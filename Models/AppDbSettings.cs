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

    public string BackupFlagEntityID { get; set; } = string.Empty;

    public string FeedInPriorityFlagEntityID { get; set; } = string.Empty;
    
    public string DischargeFlagEntityID { get; set; } = string.Empty;

    public bool UseOffPeakFlag { get; set; }

    public AppDbSettings Clone() 
    {
        return new AppDbSettings()
        {
            Id = Id,
            BackupFlagEntityID = BackupFlagEntityID,
            DischargeFlagEntityID = DischargeFlagEntityID,
            OffPeakFlagEntityID = OffPeakFlagEntityID,
            FeedInPriorityFlagEntityID = FeedInPriorityFlagEntityID
        };
    }
}
