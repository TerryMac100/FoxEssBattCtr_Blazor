using System.Text.Json.Serialization;
using static BlazorBattControl.FoxEss.FoxApiClient.FoxEssMain;

namespace NetDaemonMain.apps.FoxEss.FoxApiClient.Models;

public class SetScheduleResponse : IFoxResponse
{
    [JsonPropertyName("errno")]
    public int Errno { get; set; }

    [JsonPropertyName("msg")]
    public string Msg { get; set; }
}

public class SetSchedule
{
    [JsonPropertyName("deviceSN")]
    public string DeviceSN { get; set; }

    [JsonPropertyName("groups")]
    public List<SetTimeSegment>? Groups { get; set; }
}

public class SetTimeSegment : GetTimeSegment
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="startSlot">30 Minute start time</param>
    /// <param name="mode">Battery control mode</param>
    /// <param name="minSoc">Minimum Stat of Charge</param>
    /// <param name="fdMinSoc">Force Discharge Minimum Stat of Charge</param>
    /// <param name="dcPower">Discharge Power</param>
    public SetTimeSegment(int startSlot, MonitorSchedule mode, int minSoc, int fdMinSoc, int dcPower)
    {
        StartHour = startSlot >> 1;
        if ((startSlot % 2) == 1)
            StartMinute = 30;

        MinSocOnGrid = minSoc;
        FdSoc = fdMinSoc;
        FdPwr = dcPower;

        switch (mode)
        {
            case MonitorSchedule.ChargePeriod: // SelfUse
                WorkMode = "ForceCharge";
                break;

            case MonitorSchedule.BackupPeriod: // Backup
                WorkMode = "Backup";
                break;

            case MonitorSchedule.FeedInPeriod:
                WorkMode = "Feedin";        // Note: "FeedIn" changed to "Feedin" to match expected API value
                break;
            
            case MonitorSchedule.DischargePeriod:
                WorkMode = "ForceDischarge";
                break;
            
            default:
                WorkMode = "SelfUse";
                break;
        }
    }

    public SetTimeSegment(DateTime dateTime, string workMode)
    {
        WorkMode = workMode;

        MinSocOnGrid = 20;
        StartHour = dateTime.Hour;
        StartMinute = dateTime.Minute;
        EndHour = dateTime.Hour;
        
        if (dateTime.Minute < 30)
            EndMinute = 29;
        else
            EndMinute = 59;
    }

    public int Enable { get; set; } = 1;
}
