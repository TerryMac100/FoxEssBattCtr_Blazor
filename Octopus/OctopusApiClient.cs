using BlazorBattControl.Octopus.Models;
using Newtonsoft.Json;
using RestSharp;

namespace BlazorBattControl.Octopus;

public class OctopusApiClient
{
    public List<Product>? GetProductList()
    {
        try
        {
            var request = new RestRequest(productList, RestSharp.Method.Get);

            RestResponse response;
            var client = new RestClient();
            response = client.Execute(request);

            if (response != null &&
                response.IsSuccessStatusCode &&
                response.StatusCode == System.Net.HttpStatusCode.OK &&
                response.Content != null)
            {
                var ret = JsonConvert.DeserializeObject<Products>(response.Content);

                return ret?.results.ToList();
            }

        }
        catch (Exception)
        {
        }
        return null;
    }

    public List<CostItem> GetAgileImport(DateTime now)
    {
        try
        {
            var from = now.Date;

            if (from.IsDaylightSavingTime())
                from = from.AddHours(-1);

            var to = from.AddMinutes(1439 + 1440);

            var period = $"period_from={from.ToString("yyyy-MM-dd'T'HH:mm'Z'")}&period_to={to.ToString("yyyy-MM-dd'T'HH:mm'Z'")}";

            var requestSt = string.Format($"{agileImport}{period}");

            var request = new RestRequest(requestSt, RestSharp.Method.Get);

            RestResponse response;
            var client = new RestClient();
            response = client.Execute(request);

            if (response != null &&
                response.IsSuccessStatusCode &&
                response.StatusCode == System.Net.HttpStatusCode.OK &&
                response.Content != null)
            {
                var ret = JsonConvert.DeserializeObject<AgileRates>(response.Content);

                return ret?.results.OrderBy(r => r.valid_from).ToList();
            }
        }
        catch (Exception)
        {
        }
        return new List<CostItem>();
    }

    public List<CostItem> GetAgileExport(DateTime now)
    {
        try
        {
            var from = now.Date;
            if (from.IsDaylightSavingTime())
                from = from.AddHours(-1);

            var to = from.AddMinutes(1439 + 1440);

            var period = $"period_from={from.ToString("yyyy-MM-dd'T'HH:mm'Z'")}&period_to={to.ToString("yyyy-MM-dd'T'HH:mm'Z'")}";

            var requestSt = string.Format($"{agileExport}{period}");

            var request = new RestRequest(requestSt, RestSharp.Method.Get);

            RestResponse response;
            var client = new RestClient();
            response = client.Execute(request);

            if (response != null &&
                response.IsSuccessStatusCode &&
                response.StatusCode == System.Net.HttpStatusCode.OK &&
                response.Content != null)
            {
                var ret = JsonConvert.DeserializeObject<AgileRates>(response.Content);

                return ret?.results.OrderBy(r => r.valid_from).ToList();
            }
        }
        catch (Exception)
        {
        }
        return new List<CostItem>();
    }

    private const string productList = "https://api.octopus.energy/v1/products?is_green=true&brand=OCTOPUS_ENERGY";

    private const string agileExport = "https://api.octopus.energy/v1/products/AGILE-OUTGOING-19-05-13/electricity-tariffs/E-1R-AGILE-OUTGOING-19-05-13-C/standard-unit-rates/?";  
    private const string agileImport = "https://api.octopus.energy/v1/products/AGILE-24-10-01/electricity-tariffs/E-1R-AGILE-24-10-01-C/standard-unit-rates/?";
}

