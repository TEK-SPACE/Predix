using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Predic.Pipeline.Helper;
using Predic.Pipeline.Interface;
using Predix.Domain.Model.Constant;

namespace Predic.Pipeline.Service
{
    public class PredixHttpClient : IPredixHttpClient
    {
        private readonly ISecurity _securityService = new SecurityService();
        public async Task<string> GetAllAsync(string url, Dictionary<string,string> additionalHeaders)
        {
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
                using (HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url))
                {
                    using (HttpContent httpContent = httpResponseMessage.Content)
                    {
                        var result = await httpContent.ReadAsStringAsync();
                        return result;
                    }
                }
            }
        }
    }
}
