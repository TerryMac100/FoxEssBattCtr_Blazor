using BlazorBattControl.NetDaemon;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using NetDaemonMain.apps.FoxEss.FoxApiClient.Models;
using Newtonsoft.Json;
using RestSharp;
using System.Security.Cryptography;
using System.Text;

namespace BlazorBattControl.FoxEss.FoxApiClient;

public class FoxEssMain
{
    private readonly IHaContext m_ha;
    private readonly FoxSettings m_settings;
    private readonly IAppConfig<FoxBatteryControlSettings> m_foxBatteryControlSettings;
    private readonly ILogger<FoxEssMain> m_logger;

    public FoxEssMain(
        IHaContext ha,
        FoxSettings settings,
        IAppConfig<FoxBatteryControlSettings> foxBatteryControlSettings,
        ILogger<FoxEssMain> logger)
    {
        m_ha = ha;
        m_settings = settings;
        m_foxBatteryControlSettings = foxBatteryControlSettings;

        m_logger = logger;
    }

    private static string GenMd5Hash(string text)
    {
        //MD5 md5 = new MD5CryptoServiceProvider();
        MD5 md5 = MD5.Create();

        md5.ComputeHash(Encoding.UTF8.GetBytes(text));

        //get hash result after compute it  
        byte[] result = md5.Hash;

        StringBuilder strBuilder = new StringBuilder();
        for (int i = 0; i < result.Length; i++)
        {
            // change it into 2 hexadecimal digits  
            // for each byte  
            strBuilder.Append(result[i].ToString("x2"));
        }

        return strBuilder.ToString();
    }

    private static string ToHexString(string path, string token, string timestamp)
    {
        var myString = $"{path}\\r\\n{token}\\r\\n{timestamp}";

        return GenMd5Hash(myString);
    }

    private RestRequest GetHeader(string path, RestSharp.Method method)
    {
        var request = new RestRequest(domain + path, method);

        long time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var signature = ToHexString(path, m_settings.ApiKey, time.ToString());

        request.AddHeader("token", m_settings.ApiKey);
        request.AddHeader("signature", signature);
        request.AddHeader("timestamp", time.ToString());
        request.AddHeader("lang", lang);
        request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36");

        return request;
    }

    public async Task<GetTimeSegmentResponse?> GetSchedule()
    {
        try
        {
            var request = GetHeader("/op/v0/device/scheduler/get", RestSharp.Method.Post);

            request.AddBody(new DeviceSerialNumber(m_settings));

            var client = new RestClient();

            RestResponse response = await client.ExecuteAsync(request);

            if (response.IsSuccessful && response.Content != null)
            {
                GetTimeSegmentResponse? ret = JsonConvert.DeserializeObject<GetTimeSegmentResponse>(response.Content);
                return ret;
            }

            return null;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    private string m_callsEnabled => m_ha.Entity("input_boolean.dev_netdaemon_fox_ess_backup_active").State;

    public async void SetSchedule(SetSchedule setSchedule)
    {
        // Disable the calls in debug builds (but keep the code in place for testing)
        bool m_debugBuild = false;
#if DEBUG
        m_debugBuild = true;
#endif
        var st = JsonConvert.SerializeObject(setSchedule);
        var info = $"Sending Schedule\n{st}";
        m_logger.LogInformation(info);

        if (m_callsEnabled == "off" || m_debugBuild)
        {
            if (m_debugBuild)
                m_logger.LogInformation($"API Call disabled in debug build");
            else
                m_logger.LogInformation($"API Call disabled");
            return;
        }

        try
        {
            var request = GetHeader("/op/v0/device/scheduler/enable", RestSharp.Method.Post);

            request.AddBody(setSchedule);

            var client = new RestClient();

            RestResponse response = await client.ExecuteAsync(request);

            if (response.IsSuccessful && response.Content != null)
            {
                SetScheduleResponse? ret = JsonConvert.DeserializeObject<SetScheduleResponse>(response.Content);

                if (ret != null)
                {
                    if (ret.Errno == 0)
                        m_logger.LogInformation($"Schedule sent OK");
                    else
                    {
                        m_logger.LogWarning($"Schedule failed to send error number {ret.Errno} with message '{ret.Msg}'");
                    }
                }
                else
                    m_logger.LogWarning($"Something went wrong return value null");

                return;
            }
        }
        catch (Exception ex)
        {
            m_logger.LogWarning($"Exception while sending schedule, message '{ex.Message}'");
        }
    }

    public SetSchedule GetDefaultSchedule()
    {
        var schedule = new SetSchedule();
        schedule.DeviceSN = m_foxBatteryControlSettings.Value.DeviceSN;

        schedule.Groups = new List<SetTimeSegment>();

        foreach (var group in m_foxBatteryControlSettings.Value.DefaultSchedule.Groups)
        {
            var timeSeg = new SetTimeSegment(group);
            schedule.Groups.Add(timeSeg);
        }

        return schedule;
    }

    public SetSchedule GetBackupSegment(DateTime dateTime)
    {
        var schedule = GetDefaultSchedule();
        var section = new SetTimeSegment(dateTime);

        schedule.Groups.Add(section);

        return schedule;
    }

    public SetSchedule GetChargeSegment(DateTime dateTime)
    {
        var schedule = GetDefaultSchedule();
        var section = new SetTimeSegment(dateTime);
        section.WorkMode = "ForceCharge";

        schedule.Groups.Add(section);

        return schedule;
    }

    public bool[] GetOffPeakSegments()
    {
        bool[] sections = new bool[48];

        var defaultSchedual = GetDefaultSchedule();

        if (defaultSchedual != null && defaultSchedual.Groups != null)
            foreach (var period in defaultSchedual.Groups)
            {
                var startIndex = period.StartHour * 2;
                if (period.StartMinute >= 30)
                    startIndex += 1;

                var endIndex = period.EndHour * 2;
                if (period.EndMinute > 30)
                    endIndex += 1;

                for (int i = startIndex; i <= endIndex; i++)
                    sections[i] = true;
            }

        return sections;
    }
    private const string lang = "en";
    private const string domain = "https://www.foxesscloud.com";
}

