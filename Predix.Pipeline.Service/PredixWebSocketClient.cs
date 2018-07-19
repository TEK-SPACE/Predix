using System;
using System.Collections.Generic;
using System.Data.Entity;
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

        public void OpenAsync(string url, string bodyMessage,
            Dictionary<string, string> additionalHeaders, IImage imageService, Options options, int customerId)
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
                        clientWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true,
                            CancellationToken.None).Wait(cancellationTokenSource.Token);
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
                    Task.Run(async () =>
                            await ProcessEvent(imageService, options, customerId, response),
                        cancellationTokenSource.Token).ConfigureAwait(false);
                    //Task.Factory.StartNew(() => ProcessEvent(imageService, options, customerId, response), cancellationTokenSource.Token);
                }

                Commentary.Print($"WebSocket State:{clientWebSocket.State}");
            }
        }

        private Task<bool> ProcessEvent(IImage imageService, Options options, int customerId, string response)
        {
            try
            {
                var jsonRespone = JsonConvert.DeserializeObject<JObject>(response);
                ParkingEvent parkingEvent = jsonRespone != null
                    ? (jsonRespone).ToObject<ParkingEvent>()
                    : new ParkingEvent();
                Commentary.Print($"Location ID :{parkingEvent.LocationUid}");
                parkingEvent.CustomerId = customerId;
                if (parkingEvent.Properties == null)
                    parkingEvent.Properties = new ParkingEventProperties
                    {
                        CreatedDate = DateTime.UtcNow.ToEst(),
                        ModifiedDate = DateTime.UtcNow.ToEst()
                    };
                parkingEvent.Properties.LocationUid = parkingEvent.LocationUid;

                if (options.MarkAllAsViolations)
                {
                    #region Temp Push all as violoations

                    ForceMarkViolation(parkingEvent);
                    Save(parkingEvent);
                    imageService.MediaOnDemand(parkingEvent, parkingEvent.Properties.ImageAssetUid,
                        parkingEvent.Timestamp);

                    return Task.FromResult(true);

                    #endregion
                }

                if (options.IgnoreRegulationCheck)
                {
                    Commentary.Print($"Skipping Regulation Check Alg", true);
                    Save(parkingEvent);
                    imageService.MediaOnDemand(parkingEvent, parkingEvent.Properties.ImageAssetUid,
                        parkingEvent.Timestamp);

                    return Task.FromResult(true);
                }

                var isVoilation = false;
                using (var context = new PredixContext())
                {
                    var nodeMasterRegulations =
                        context.NodeMasterRegulations.Include(x => x.ParkingRegulation).ToList();

                    var nodeMasterRegulation =
                        nodeMasterRegulations.Where(x => x.LocationUid == parkingEvent.LocationUid)
                            .ToList();
                    if (nodeMasterRegulation.Any())
                    {
                        var latLongs = parkingEvent.Properties.GeoCoordinates.Split(',').ToList();
                        var parkingRegulations = new List<ParkingRegulation>();
                        foreach (var regulation in nodeMasterRegulation)
                        {
                            ViolationPercentage(latLongs, regulation, parkingEvent);
                            if (parkingEvent.MatchRate > 0)
                                parkingRegulations.Add(regulation.ParkingRegulation);
                            Commentary.Print($"Parking Event Id: {parkingEvent.Id} Match Rate {parkingEvent.MatchRate}",
                                true);
                        }

                        foreach (var regulation in parkingRegulations)
                        {
                            if (regulation.DayOfWeek.Split('|')
                                .Contains(DateTime.UtcNow.ToEst().DayOfWeek.ToString().Substring(0, 3)))
                            {
                                GeViolation geViolation = new GeViolation();
                               
                                switch (regulation.ViolationType)
                                {
                                    case ViolationType.NoParking:
                                        Commentary.Print($"Parking Event Id: {parkingEvent.Id} Checking No Parking",
                                            true);
                                        isVoilation = NoParkingCheck(regulation, parkingEvent, geViolation, context);
                                        break;
                                    case ViolationType.StreetSweeping:
                                        Commentary.Print($"Parking Event Id: {parkingEvent.Id} Checking Street Sweeping",
                                            true);
                                        isVoilation = IsStreetSweeping(regulation, parkingEvent, geViolation, context);
                                        break;
                                    case ViolationType.TimeLimitParking:
                                        Commentary.Print($"Parking Event Id: {parkingEvent.Id} Checking Time Limit",
                                            true);
                                        isVoilation = IsTimeLimitExceed(regulation, parkingEvent, geViolation, context);
                                        break;
                                    case ViolationType.ReservedParking:
                                        //This is out of scope for now, so we ae skipping this logic
                                        break;
                                    case ViolationType.FireHydrant:
                                        Commentary.Print($"Parking Event Id: {parkingEvent.Id} Fire Hydrant",
                                            true);
                                        isVoilation = IsFireHydrant(parkingEvent, geViolation, regulation, context);
                                        break;
                                }
                                Commentary.Print($"Parking Event Id: {parkingEvent.Id}, Is Violation: {isVoilation}",
                                    true);
                            }

                            if (isVoilation)
                                break;
                        }
                    }

                    context.SaveChanges();
                }

                if (!isVoilation && !options.SaveEvents) return Task.FromResult(true);
                Save(parkingEvent);
                if (isVoilation || options.SaveImages)
                    imageService.MediaOnDemand(parkingEvent, parkingEvent.Properties.ImageAssetUid,
                        parkingEvent.Timestamp);
                return Task.FromResult(false);
            }
            catch (Exception e)
            {
                Commentary.Print("******************************************");
                Commentary.Print(e.ToString());
                return Task.FromResult(false);
            }
        }

        private static bool IsFireHydrant(ParkingEvent parkingEvent, GeViolation geViolation,
            ParkingRegulation regulation, PredixContext context)
        {
            Commentary.Print($"*** Fire Hydrant Violation");
            
            if (parkingEvent.EventType.Equals("PKOUT"))
            {
                using (var innerContext = new PredixContext())
                {
                    var inEvent = innerContext.GeViolations.FirstOrDefault(x =>
                        x.ObjectUid == parkingEvent.Properties.ObjectUid &&
                        x.LocationUid == parkingEvent.LocationUid);
                    if (inEvent?.ParkinTime != null)
                    {
                        inEvent.ExceedParkingLimit = true;
                        inEvent.ParkoutTime = DateTime.UtcNow.ToEst();
                        inEvent.EventOutDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst();
                        inEvent.ViolationDuration = (long) DateTime.UtcNow.ToEst().Subtract(inEvent.ParkinTime.Value).TotalMinutes;
                        inEvent.ExceedParkingLimit = true;
                        inEvent.ModifiedDateTime = DateTime.UtcNow.ToEst();
                        innerContext.SaveChanges();
                        return false;
                    }
                }
            }

            geViolation.NoParking = true;
            geViolation.ObjectUid = parkingEvent.Properties.ObjectUid;
            geViolation.LocationUid = parkingEvent.LocationUid;
            if (parkingEvent.EventType.Equals("PKIN"))
            {
                geViolation.EventInDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst();
                geViolation.ParkinTime = DateTime.UtcNow.ToEst();
            }

            if (parkingEvent.EventType.Equals("PKOUT"))
            {
                geViolation.EventOutDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst();
                geViolation.ParkoutTime = DateTime.UtcNow.ToEst();
            }

            geViolation.RegulationId = regulation.RegualationId;
            geViolation.ViolationType = regulation.ViolationType;

            context.GeViolations.Add(geViolation);
            return true;
        }

        private static bool IsTimeLimitExceed(ParkingRegulation regulation, ParkingEvent parkingEvent,
            GeViolation geViolation, PredixContext context)
        {
            bool isVoilation = false;
            if (DateTime.UtcNow.ToEst().TimeOfDay >= regulation.StartTime &&
                DateTime.UtcNow.ToEst().TimeOfDay <= regulation.EndTime &&
                parkingEvent.EventType.Equals("PKIN"))
            {
                Commentary.Print($"*** Timelimit In Event");
                isVoilation = true;

                geViolation.ParkinTime = DateTime.UtcNow.ToEst();
                geViolation.EventInDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst();
                geViolation.ObjectUid = parkingEvent.Properties.ObjectUid;
                geViolation.LocationUid = parkingEvent.LocationUid;

                geViolation.RegulationId = regulation.RegualationId;
                geViolation.ViolationType = regulation.ViolationType;

                context.GeViolations.Add(geViolation);
            }
            else if (DateTime.UtcNow.ToEst().TimeOfDay >= regulation.StartTime &&
                     DateTime.UtcNow.ToEst().TimeOfDay <= regulation.EndTime &&
                     parkingEvent.EventType.Equals("PKOUT"))
            {
                isVoilation = true;
                Commentary.Print($"*** Exceeded Time Limit Violation");
                using (var innerContext = new PredixContext())
                {
                    var inEvent = innerContext.GeViolations.FirstOrDefault(x =>
                        x.ObjectUid == parkingEvent.Properties.ObjectUid &&
                        x.LocationUid == parkingEvent.LocationUid);
                    if (inEvent?.ParkinTime != null)
                    {
                        inEvent.ExceedParkingLimit = true;
                        inEvent.ParkoutTime = DateTime.UtcNow.ToEst();
                        inEvent.EventOutDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst();
                        inEvent.ViolationDuration = (long) DateTime.UtcNow.ToEst()
                            .Subtract(inEvent.ParkinTime.Value).TotalMinutes;
                        inEvent.ExceedParkingLimit = true;
                        inEvent.ModifiedDateTime = DateTime.UtcNow.ToEst();
                        innerContext.SaveChanges();
                    }
                }
            }

            return isVoilation;
        }

        private static bool IsStreetSweeping(ParkingRegulation regulation, ParkingEvent parkingEvent, 
            GeViolation geViolation, PredixContext context)
        {
            bool isVoilation = false;
            if (DateTime.UtcNow.ToEst().TimeOfDay >= regulation.StartTime &&
                DateTime.UtcNow.ToEst().TimeOfDay <= regulation.EndTime)
            {
                Commentary.Print($"*** StreetWeeping Violation");
                if (parkingEvent.EventType.Equals("PKOUT"))
                {
                    using (var innerContext = new PredixContext())
                    {
                        var inEvent = innerContext.GeViolations.FirstOrDefault(x =>
                            x.ObjectUid == parkingEvent.Properties.ObjectUid &&
                            x.LocationUid == parkingEvent.LocationUid);
                        if (inEvent?.ParkinTime != null)
                        {
                            inEvent.ExceedParkingLimit = true;
                            inEvent.ParkoutTime = DateTime.UtcNow.ToEst();
                            inEvent.EventOutDateTime =
                                parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst();
                            inEvent.ViolationDuration = (long) DateTime.UtcNow.ToEst()
                                .Subtract(inEvent.ParkinTime.Value).TotalMinutes;
                            inEvent.ExceedParkingLimit = true;
                            inEvent.ModifiedDateTime = DateTime.UtcNow.ToEst();
                            innerContext.SaveChanges();
                            return false;
                        }
                    }
                }

                isVoilation = true;
                geViolation.StreetSweeping = true;
                geViolation.ObjectUid = parkingEvent.Properties.ObjectUid;
                geViolation.LocationUid = parkingEvent.LocationUid;
                if (parkingEvent.EventType.Equals("PKIN"))
                {
                    geViolation.EventInDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst();
                    geViolation.ParkinTime = DateTime.UtcNow.ToEst();
                }

                if (parkingEvent.EventType.Equals("PKOUT"))
                {
                    geViolation.EventOutDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst();
                    geViolation.ParkoutTime = DateTime.UtcNow.ToEst();
                }

                geViolation.RegulationId = regulation.RegualationId;
                geViolation.ViolationType = regulation.ViolationType;

                context.GeViolations.Add(geViolation);
            }

            return isVoilation;
        }

        private static bool NoParkingCheck(ParkingRegulation regulation, ParkingEvent parkingEvent,
            GeViolation geViolation, PredixContext context)
        {
            bool isVoilation = false;
            if (DateTime.UtcNow.ToEst().TimeOfDay >= regulation.StartTime &&
                DateTime.UtcNow.ToEst().TimeOfDay <= regulation.EndTime)
            {
                Commentary.Print($"No Parkign Violation");
                if (parkingEvent.EventType.Equals("PKOUT"))
                {
                    using (var innerContext = new PredixContext())
                    {
                        var inEvent = innerContext.GeViolations.FirstOrDefault(x =>
                            x.ObjectUid == parkingEvent.Properties.ObjectUid &&
                            x.LocationUid == parkingEvent.LocationUid);
                        if (inEvent?.ParkinTime != null)
                        {
                            inEvent.ExceedParkingLimit = true;
                            inEvent.ParkoutTime = DateTime.UtcNow.ToEst();
                            inEvent.ViolationDuration = (long) DateTime.UtcNow.ToEst()
                                .Subtract(inEvent.ParkinTime.Value).TotalMinutes;
                            inEvent.ExceedParkingLimit = true;
                            inEvent.EventOutDateTime =
                                parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst();
                            inEvent.ModifiedDateTime = DateTime.UtcNow.ToEst();
                            innerContext.SaveChanges();
                            return false;
                        }
                    }
                }

                isVoilation = true;
                geViolation.NoParking = true;
                geViolation.ObjectUid = parkingEvent.Properties.ObjectUid;
                geViolation.LocationUid = parkingEvent.LocationUid;
                if (parkingEvent.EventType.Equals("PKIN"))
                {
                    geViolation.EventInDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst();
                    geViolation.ParkinTime = DateTime.UtcNow.ToEst();
                }

                if (parkingEvent.EventType.Equals("PKOUT"))
                {
                    geViolation.EventOutDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst();
                    geViolation.ParkoutTime = DateTime.UtcNow.ToEst();
                }

                geViolation.RegulationId = regulation.RegualationId;
                geViolation.ViolationType = regulation.ViolationType;
                context.GeViolations.Add(geViolation);
            }

            return isVoilation;
        }

        private static void ViolationPercentage(List<string> latLongs, NodeMasterRegulation regulation, ParkingEvent parkingEvent)
        {
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
                    parkingEvent.MatchRate += 25;
                }
            }
        }

        private static void ForceMarkViolation(ParkingEvent parkingEvent)
        {
            Commentary.Print("Force Mark Violation");
            if (parkingEvent.EventType.Equals("PKOUT"))
            {
                using (var innerContext = new PredixContext())
                {
                    var inEvent = innerContext.GeViolations.FirstOrDefault(x =>
                        x.ObjectUid == parkingEvent.Properties.ObjectUid &&
                        x.LocationUid == parkingEvent.LocationUid);
                    if (inEvent?.ParkinTime != null)
                    {
                        inEvent.ExceedParkingLimit = true;
                        inEvent.ParkoutTime = DateTime.UtcNow.ToEst();
                        inEvent.EventOutDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst();
                        inEvent.ViolationDuration = (long) DateTime.UtcNow.ToEst()
                            .Subtract(inEvent.ParkinTime.Value).TotalMinutes;
                        inEvent.ExceedParkingLimit = true;
                        inEvent.ModifiedDateTime = DateTime.UtcNow.ToEst();
                        innerContext.SaveChanges();
                        return;
                    }
                }
            }

            GeViolation geViolation = new GeViolation
            {
                NoParking = true,
                ObjectUid = parkingEvent.Properties.ObjectUid,
                LocationUid = parkingEvent.LocationUid
            };
            if (parkingEvent.EventType.Equals("PKIN"))
            {
                geViolation.EventInDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst();
                geViolation.ParkinTime = DateTime.UtcNow.ToEst();
            }

            if (parkingEvent.EventType.Equals("PKOUT"))
            {
                geViolation.EventOutDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst();
                geViolation.ParkoutTime = DateTime.UtcNow.ToEst();
            }

            geViolation.RegulationId = 81;
            geViolation.ViolationType = ViolationType.FireHydrant;
            using (var context = new PredixContext())
            {
                context.GeViolations.Add(geViolation);
                context.SaveChanges();
            }
        }

        //private static DateTime? EpochToDateTime(string epoch)
        //{
        //    if (string.IsNullOrWhiteSpace(epoch))
        //        return null;
        //    return DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(epoch)).DateTime;
        //    //System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        //    //dtDateTime = dtDateTime.AddSeconds(Convert.ToInt64(epoch)).ToLocalTime();
        //    //return dtDateTime;
        //}

        private void Save(ParkingEvent parkingEvent)
        {
            if (parkingEvent == null)
                return;
            using (PredixContext context = new PredixContext())
            {
                Commentary.Print($"Saving Event Data", true);
                parkingEvent.CreatedDate = DateTime.UtcNow.ToEst();
                context.ParkingEvents.Add(parkingEvent);
                //context.ParkingEvents.AddOrUpdate(x => new { x.LocationUid, x.EventType }, parkingEvent);
                context.SaveChanges();
            }
        }

        /// Determines if the given point is inside the polygon
        /// <param name="polygon">the vertices of polygon</param>
        /// <param name="testPoint">the given point</param>
        /// <returns>true if the point is inside the polygon; otherwise, false</returns>
        private static bool IsPointInPolygon4(PointF[] polygon, PointF testPoint)
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