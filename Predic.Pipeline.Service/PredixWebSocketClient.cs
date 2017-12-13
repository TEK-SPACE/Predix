using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Predic.Pipeline.DataService;
using Predic.Pipeline.Helper;
using Predic.Pipeline.Interface;
using Predix.Domain.Model.Constant;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Service
{
    public class PredixWebSocketClient : IPredixWebSocketClient
    {
        private readonly ISecurity _securityService = new SecurityService();

        public async Task OpenAsync(string url, string bodyMessage,
            Dictionary<string, string> additionalHeaders, IImage imageService)
        {
            _securityService.SetClientToken().Wait();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls |
                                                   SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            var cancellationTokenSource = new CancellationTokenSource(new TimeSpan(1, 1, 0, 0));
            using (ClientWebSocket clientWebSocket = new ClientWebSocket())
            {
                Uri serverUri = new Uri(url);
                clientWebSocket.Options.SetRequestHeader("Authorization", $"Bearer {Endpoint.ClientAccessToken}");
                foreach (var additionalHeader in additionalHeaders)
                {
                    clientWebSocket.Options.SetRequestHeader(additionalHeader.Key, additionalHeader.Value);
                }
                try
                {
                    clientWebSocket.ConnectAsync(serverUri, cancellationTokenSource.Token)
                        .Wait(cancellationTokenSource.Token);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }
                while (clientWebSocket.State == WebSocketState.Open)
                {
                    string response = null;
                    try
                    {
                        ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(bodyMessage));
                        await clientWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true,
                            CancellationToken.None);
                        byte[] incomingData = new byte[1024];
                        WebSocketReceiveResult result =
                            clientWebSocket.ReceiveAsync(new ArraySegment<byte>(incomingData), CancellationToken.None).Result;
                        if (result.CloseStatus.HasValue)
                        {
                            Console.WriteLine("Closed; Status: " + result.CloseStatus + ", " + result.CloseStatusDescription);
                        }
                        else
                        {
                            response = Encoding.UTF8.GetString(incomingData, 0, result.Count);
                            Console.WriteLine("Received message: " + response);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    if (string.IsNullOrWhiteSpace(response)) continue;
                    var jsonRespone = JsonConvert.DeserializeObject<JObject>(response);
                    ParkingEvent parkingEvent = jsonRespone != null
                        ? (jsonRespone).ToObject<ParkingEvent>()
                        : new ParkingEvent();
                    //SaveAsync(parkingEvent).RunSynchronously();
                    imageService.MediaOnDemand(parkingEvent.Properties.ImageAssetUid, parkingEvent.Timestamp);
                }
            }
        }

        private async Task SaveAsync(ParkingEvent parkingEvent)
        {
            if (parkingEvent == null)
                return;
            using (PredixContext context = new PredixContext())
            {
                context.ParkingEvents.AddOrUpdate(x => new { x.LocationUid, x.EventType }, parkingEvent);
                await context.SaveChangesAsync();
            }
        }
    }
}