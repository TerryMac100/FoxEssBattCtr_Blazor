using BlazorBattControl.NetDaemon;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;
using NetDaemonMain.apps.FoxEss.FoxApiClient.Models;
using Newtonsoft.Json;
using NuGet.Configuration;
using RestSharp;
using System.Security.Cryptography;
using System.Text;

namespace BlazorBattControl.FoxEss.FoxApiClient;

public class FoxEssMain
{
    private readonly IHaContext m_ha;
    private readonly FoxSettings m_settings;
    private readonly IConfiguration m_config;
    private readonly ILogger<FoxEssMain> m_logger;

    public FoxEssMain(
        IHaContext ha,
        FoxSettings settings,
        IAppConfig<FoxBatteryControlSettings> foxBatteryControlSettings,
        IConfiguration config,
        ILogger<FoxEssMain> logger)
    {
        m_ha = ha;
        m_settings = settings;
        m_config = config;
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

        var apiKey = m_config.GetSection("FoxEss:ApiKey").Value?.ToString() ?? string.Empty;
        var signature = ToHexString(path, apiKey, time.ToString());
        request.AddHeader("token", apiKey);

        request.AddHeader("signature", signature);
        request.AddHeader("timestamp", time.ToString());
        request.AddHeader("lang", lang);
        request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36");

        return request;
    }

    public async Task SendSelectedScheduleAsync()
    {
        var schedule = GetSelectedSchedule();
        
        await SetScheduleAsync(schedule);
    }

    private int[] GetModes()        
    {
        int[] modes = new int[48];

        // Populate the array of modes setting undefined modes to 2 (SelfUse)
        for (int index = 0; index < 48; index++)
        {
            modes[index] = m_settings.GetModeValue(index);
        }

        return modes;
    }

    public SetSchedule GetSelectedSchedule()
    {
        int[] modes = GetModes();

        return GetScheduleFromModes(modes);
    }

    private SetSchedule GetScheduleFromModes(int[] modes)
    {
        m_logger.LogInformation($"Getting ScheduleId {m_settings.SelectedScheduleId}");

        var schedule = new SetSchedule()               
        { 
            Groups = new List<SetTimeSegment>(),
            DeviceSN = m_config.GetSection("FoxEss:DeviceSN").Value?.ToString() ?? string.Empty
        };

        
        int i = 0;
        while (i < modes.Length)
        {
            int mode = modes[i];
            var segment = new SetTimeSegment(i, mode, m_settings.MinSoc, m_settings.MinDischargeSoc, m_settings.DischargePower);

            schedule.Groups.Add(segment);

            while (i < modes.Length - 1 && modes[i + 1] == mode)
                i++;

            segment.EndHour = i >> 1;
            if ((i % 2) == 1)
                segment.EndMinute = 59;
            else
                segment.EndMinute = 29;
            i++;
        }

        return schedule;
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

    public async Task SetScheduleAsync(SetSchedule setSchedule)
    {
        m_settings.StatusMessage = "Sending";

        // Disable the calls in debug builds (but keep the code in place for testing)
        bool m_debugBuild = true;
#if DEBUG
        m_debugBuild = true;
#endif
        var st = JsonConvert.SerializeObject(setSchedule);
        var info = $"Sending Schedule\n{st}";
        m_logger.LogInformation(info);

        if (m_callsEnabled == "off" || m_debugBuild)
        {
            // Simulate sending delay
            await Task.Run(() =>
            {
                Thread.Sleep(1000);
            });

            if (m_debugBuild)
                m_logger.LogInformation($"API Call disabled in debug build");
            else
                m_logger.LogInformation($"API Call disabled");

            // Update Schedule Id
            m_settings.LastScheduleId = m_settings.SelectedScheduleId;

            m_settings.StatusMessage = "OK";
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

                // Update Schedule Id
                m_settings.LastScheduleId = m_settings.SelectedScheduleId;
                return;
            }
        }
        catch (Exception ex)
        {
            m_logger.LogWarning($"Exception while sending schedule, message '{ex.Message}'");
        }
    }

    private bool m_foxChargeActive => new Entity(m_ha, m_settings.OffPeakFlagEntityID).State == "on";
    private bool m_foxBackupActive => new Entity(m_ha, m_settings.BackupFlagEntityID).State == "on";
    private bool m_foxFeedInActive => new Entity(m_ha, m_settings.FeedInPriorityFlagEntityID).State == "on";
    private bool m_foxDischargeActive => new Entity(m_ha, m_settings.DischargeFlagEntityID).State == "on";

    public int GetSegment()
    {
        var dateTime = DateTime.Now;

        var seg = dateTime.Hour * 2;
        if (dateTime.Minute >= 30)
            seg += 1;

        return seg;
    }

    private MonitorSchedule SendModes(int[] modes, int seg, MonitorSchedule state)
    {
        if (modes[seg] != (int)state)
        {
            modes[seg] = (int)state;
            SetSchedule(GetScheduleFromModes(modes));
        }
        return state;
    }

    private bool backupActive = false;
    private bool chargeActive = false;
    private bool dischargeActive = false;
    private bool feedActive = false;

    public MonitorSchedule SendSchedule(MonitorSchedule currentState)
    {
        var modes = GetModes();
        var seg = GetSegment();
        
        // Clear any set active flags if segment has changed
        if (seg != m_lastSegment)
        {
            chargeActive = false;
            backupActive = false;
            feedActive = false;
            dischargeActive = false;
        }

        m_lastSegment = seg;    // Don't need this again so set it to current segment

        if (m_foxChargeActive)
            if (chargeActive)
                return MonitorSchedule.ChargePeriod;        // Charge already set so just return
            else
            {
                chargeActive = true;
                return SendModes(modes, seg, MonitorSchedule.ChargePeriod);
            }

        if (m_foxBackupActive)
            if (backupActive)
                return MonitorSchedule.BackupPeriod;        // Charge already set so just return
            else
            {
                backupActive = true;
                return SendModes(modes, seg, MonitorSchedule.BackupPeriod);
            }

        if (m_foxFeedInActive)
            if (feedActive)
                return MonitorSchedule.FeedInPeriod;        // Charge already set so just return
            else
            {
                feedActive = true;
                return SendModes(modes, seg, MonitorSchedule.FeedInPeriod);
            }

        if (m_foxDischargeActive)
            if (dischargeActive)
                return MonitorSchedule.DischargePeriod;        // Charge already set so just return
            else
            {
                dischargeActive = true;
                return SendModes(modes, seg, MonitorSchedule.DischargePeriod);
            }

        // See if any charge active periods are set that now need clearing
        if (chargeActive || backupActive || feedActive || dischargeActive)
        {
            chargeActive = false;
            backupActive = false;
            feedActive = false;
            dischargeActive = false;
            SetSchedule(GetScheduleFromModes(modes));
        }

        return (MonitorSchedule)modes[seg];

    }

    int m_lastSegment = -1;

    private void SetSchedule(SetSchedule schedule)
    {
        var task = Task.Run(async () => await SetScheduleAsync(schedule));
    }


    public bool[] GetOffPeakSegments()
    {
        bool[] sections = new bool[48];

        var defaultSchedual = GetSelectedSchedule();

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

    public enum MonitorSchedule
    {
        ChargePeriod = 0,
        BackupPeriod = 1,
        SelfUsePeriod = 2,
        FeedInPeriod = 3,
        DischargePeriod = 4,
        Reset = 5
    }

    private const string lang = "en";
    private const string domain = "https://www.foxesscloud.com";
}

