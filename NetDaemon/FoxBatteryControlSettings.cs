namespace BlazorBattControl.NetDaemon;

public class Group
{
    public string WorkMode { get; set; }
    public int Enabled { get; set; }

    public int MinSocOnGrid { get; set; }

    public int StartHour { get; set; }
    public int StartMinute { get; set; }

    public int EndHour { get; set; }
    public int EndMinute { get; set; }

    public int FdSoc { get; set; } = 10;
    public int FdPwr { get; set; } = 0;
}

public class DefaultSchedule
{
    public Group[]? Groups { get; set; }
}

public class FoxBatteryControlSettings
{
    public string? ApiKey { get; set; }
    public string? DeviceSN { get; set; }
    public string? OffPeakFlagEntityID { get; set; }
    public DefaultSchedule? DefaultSchedule { get; set; }
}
