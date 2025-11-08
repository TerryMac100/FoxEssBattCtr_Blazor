using System.Text.Json.Serialization;

namespace NetDaemonMain.apps.FoxEss.FoxApiClient.Models
{
    public class DeviceSerialNumber
    {
        private readonly FoxSettings m_settings;

        internal DeviceSerialNumber(FoxSettings settings)
        {
            m_settings = settings;
        }

        [JsonPropertyName("deviceSN")]
        public string DeviceSN => m_settings.DeviceSN;
    }
}
