using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;
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
    private readonly IConfiguration m_config;
    private readonly ILogger<FoxEssMain> m_logger;

    public FoxEssMain(
        IHaContext ha,
        FoxSettings settings,
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

    /// <summary>
    /// Sends the currently selected schedule to the appropriate destination asynchronously.
    /// </summary>
    /// <remarks>This method retrieves the latest modes from the database, generates a schedule based on those
    /// modes,  and sends the schedule using an asynchronous operation.</remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendSelectedScheduleAsync()
    {
        var modes = GetModesFromDb();

        var schedule = GetScheduleFromModes(modes);
        var status = await SetScheduleAsync(schedule);

        if (status)
        {
            m_settings.LatestModes = modes;
        }
    }

    /// <summary>
    /// Updates the current schedule based on the provided monitor modes.
    /// </summary>
    public void SetScheduleFromModes(MonitorSchedule[] modes)
    {
        var schedule = GetScheduleFromModes(modes);
        var status = SetSchedule(schedule);

        if (status)
        {
            m_settings.LatestModes = modes;
        }
    }

    /// <summary>
    /// Creates a SetSchedule object from an array of MonitorSchedule modes.
    /// </summary>
    /// <param name="modes">The supplied modes</param>
    /// <returns>The SetSchedule object</returns>
    private SetSchedule GetScheduleFromModes(MonitorSchedule[] modes)
    {
        var schedule = new SetSchedule()               
        { 
            Groups = new List<SetTimeSegment>(),
            DeviceSN = m_config.GetSection("FoxEss:DeviceSN").Value?.ToString() ?? string.Empty
        };
      
        int i = 0;
        while (i < modes.Length)
        {
            MonitorSchedule mode = modes[i];
            var segment = new SetTimeSegment(i, mode, m_settings.MinSoc, m_settings.MinDischargeSoc, m_settings.DischargePower);

            while (i < modes.Length - 1 && modes[i + 1] == mode)
                i++;

            segment.EndHour = i >> 1;
            if ((i % 2) == 1)
                segment.EndMinute = 59;
            else
                segment.EndMinute = 29;

            // The default if nothing is defined is mode 2 "SelfUse"
            if (mode != MonitorSchedule.SelfUsePeriod)
            {
                schedule.Groups.Add(segment);
            }

            i++;
        }

        return schedule;
    }

    /// <summary>
    /// Gets the currently selected modes from the database.
    /// </summary>
    /// <returns>The schedule as an array of modes</returns>
    public MonitorSchedule[] GetModesFromDb()        
    {
        MonitorSchedule[] modes = new MonitorSchedule[48];

        // Populate the array of modes setting undefined modes to 2 (SelfUse)
        for (int index = 0; index < 48; index++)
        {
            modes[index] = m_settings.GetModeValue(index, m_settings.SelectedScheduleId);
        }

        return modes;
    }

    /// <summary>
    /// Schedules a task to update the schedule asynchronously.
    /// </summary>
    /// <remarks>This method initiates an asynchronous operation to update the schedule.  The operation runs on a
    /// separate task and does not block the calling thread.</remarks>
    /// <param name="schedule">The schedule details to be set.</param>
    private bool SetSchedule(SetSchedule schedule)
    {
        Task<bool> t = Task<bool>.Run(async () =>
        {
            return await SetScheduleAsync(schedule).ConfigureAwait(true);        
        });

        t.Wait();

        return t.Result;
    }

    private bool m_reTryTestFlag => new Entity(m_ha, "input_boolean.fox_ess_re_try_test_flag").State == "on";


    // Any class decorated with the [NetDaemonApp] attribute will automatically generate a flag in Home Assistant if connected
    // with the following format BlazorBattControl (the project name) + BlazorBattControl.FoxEss (the name space) +
    // FoxBatteryControl (the class name) where camel case are converted to low case and separated by _ character
    // So the battery control background class the following boolean flag is created
    // 'input_boolean.netdaemon_fox_batt_control_api_enable' for release build and 
    // 'input_boolean.dev_netdaemon_fox_batt_control_api_enable' for the debug build
    // Setting the flag to false disables battery control background loop so we use the same flag to disable calls to
#if DEBUG
    // The name of the enable flag is presided by "dev_" in the debug build
    private bool apiCallEnabled => m_ha.Entity("input_boolean.dev_netdaemon_fox_batt_control_api_enable").State == "on";
#else
    private bool apiCallEnabled => m_ha.Entity("input_boolean.netdaemon_fox_batt_control_api_enable").State == "on";
#endif

    private async Task<bool> SetScheduleAsync(SetSchedule setSchedule)
    {
        m_settings.StatusMessage = "Sending";

        // Disable the calls in debug builds (but keep the code in place for testing)
        bool m_debugBuild = false;
#if DEBUG
        m_debugBuild = true;
#endif
        var st = JsonConvert.SerializeObject(setSchedule);
        var info = $"Sending Schedule\n{st}";
        m_logger.LogInformation(info);

        bool returnValue = false;

        int retry = 0;
        while (retry++ < retryRequest && returnValue == false)
        {
            if (retry > 1)
                m_logger.LogWarning($"Re-try {retry - 1}");

            try
            {
                var request = GetHeader("/op/v1/device/scheduler/enable", RestSharp.Method.Post);
                request.AddBody(setSchedule);

                RestResponse response;

                // In debug builds or if the api call is disabled we simulate a successful response
                if (apiCallEnabled == false || m_debugBuild == true)
                {
                    response = new RestResponse();
                    response.ResponseStatus = ResponseStatus.Completed;
                    response.IsSuccessStatusCode = true;
                    response.StatusCode = System.Net.HttpStatusCode.OK;
                    response.Content = JsonConvert.SerializeObject(new SetScheduleResponse() { Errno = 0, Msg = "Ok"});
                }
                else
                {
                    var client = new RestClient();
                    response = await client.ExecuteAsync(request);             
                }

                // Simulate various error conditions for testing re-try logic
                if (m_reTryTestFlag)
                {
                    switch (retry)
                    {
                        case 1:
                            response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                            break;

                        case 2:
                            response.IsSuccessStatusCode = false;
                            break;

                        case 3:
                            response.Content = null;
                            response.StatusCode = System.Net.HttpStatusCode.OK;
                            break;

                        case 4:
                            response.Content = "{\"errno\":123,\"msg\":\"Simulated Error\"}";
                            response.StatusCode = System.Net.HttpStatusCode.OK;
                            break;

                        default:
                            throw new Exception("Simulated API Exception throw");
                    }
                }

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    m_settings.StatusMessage = "Sending Error";
                    m_logger.LogWarning($"Send Error, status code {response.StatusCode}");
                }
                else if (response.IsSuccessful == false)
                {
                    m_settings.StatusMessage = "Sending Error";
                    m_logger.LogWarning($"Send Error, IsSuccessful false");
                }
                else if (response.Content == null)
                {
                    m_settings.StatusMessage = "Sending Error";
                    m_logger.LogWarning($"Send Error, return value null");
                }
                else
                {
                    SetScheduleResponse? ret = JsonConvert.DeserializeObject<SetScheduleResponse>(response.Content);

                    if (ret != null)
                    {
                        if (ret.Errno != 0)
                        {
                            m_settings.StatusMessage = $"Sending Error No. {ret.Errno}";
                            m_logger.LogWarning($"Send error number {ret.Errno} with message '{ret.Msg}'");
                        }
                        else
                        {
                            // Everything OK
                            if (apiCallEnabled == false || m_debugBuild == true)
                            {
                                if (m_debugBuild)
                                    m_logger.LogInformation($"API Calls Disabled in debug");
                                else
                                    m_logger.LogInformation($"API Calls Disabled by flag");  
                                m_settings.StatusMessage = "API Call Disabled";
                            }
                            else
                            {
                                m_logger.LogInformation($"Schedule sent OK");
                                m_settings.StatusMessage = "Schedule sent OK";                              
                            }

                            returnValue = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_settings.StatusMessage = "Sending Error";
                m_logger.LogWarning($"Exception while sending schedule, message '{ex.Message}'");
            }       
        }

        return returnValue;
    }

    private const int backOffMax = 4;

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

    private const int retryRequest = 5;
}

