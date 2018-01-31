using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Predix.Domain.Model.Constant;
using Predix.Pipeline.Helper;
using Predix.Pipeline.Interface;

namespace Predix.Pipeline.Service
{
    public class PredixHttpClient : IPredixHttpClient
    {
        private readonly ISecurity _securityService = new SecurityService();
        private static Dictionary<string, object> _globalVariables;
        public PredixHttpClient(Dictionary<string, object> globalVariables)
        {
            _globalVariables = globalVariables;
        }

        public int ActivityId { get; set; }

        public async Task<string> GetAllAsync(string url, Dictionary<string,string> additionalHeaders)
        {
            _securityService.SetClientToken().Wait();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls |
                                                   SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            var logging = new LoggingHandler(new HttpClientHandler());
            using (HttpClient httpClient = new HttpClient(logging))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Endpoint.ClientAccessToken);
                foreach (var additionalHeader in additionalHeaders)
                {
                    httpClient.DefaultRequestHeaders.Add(additionalHeader.Key, additionalHeader.Value);
                }
                using (HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url))
                {
                    using (HttpContent httpContent = httpResponseMessage.Content)
                    {
                        var result = await httpContent.ReadAsStringAsync();
                        if(_globalVariables.ContainsKey("ActivityId"))
                            _globalVariables["ActivityId"] = logging.ActivityId;
                        else
                        _globalVariables.Add("ActivityId", logging.ActivityId);
                        return result;
                    }
                }
            }
        }
        public async Task<string> GetFile(string url, Dictionary<string, string> additionalHeaders)
        {
            Commentary.Print($"Fething Image Base64", true);
            _securityService.SetClientToken().Wait();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls |
                                                   SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            using (HttpClient httpClient = new HttpClient(new LoggingHandler(new HttpClientHandler())))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Endpoint.ClientAccessToken);
                foreach (var additionalHeader in additionalHeaders)
                {
                    httpClient.DefaultRequestHeaders.Add(additionalHeader.Key, additionalHeader.Value);
                }
                //using (MemoryStream memoryStream = new MemoryStream())
                //{
                //    using (HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK))
                //    {
                //        result.Content = new ByteArrayContent(memoryStream.ToArray());
                //        result.Content.Headers.ContentType =
                //            new MediaTypeHeaderValue($"image/jpg");
                //        var imageUrl = $"data:image/jpg;base64," +
                //                       Convert.ToBase64String(memoryStream.ToArray(), 0, memoryStream.ToArray().Length);
                //        return imageUrl;
                //    }
                //}

                using (HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url))
                {
                    using (HttpContent httpContent = httpResponseMessage.Content)
                    {
                        var result = await httpContent.ReadAsByteArrayAsync();
                        var base64 = $"data:image/jpg;base64," +
                                       Convert.ToBase64String(result);
                        return base64;
                    }
                }
            }
        }
    }
}
