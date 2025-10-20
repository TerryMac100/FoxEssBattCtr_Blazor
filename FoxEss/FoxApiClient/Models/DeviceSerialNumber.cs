using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
