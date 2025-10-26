using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorBattControl.Models;

public class AppDbSettings
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int SeletedScheduleId { get; set; }

    public string DeviceSN { get; set; } = string.Empty;

    public string FoxApiKey { get; set; } = string.Empty;

    public string OffPeakFlagEntityID { get; set; } = string.Empty;

    public string BackupFlagEntityID { get; set; } = string.Empty;

    public bool UseOffPeakFlag { get; set; }
}
