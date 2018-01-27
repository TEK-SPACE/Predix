using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Predic.Pipeline.DataService;
using Predic.Pipeline.Helper;
using Predic.Pipeline.Interface;
using Predix.Domain.Model;
using Predix.Domain.Model.Constant;
using Predix.Domain.Model.Enum;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Service
{
    public class PredixWebSocketClient : IPredixWebSocketClient
    {
        private readonly ISecurity _securityService = new SecurityService();

        public async Task OpenAsync(string url, string bodyMessage,
            Dictionary<string, string> additionalHeaders, IImage imageService, bool ignoreRegulationCheck)
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
                    Commentary.Print(exception.ToString());
                }

                while (clientWebSocket.State == WebSocketState.Open)
                {
                    Commentary.Print($"Opened Socket Connection");
                    string response = null;
                    try
                    {
                        ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(bodyMessage));
                        await clientWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true,
                            CancellationToken.None);
                        byte[] incomingData = new byte[1024];
                        WebSocketReceiveResult result =
                            clientWebSocket.ReceiveAsync(new ArraySegment<byte>(incomingData), CancellationToken.None)
                                .Result;
                        if (result.CloseStatus.HasValue)
                        {
                            Console.WriteLine("Closed; Status: " + result.CloseStatus + ", " +
                                              result.CloseStatusDescription);
                        }
                        else
                        {
                            response = Encoding.UTF8.GetString(incomingData, 0, result.Count);
                            //Console.WriteLine("Received message: " + response);
                            Commentary.Print($"Received Message");
                        }
                    }
                    catch (Exception exception)
                    {
                        Commentary.Print(exception.ToString());
                        //Commentary.Print($"WebSocket State:{clientWebSocket.State}");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(response)) continue;
                    var jsonRespone = JsonConvert.DeserializeObject<JObject>(response);
                    ParkingEvent parkingEvent = jsonRespone != null
                        ? (jsonRespone).ToObject<ParkingEvent>()
                        : new ParkingEvent();
                    Commentary.Print($"Location ID :{parkingEvent.LocationUid}");
                    if (ignoreRegulationCheck)
                    {
                        Commentary.Print($"Skipping Regulation Check Alg", true);
                        Save(parkingEvent);
                        imageService.MediaOnDemand(parkingEvent.Properties.ImageAssetUid, parkingEvent.Timestamp);
                        continue;
                    }

                    var isVoilation = false;
                    var storeForDurationCheck = false;
                    using (var context = new PredixContext())
                    {
                        var nodeMasterRegulations = context.NodeMasterRegulations.Include(x => x.ParkingRegulation)
                            .Include(x => x.NodeMaster).ToList();

                        //check if GEO Coordinates match

                        var nodeMasterRegulation =
                            nodeMasterRegulations.Where(x => x.NodeMaster.LocationUid == parkingEvent.LocationUid)
                                .ToList();
                        if (nodeMasterRegulation.Any())
                        {
                            foreach (var regulation in nodeMasterRegulation.Select(x => x.ParkingRegulation))
                            {
                                if (regulation.DayOfWeek.Split('|')
                                    .Contains(DateTime.Now.DayOfWeek.ToString().Substring(0, 3)))
                                {
                                    if (DateTime.Now.TimeOfDay >= regulation.StartTime &&
                                        DateTime.Now.TimeOfDay <= regulation.EndTime)
                                    {
                                        if (!regulation.ParkingAllowed)
                                        {
                                            isVoilation = true;

                                            GeViolation geViolation = new GeViolation
                                            {
                                                NodeId = nodeMasterRegulation.First().NodeMasterId,
                                                ExceedParkingLimit =
                                                    regulation.ViolationType == ViolationType.ExceedParkingLimit,
                                                NoParking = regulation.ViolationType == ViolationType.NoParking,
                                                StreetSweeping =
                                                    regulation.ViolationType == ViolationType.StreetSweeping,
                                                //ParkinTime =
                                                //    parkingEvent.EventType.Equals("PKIN") ? DateTime.Now.TimeOfDay : null,
                                                //ParkoutTime =
                                                //    parkingEvent.EventType.Equals("PKOUT") ? DateTime.Now.TimeOfDay : null
                                            };
                                            if (parkingEvent.EventType.Equals("PKIN"))
                                                geViolation.ParkinTime = DateTime.Now.TimeOfDay;
                                            if (parkingEvent.EventType.Equals("PKOUT"))
                                                geViolation.ParkoutTime = DateTime.Now.TimeOfDay;
                                            context.GeViolations.Add(geViolation);
                                        }
                                        else if (regulation.Duration > 0 && parkingEvent.EventType.Equals("PKIN"))
                                        {
                                            storeForDurationCheck = true;
                                            parkingEvent.DurationCheck = true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!isVoilation && !storeForDurationCheck) continue;
                    Save(parkingEvent);
                    imageService.MediaOnDemand(parkingEvent.Properties.ImageAssetUid, parkingEvent.Timestamp);
                }

                Commentary.Print($"WebSocket State:{clientWebSocket.State}");
            }
        }

        private void Save(ParkingEvent parkingEvent)
        {
            if (parkingEvent == null)
                return;
            using (PredixContext context = new PredixContext())
            {
                Commentary.Print($"Saving Event Data", true);
                context.ParkingEvents.AddOrUpdate(x => new { x.LocationUid, x.EventType }, parkingEvent);
                context.SaveChanges();
            }
        }

        /// Determines if the given point is inside the polygon
        /// <param name="polygon">the vertices of polygon</param>
        /// <param name="testPoint">the given point</param>
        /// <returns>true if the point is inside the polygon; otherwise, false</returns>
        public static bool IsPointInPolygon4(PointF[] polygon, PointF testPoint)
        {
            bool result = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y || polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y)
                {
                    if (polygon[i].X + (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < testPoint.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }
    }
}