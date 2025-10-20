using BlazorBattControl.NetDaemon;
using NetDaemon.AppModel;

namespace NetDaemonMain.apps.FoxEss.FoxApiClient.Models;


public class FoxSettings
{
    private readonly string m_key;
    private readonly string m_deviceSn;
    private readonly IAppConfig<FoxBatteryControlSettings> m_foxBatteryControlSettings;

    public FoxSettings(IAppConfig<FoxBatteryControlSettings> foxBatteryControlSettings)
    {
        m_foxBatteryControlSettings = foxBatteryControlSettings;
    }

    public string ApiKey => m_foxBatteryControlSettings.Value.ApiKey;
    public string DeviceSN => m_foxBatteryControlSettings.Value.ApiKey; 
}
