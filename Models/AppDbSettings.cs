namespace BlazorBattControl.Models;

public class AppDbSettings
{
    public int Id { get; set; }
    public int SeletedScheduleId { get; set; }

    public string DeviceSN { get; set; } = string.Empty;

    public string FoxApiKey { get; set; } = string.Empty;

    public string OffPeakFlagEntityID { get; set; } = string.Empty;
    public bool UseOffPeakFlag { get; set; }
}
