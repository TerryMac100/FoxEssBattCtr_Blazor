namespace NetDaemonMain.apps.FoxEss.FoxApiClient.Models;

public interface IFoxResponse
{
    public int Errno { get; set; }
    public string Msg { get; set; }
}
