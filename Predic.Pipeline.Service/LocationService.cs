using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Predic.Pipeline.Interface;
using Predix.Domain.Model.Constant;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Service
{
    public class LocationService : ILocation, IImage
    {
        private readonly IPredixHttpClient _predixHttpClient = new PredixHttpClient();

        public List<Identifier> All(string bbox, string locationType, int size)
        {
            List<Identifier> identifiers = new List<Identifier>();
            int pageNumber = 0;
            int totalPages = 1;
            Dictionary<string, string> additionalHeaders =
                new Dictionary<string, string> {{"Predix-Zone-Id", "ics-IE-PARKING"}};
            while (totalPages - 1 >= pageNumber)
            {
                var response = _predixHttpClient.GetAllAsync(Endpoint.GetListOfLocation
                    .Replace("{bbox}", bbox)
                    .Replace("{locationType}", locationType)
                    .Replace("{pageNumber}", pageNumber.ToString())
                    .Replace("{pageSize}", size.ToString()), additionalHeaders);
                if (!string.IsNullOrWhiteSpace(response.Result))
                {
                    var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
                    identifiers.AddRange(jsonRespone["content"] != null
                        ? ((JArray) jsonRespone["content"]).ToObject<List<Identifier>>()
                        : new List<Identifier>());
                    totalPages = jsonRespone["totalPages"] != null ? (int) jsonRespone["totalPages"] : 0;
                    pageNumber++;
                }

            }
            return identifiers;
        }

        public async System.Threading.Tasks.Task<Details> Get(string locationUid, string eventType)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls |
                                                   SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            var cancellationTokenSource = new CancellationTokenSource();
            using (ClientWebSocket clientWebSocket = new ClientWebSocket())
            {
                Uri serverUri = new Uri(Endpoint.WebSocketUrl);
                clientWebSocket.Options.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["ClientId"], ConfigurationManager.AppSettings["ClientSecrete"]);
                try
                {
                    clientWebSocket.ConnectAsync(serverUri, cancellationTokenSource.Token).Wait(cancellationTokenSource.Token);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }
                while (clientWebSocket.State == WebSocketState.Open)
                {
                    string bodyMessage = $"{{\"locationUid\":\"{locationUid}\",\"eventTypes\":[\"{eventType}\"]}}";
                    ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(bodyMessage));
                    await clientWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);
                    ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult result = await clientWebSocket.ReceiveAsync(bytesReceived, CancellationToken.None);
                    var response = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);
                }
            }
            return null;
        }

        public void SaveLocationKeys(List<Identifier> locationKeys)
        {
            throw new NotImplementedException();
        }

        public void SaveLocationDetails(List<Details> locationDetails)
        {
            throw new NotImplementedException();
        }

        public Image MediaOnDemand(string imageAssetUid)
        {
            throw new NotImplementedException();
        }

        public void SaveImage(Image image)
        {
            throw new NotImplementedException();
        }
    }
}
