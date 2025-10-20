using BlazorBattControl.NetDaemon;
using NetDaemon.AppModel;
using System.Text.Json.Serialization;

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
    private readonly Group m_group;

    public SetTimeSegment(DateTime dateTime)
    {
        WorkMode = "Backup";

        MinSocOnGrid = 20;
        StartHour = dateTime.Hour;
        StartMinute = dateTime.Minute;
        EndHour = dateTime.Hour;
        
        if (dateTime.Minute < 30)
            EndMinute = 29;
        else
            EndMinute = 59;
    }

    public SetTimeSegment(Group group)
    {
        m_group = group;
        WorkMode = m_group.WorkMode;
        Enable = m_group.Enabled;
        MinSocOnGrid = m_group.MinSocOnGrid;
        StartHour = m_group.StartHour;
        StartMinute = m_group.StartMinute;
        EndHour = m_group.EndHour;
        EndMinute = m_group.EndMinute;
        FdSoc = m_group.FdSoc;
        FdPwr = m_group.FdPwr;
    }
    public int Enable { get; set; } = 1;
}
