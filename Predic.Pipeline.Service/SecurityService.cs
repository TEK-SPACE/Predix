using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Predic.Pipeline.Interface;
using Predix.Domain.Model.Constant;

namespace Predic.Pipeline.Service
{
    public class SecurityService : ISecurity
    {
        public async Task<string> SetClientToken()
        {
            //ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls |
                                                   SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Endpoint.OAuthToken);
                using (HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(Endpoint.AccessTokenUri))
                {
                    using (HttpContent httpContent = httpResponseMessage.Content)
                    {
                        var result = await httpContent.ReadAsStringAsync();
                        if (string.IsNullOrWhiteSpace(result)) return null;
                        var jsonResponse = JsonConvert.DeserializeObject<JObject>(result);
                        Endpoint.ClientAccessToken = jsonResponse["access_token"] == null
                            ? null
                            : (string) jsonResponse["access_token"];
                    }
                }
            }
            return Endpoint.ClientAccessToken;
        }
    }
}
