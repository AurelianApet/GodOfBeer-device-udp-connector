using GodOfBeer.util;
using RestSharp;
using RestSharp.Serialization.Json;
using System;
using System.Collections.Generic;

namespace GodOfBeer.restful
{
    public class ApiClient : GenericSingleton<ApiClient>
    {
        public class ApiInfo
        {
            public string api { get; set; }
            public object resObject { get; set; }
        }
        public class ApiResponse
        {
            public int? suc { get; set; }
            public string msg { get; set; }
            public Dictionary<string, object> dataMap { get; set; }
        }

        Dictionary<Type, ApiInfo> matchDic = null;

        JsonSerializer json = new JsonSerializer();
        
        public class SendSettingResultApi
        {
            public string ip { get; set; }
            public string id { get; set; }
            public string mac { get; set; }
        }

        public class AlertApi
        {
        }

        public class UdpConnecterSuccessApi
        {
        }

        public class UdpConnecterKillApi
        {
        }

        public ApiClient()
        {
            matchDic = new Dictionary<Type, ApiInfo>();
            matchDic.Add(typeof(SendSettingResultApi), new ApiInfo() { api = "send-setting-result", resObject = new ApiResponse() });
            matchDic.Add(typeof(AlertApi), new ApiInfo() { api = "send-alert", resObject = new ApiResponse() });
            matchDic.Add(typeof(UdpConnecterSuccessApi), new ApiInfo() { api = "udpconnecter-set-success", resObject = new ApiResponse() });
            matchDic.Add(typeof(UdpConnecterKillApi), new ApiInfo() { api = "udpconnecter-kill", resObject = new ApiResponse() });
        }

        public ApiResponse PostQuery(object postData)
        {
            ApiResponse result = null;
            try
            {
                var client = new RestClient(ConfigSetting.api_server_domain);
                var request = new RestRequest(ConfigSetting.api_prefix + matchDic[postData.GetType()].api, Method.POST);
                request.AddHeader("Content-Type", "application/json; charset=utf-8");
                request.AddJsonBody(postData);
                var response = client.Execute(request);
                result = json.Deserialize<ApiResponse>(response);
            }
            catch (Exception ex)
            {
                result = new ApiResponse();
                result.suc = 0;
                result.msg = ex.Message;
                result.dataMap = null;
            }
            return result;
        }

        public ApiResponse SendSettingResultFunc(string id, string ip, string mac)
        {
            SendSettingResultApi info = new SendSettingResultApi();
            info.ip = ip;
            info.id = id;
            info.mac = mac;
            return PostQuery(info);
        }

        public ApiResponse SendAlertFunc()
        {
            AlertApi info = new AlertApi();
            return PostQuery(info);
        }

        public ApiResponse UdpConnecterSuccessFunc()
        {
            UdpConnecterSuccessApi info = new UdpConnecterSuccessApi();
            return PostQuery(info);
        }

        public ApiResponse UdpConnecterKillFunc()
        {
            UdpConnecterKillApi info = new UdpConnecterKillApi();
            return PostQuery(info);
        }
    }
}
