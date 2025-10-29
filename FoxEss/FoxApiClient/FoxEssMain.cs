using BlazorBattControl.Data;
using BlazorBattControl.NetDaemon;
using Microsoft.EntityFrameworkCore;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;
using NetDaemonMain.apps.FoxEss.FoxApiClient.Models;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Security.Cryptography;
using System.Text;

namespace BlazorBattControl.FoxEss.FoxApiClient;

public class FoxEssMain
{
    private readonly IHaContext m_ha;
    private readonly FoxSettings m_settings;
    private readonly ILogger<FoxEssMain> m_logger;

    public FoxEssMain(
        IHaContext ha,
        FoxSettings settings,
        IAppConfig<FoxBatteryControlSettings> foxBatteryControlSettings,
        ILogger<FoxEssMain> logger)
    {
        m_ha = ha;
        m_settings = settings;
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

    public void SendSelectedSchedule()
    {
        var schedule = GetSelectedSchedule();
        SetSchedule(schedule);
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
            DeviceSN = m_settings.DeviceSN
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

    public async void SetSchedule(SetSchedule setSchedule)
    {
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

    private bool m_foxChargeActive => new Entity(m_ha, m_settings.OffPeakFlagEntityID).State == "on";
    private bool m_foxBackupActive => new Entity(m_ha, m_settings.BackupFlagEntityID).State == "on";
    private bool m_foxFeedInActive => new Entity(m_ha, m_settings.FeedInPriorityFlagEntityID).State == "on";
    private bool m_foxDischargeActive => new Entity(m_ha, m_settings.DischargeFlagEntityID).State == "on";

    public MonitorSchedule SendSchedule (MonitorSchedule currentState)
    {
        MonitorSchedule requiredMode = MonitorSchedule.SelfUsePeriod;       // SelfUse
        
        var modes = GetModes();
        var dateTime = DateTime.Now;
        var seg = dateTime.Hour * 2;
        if (dateTime.Minute >= 30)
            seg += 1;

        if (m_foxChargeActive)
            requiredMode = MonitorSchedule.ChargePeriod; // Charge
        else if (m_foxBackupActive)
            requiredMode = MonitorSchedule.BackupPeriod; // Backup
        else if (m_foxFeedInActive)
            requiredMode = MonitorSchedule.FeedInPeriod; // FeedIn
        else if (m_foxDischargeActive)
            requiredMode = MonitorSchedule.DischargePeriod; // Discharge
        else
        {
            // get required mode from schedule
            requiredMode = (MonitorSchedule)modes[seg];
        }

        if (currentState == requiredMode)
        {
            // Change to schedule not required
            return requiredMode;
        }

        modes[seg] = (int)requiredMode;



        var schedule = GetScheduleFromModes(modes);
        SetSchedule(schedule);

        return requiredMode;
    }


    public void SetSegment(DateTime dateTime, int mode)
    {
        var seg = dateTime.Hour * 2;
        if (dateTime.Minute >= 30)
            seg += 1;

        var modes = GetModes();

        if (modes[seg] != mode)
        {
            modes[seg] = mode;
            var schedule = GetScheduleFromModes(modes);
            SetSchedule(schedule);
        }
        else
        {
            m_logger.LogInformation($"Segment at {dateTime} already set to mode {mode}");
        }
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

