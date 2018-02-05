using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Device.Location;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Predix.Domain.Model;
using Predix.Domain.Model.Constant;
using Predix.Domain.Model.Enum;
using Predix.Domain.Model.Location;
using Predix.Pipeline.DataService;
using Predix.Pipeline.Helper;
using Predix.Pipeline.Interface;

namespace Predix.Pipeline.Service
{
    public class PredixWebSocketClient : IPredixWebSocketClient
    {
        private readonly ISecurity _securityService = new SecurityService();

        public async Task OpenAsync(string url, string bodyMessage,
            Dictionary<string, string> additionalHeaders, IImage imageService, Options options)
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
                    if (options.IgnoreRegulationCheck)
                    {
                        Commentary.Print($"Skipping Regulation Check Alg", true);
                        Save(parkingEvent);
                        imageService.MediaOnDemand(parkingEvent.Properties.ImageAssetUid, parkingEvent.Timestamp);
                        continue;
                    }

                    parkingEvent.Properties.LocationUid = parkingEvent.LocationUid;
                    //parkingEvent.Properties.EventUid = parkingEvent.Id
                    //parkingEvent.Properties.ParkingEventId = parkingEvent.Id;

                    var isVoilation = false;
                    var storeForDurationCheck = false;
                    using (var context = new PredixContext())
                    {
                        var nodeMasterRegulations =
                            context.NodeMasterRegulations.Include(x => x.ParkingRegulation).ToList();

                        //check if GEO Coordinates match

                        var nodeMasterRegulation =
                            nodeMasterRegulations.Where(x => x.LocationUid == parkingEvent.LocationUid)
                                .ToList();
                        if (nodeMasterRegulation.Any())
                        {
                            var latLongs = parkingEvent.Properties.GeoCoordinates.Split(',').ToList();
                            //var geoCoordinates = new List<GeoCoordinate>();
                            //foreach (var latLong in latLongs)
                            //{
                            //    geoCoordinates.Add(new GeoCoordinate(Convert.ToDouble(latLong.Split(':')[0]),
                            //        Convert.ToDouble(latLong.Split(':')[1])));
                            //}

                            //var centralCoordinates = GetCentralGeoCoordinate(geoCoordinates);

                            //var parkingRegulations = nodeMasterRegulation.Where(x =>
                            //        IsPointInPolygon4(new List<PointF>
                            //            {
                            //                new PointF(float.Parse(x.ParkingRegulation.Coodrinate1.Split(':')[0]),
                            //                    float.Parse(x.ParkingRegulation.Coodrinate1.Split(':')[1]))
                            //            }.ToArray(),
                            //            new PointF(
                            //                float.Parse(
                            //                    centralCoordinates.Latitude.ToString(CultureInfo.InvariantCulture)),
                            //                float.Parse(
                            //                    centralCoordinates.Latitude.ToString(CultureInfo.InvariantCulture)))))
                            //    .Select(x => x.ParkingRegulation).ToList();
                            var parkingRegulations = new List<ParkingRegulation>();
                            foreach (var regulation in nodeMasterRegulation)
                            {
                                //var include = false;

                                foreach (var latLong in latLongs)
                                {
                                    if (IsPointInPolygon4(new List<PointF>
                                        {
                                            new PointF(
                                                float.Parse(regulation.ParkingRegulation.Coodrinate1.Split(':')[0]),
                                                float.Parse(regulation.ParkingRegulation.Coodrinate1.Split(':')[1])),
                                            new PointF(
                                                float.Parse(regulation.ParkingRegulation.Coodrinate2.Split(':')[0]),
                                                float.Parse(regulation.ParkingRegulation.Coodrinate2.Split(':')[1])),
                                            new PointF(
                                                float.Parse(regulation.ParkingRegulation.Coodrinate3.Split(':')[0]),
                                                float.Parse(regulation.ParkingRegulation.Coodrinate3.Split(':')[1])),
                                            new PointF(
                                                float.Parse(regulation.ParkingRegulation.Coodrinate4.Split(':')[0]),
                                                float.Parse(regulation.ParkingRegulation.Coodrinate4.Split(':')[1]))
                                        }.ToArray(),
                                        new PointF(float.Parse(latLong.Split(':')[0]),
                                            float.Parse(latLong.Split(':')[1]))))
                                    {
                                        //include = true;
                                        parkingEvent.MatchRate += 25;
                                    }

                                    //if (include)
                                    //    break;
                                }

                                if (parkingEvent.MatchRate > 0)
                                    parkingRegulations.Add(regulation.ParkingRegulation);
                            }


                            foreach (var regulation in parkingRegulations)
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
                                                //NodeId = nodeMasterRegulation.First().NodeMasterId,
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

                    if (!isVoilation && !storeForDurationCheck && !options.SaveEvents) continue;
                    Save(parkingEvent);
                    if(isVoilation || options.SaveImages)
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
                context.ParkingEvents.Add(parkingEvent);
                //context.ParkingEvents.AddOrUpdate(x => new { x.LocationUid, x.EventType }, parkingEvent);
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

        public static GeoCoordinate GetCentralGeoCoordinate(
            IList<GeoCoordinate> geoCoordinates)
        {
            if (geoCoordinates.Count == 1)
            {
                return geoCoordinates.Single();
            }

            double x = 0;
            double y = 0;
            double z = 0;

            foreach (var geoCoordinate in geoCoordinates)
            {
                var latitude = geoCoordinate.Latitude * Math.PI / 180;
                var longitude = geoCoordinate.Longitude * Math.PI / 180;

                x += Math.Cos(latitude) * Math.Cos(longitude);
                y += Math.Cos(latitude) * Math.Sin(longitude);
                z += Math.Sin(latitude);
            }

            var total = geoCoordinates.Count;

            x = x / total;
            y = y / total;
            z = z / total;

            var centralLongitude = Math.Atan2(y, x);
            var centralSquareRoot = Math.Sqrt(x * x + y * y);
            var centralLatitude = Math.Atan2(z, centralSquareRoot);

            return new GeoCoordinate(centralLatitude * 180 / Math.PI, centralLongitude * 180 / Math.PI);
        }
    }
}