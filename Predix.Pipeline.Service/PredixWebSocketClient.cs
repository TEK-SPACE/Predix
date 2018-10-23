using System;
using System.Collections.Generic;
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
            Dictionary<string, string> additionalHeaders, IImage imageService, Options options, Customer customer)
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
                    Commentary.Print("Opened Socket Connection");
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
                            Commentary.Print($"Closed; Status: {result.CloseStatus}, {result.CloseStatusDescription}");
                        }
                        else
                        {
                            response = Encoding.UTF8.GetString(incomingData, 0, result.Count);
                            //Console.WriteLine("Received message: " + response);
                            Commentary.Print("Received Message");
                        }
                    }
                    catch (Exception exception)
                    {
                        Commentary.Print(exception.ToString());
                        //Commentary.Print($"WebSocket State:{clientWebSocket.State}");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(response)) continue;
                    Task.Run(() =>
                            ProcessEvent(imageService, options, customer, response),
                        cancellationTokenSource.Token).ConfigureAwait(false);
                    //Task.Factory.StartNew(() => ProcessEvent(imageService, options, customerId, response), cancellationTokenSource.Token);
                }

                Commentary.Print($"WebSocket State:{clientWebSocket.State}");
            }
        }
        public bool ProcessEvent(IImage imageService, Options options, Customer customer, ParkingEvent parkingEvent)
        {
            try
            {
                Commentary.Print($"Location ID :{parkingEvent.LocationUid}");
                parkingEvent.CustomerId = customer.Id;
                if (parkingEvent.Properties == null)
                    parkingEvent.Properties = new ParkingEventProperties
                    {
                        CreatedDate = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId),
                        ModifiedDate = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId)
                    };
                parkingEvent.Properties.LocationUid = parkingEvent.LocationUid;


                var isVoilation = false;
                using (var context = new PredixContext())
                {
                    var anyRecord = context.ParkingEvents.Any(x => x.LocationUid == parkingEvent.LocationUid
                    && x.EventType == parkingEvent.EventType
                    && x.Properties.ObjectUid == parkingEvent.Properties.ObjectUid
                    && x.Properties.ImageAssetUid == parkingEvent.Properties.ImageAssetUid);
                    if (anyRecord)
                        return false;

                    var nodeMasterRegulations =
                        context.NodeMasterRegulations.Join(context.ParkingRegulations,
                                node => node.RegulationId,
                                regulation => regulation.RegualationId,
                                (node, regulation) => new {Node = node, Regulation = regulation})
                            .Where(x => x.Regulation.IsActive && x.Node.LocationUid == parkingEvent.LocationUid)
                            .Select(x=>x.Regulation).ToList();

                    //var nodeMasterRegulation =
                    //    nodeMasterRegulations.Where(x => x.LocationUid == parkingEvent.LocationUid)
                    //        .ToList();
                    if (nodeMasterRegulations.Any())
                    {
                        var latLongs = parkingEvent.Properties.GeoCoordinates.Split(',').ToList();
                        var parkingRegulations = new List<ParkingRegulation>();
                        foreach (var regulation in nodeMasterRegulations)
                        {
                            ViolationPercentage(latLongs, regulation, parkingEvent);
                            if (parkingEvent.MatchRate > 0)
                                parkingRegulations.Add(regulation);
                            Commentary.Print(
                                $"Regulation Id: {regulation.RegualationId}, Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, Match Rate {parkingEvent.MatchRate}",
                                true);
                        }

                        foreach (var regulation in parkingRegulations)
                        {
                            Commentary.Print(
                                $"Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid},parkingEvent.Timestamp.ToUtcDateTimeOrNull().DayOfWeek:{parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId).DayOfWeek}, ViolationType: {regulation.ViolationType}",
                                true);
                            if (regulation.DayOfWeek.Split('|').Select(x=>x.ToUpper())
                                .Contains(parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId).DayOfWeek.ToString()
                                    .Substring(0, 3).ToUpper()))
                            {
                                GeViolation geViolation = new GeViolation();

                                switch (regulation.ViolationType)
                                {
                                    case ViolationType.NoParking:
                                        Commentary.Print(
                                            $"Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, Checking No Parking",
                                            true);
                                        isVoilation = NoParkingCheck(regulation, parkingEvent, geViolation, context,
                                            customer);
                                        break;
                                    case ViolationType.StreetSweeping:
                                        Commentary.Print(
                                            $"Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, Checking Street Sweeping",
                                            true);
                                        isVoilation = IsStreetSweeping(regulation, parkingEvent, geViolation, context,
                                            customer);
                                        break;
                                    case ViolationType.TimeLimitParking:
                                        Commentary.Print(
                                            $"Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, Checking Time Limit",
                                            true);
                                        isVoilation = IsTimeLimitExceed(regulation, parkingEvent, geViolation, context,
                                            customer);
                                        break;
                                    case ViolationType.ReservedParking:
                                        //This is out of scope for now, so we ae skipping this logic
                                        break;
                                    case ViolationType.FireHydrant:
                                        Commentary.Print(
                                            $"Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, Fire Hydrant",
                                            true);
                                        isVoilation = IsFireHydrant(parkingEvent, geViolation, regulation, context,
                                            customer);
                                        break;
                                }

                                Commentary.Print(
                                    $"Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, Is Violation: {isVoilation}",
                                    true);
                            }

                            if (isVoilation)
                                break;
                        }
                    }

                    context.SaveChanges();
                }

                if (!isVoilation) return true;
                Save(parkingEvent, customer);
                imageService.MediaOnDemand(parkingEvent, parkingEvent.Properties.ImageAssetUid,
                    parkingEvent.Timestamp, customer);
                return false;
            }
            catch (Exception e)
            {
                Commentary.Print("******************************************");
                Commentary.Print(e.ToString());
                return false;
            }
        }
        private bool ProcessEvent(IImage imageService, Options options, Customer customer, string response)
        {
            try
            {
                var jsonRespone = JsonConvert.DeserializeObject<JObject>(response);
                ParkingEvent parkingEvent = jsonRespone != null
                    ? (jsonRespone).ToObject<ParkingEvent>()
                    : new ParkingEvent();
                parkingEvent.CustomerId = customer.Id;
                if (parkingEvent.Properties == null)
                    parkingEvent.Properties = new ParkingEventProperties
                    {
                        CreatedDate = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId),
                        ModifiedDate = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId)
                    };
                parkingEvent.Properties.LocationUid = parkingEvent.LocationUid;

                Commentary.Print($"Location Id :{parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, Object Id: {parkingEvent.Properties?.ObjectUid}");

                if (options.MarkAllAsViolations)
                {
                    #region Temp Push all as violoations

                    ForceMarkViolation(parkingEvent, customer);
                    Save(parkingEvent, customer);
                    imageService.MediaOnDemand(parkingEvent, parkingEvent.Properties.ImageAssetUid,
                        parkingEvent.Timestamp, customer);

                    return true;

                    #endregion
                }

                if (options.IgnoreRegulationCheck)
                {
                    Commentary.Print("Skipping Regulation Check Alg", true);
                    Save(parkingEvent, customer);
                    imageService.MediaOnDemand(parkingEvent, parkingEvent.Properties.ImageAssetUid,
                        parkingEvent.Timestamp, customer);

                    return true;
                }

                var isVoilation = false;
                using (var context = new PredixContext())
                {
                    //var nodeMasterRegulations =
                    //    context.NodeMasterRegulations.Include(x => x.ParkingRegulation)
                    //        .Where(x => x.ParkingRegulation.IsActive).ToList();

                    //var nodeMasterRegulation =
                    //    nodeMasterRegulations.Where(x => x.LocationUid == parkingEvent.LocationUid)
                    //        .ToList();
                    var nodeMasterRegulations =
                        context.NodeMasterRegulations.Join(context.ParkingRegulations,
                                node => node.RegulationId,
                                regulation => regulation.RegualationId,
                                (node, regulation) => new { Node = node, Regulation = regulation })
                            .Where(x => x.Regulation.IsActive && x.Node.LocationUid == parkingEvent.LocationUid)
                            .Select(x => x.Regulation).ToList();
                    if (nodeMasterRegulations.Any())
                    {
                        var latLongs = parkingEvent.Properties.GeoCoordinates.Split(',').ToList();
                        var parkingRegulations = new List<ParkingRegulation>();
                        foreach (var regulation in nodeMasterRegulations)
                        {
                            ViolationPercentage(latLongs, regulation, parkingEvent);
                            if (parkingEvent.MatchRate > 0)
                            {
                                parkingRegulations.Add(regulation);
                                Commentary.Print(
                                    $"Regulation Id: {regulation.RegualationId}, Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, Match Rate {parkingEvent.MatchRate}, ObjectUid {parkingEvent.Properties?.ObjectUid}",
                                    true);
                            }
                        }

                        foreach (var regulation in parkingRegulations)
                        {
                            Commentary.Print(
                                $"Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst().DayOfWeek:{parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId).DayOfWeek}, ViolationType: {regulation.ViolationType}",
                                true);
                            if (regulation.DayOfWeek.Split('|').Select(x=>x.ToUpper())
                                .Contains(parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId).DayOfWeek.ToString()
                                    .Substring(0, 3).ToUpper()))
                            {
                                GeViolation geViolation = new GeViolation();

                                switch (regulation.ViolationType)
                                {
                                    case ViolationType.NoParking:
                                        Commentary.Print(
                                            $"Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, Object Id: {parkingEvent.Properties.ObjectUid}, Checking No Parking",
                                            true);
                                        isVoilation = NoParkingCheck(regulation, parkingEvent, geViolation, context,
                                            customer);
                                        break;
                                    case ViolationType.StreetSweeping:
                                        Commentary.Print(
                                            $"Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, Object Id: {parkingEvent.Properties.ObjectUid}, Checking Street Sweeping",
                                            true);
                                        isVoilation = IsStreetSweeping(regulation, parkingEvent, geViolation, context,
                                            customer);
                                        break;
                                    case ViolationType.TimeLimitParking:
                                        Commentary.Print(
                                            $"Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, Object Id: {parkingEvent.Properties.ObjectUid}, Checking Time Limit",
                                            true);
                                        isVoilation = IsTimeLimitExceed(regulation, parkingEvent, geViolation, context,
                                            customer);
                                        break;
                                    case ViolationType.ReservedParking:
                                        //This is out of scope for now, so we ae skipping this logic
                                        break;
                                    case ViolationType.FireHydrant:
                                        Commentary.Print(
                                            $"Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, Object Id: {parkingEvent.Properties.ObjectUid}, Fire Hydrant",
                                            true);
                                        isVoilation = IsFireHydrant(parkingEvent, geViolation, regulation, context,
                                            customer);
                                        break;
                                }

                                Commentary.Print(
                                    $"Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, Object Id: {parkingEvent.Properties.ObjectUid}, Is Violation: {isVoilation}",
                                    true);
                            }

                            if (isVoilation)
                                break;
                        }
                    }

                    context.SaveChanges();
                }

                if (!isVoilation && !options.SaveEvents) return true;
                Save(parkingEvent, customer);
                if (isVoilation || options.SaveImages)
                    imageService.MediaOnDemand(parkingEvent, parkingEvent.Properties.ImageAssetUid,
                        parkingEvent.Timestamp, customer);
                return false;
            }
            catch (Exception e)
            {
                Commentary.Print("******************************************");
                Commentary.Print(e.ToString());
                return false;
            }
        }

        private static bool IsFireHydrant(ParkingEvent parkingEvent, GeViolation geViolation,
            ParkingRegulation regulation, PredixContext context, Customer customer)
        {
            Commentary.Print("*** Fire Hydrant Violation");

            if (parkingEvent.EventType.Equals("PKOUT"))
            {
                using (var innerContext = new PredixContext())
                {
                    var inEvent = innerContext.GeViolations.FirstOrDefault(x =>
                        x.ObjectUid == parkingEvent.Properties.ObjectUid &&
                        x.LocationUid == parkingEvent.LocationUid);
                    if (inEvent?.ParkinTime != null)
                    {
                        //inEvent.ExceedParkingLimit = true;
                        inEvent.ParkoutTime =
                            parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.EventOutDateTime =
                            parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.ViolationDuration = (long) parkingEvent.Timestamp.ToUtcDateTimeOrNull()
                            .ToTimeZone(customer.TimezoneId)
                            .Subtract(inEvent.ParkinTime.Value).TotalMinutes;
                        //inEvent.ExceedParkingLimit = true;
                        inEvent.ModifiedDateTime =
                            parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.IsException = true;
                        inEvent.UserAction = "Missed_Violation";
                        innerContext.SaveChanges();
                        return true;
                    }
                }
                return false;
            }

            geViolation.NoParking = true;
            geViolation.ObjectUid = parkingEvent.Properties.ObjectUid;
            geViolation.LocationUid = parkingEvent.LocationUid;
            if (parkingEvent.EventType.Equals("PKIN"))
            {
                geViolation.EventInDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                geViolation.ParkinTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
            }

            if (parkingEvent.EventType.Equals("PKOUT"))
            {
                geViolation.EventOutDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                geViolation.ParkoutTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
            }

            geViolation.RegulationId = regulation.RegualationId;
            geViolation.ViolationType = regulation.ViolationType;

            context.GeViolations.Add(geViolation);
            return true;
        }

        private static bool IsTimeLimitExceed(ParkingRegulation regulation, ParkingEvent parkingEvent,
            GeViolation geViolation, PredixContext context, Customer customer)
        {
            bool isVoilation = false;
            if (parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId).TimeOfDay >= regulation.StartTime &&
                parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId).TimeOfDay <= regulation.EndTime &&
               parkingEvent.EventType.Equals("PKIN"))
            {
                Commentary.Print("*** Timelimit In Event");
                isVoilation = true;

                geViolation.ParkinTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                geViolation.EventInDateTime =
                    parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                geViolation.ObjectUid = parkingEvent.Properties.ObjectUid;
                geViolation.LocationUid = parkingEvent.LocationUid;

                geViolation.RegulationId = regulation.RegualationId;
                geViolation.ViolationType = regulation.ViolationType;

                context.GeViolations.Add(geViolation);
            }
            else if (parkingEvent.EventType.Equals("PKOUT")
                     && regulation.Duration >= parkingEvent.Timestamp.ToUtcDateTimeOrNull()
                         .ToTimeZone(customer.TimezoneId)
                         .Subtract(parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId))
                         .TotalMinutes)
            {
                Commentary.Print("*** Exceeded Time Limit Violation");
                using (var innerContext = new PredixContext())
                {
                    var inEvent = innerContext.GeViolations.FirstOrDefault(x =>
                        x.ObjectUid == parkingEvent.Properties.ObjectUid &&
                        x.LocationUid == parkingEvent.LocationUid);
                    if (inEvent?.ParkinTime != null)
                    {
                        inEvent.ParkoutTime =
                            parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.EventOutDateTime =
                            parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.ViolationDuration = (long) parkingEvent.Timestamp.ToUtcDateTimeOrNull()
                            .ToTimeZone(customer.TimezoneId)
                            .Subtract(inEvent.ParkinTime.Value).TotalMinutes;
                        inEvent.ExceedParkingLimit = true;
                        inEvent.ModifiedDateTime =
                            parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.IsException = true;
                        inEvent.UserAction = "Missed_Violation";
                        innerContext.SaveChanges();
                        return true;
                    }
                }

                return false;
            }

            return isVoilation;
        }

        private static bool IsStreetSweeping(ParkingRegulation regulation, ParkingEvent parkingEvent,
            GeViolation geViolation, PredixContext context, Customer customer)
        {
            bool isVoilation = false;
            if (parkingEvent.EventType.Equals("PKOUT"))
            {
                using (var innerContext = new PredixContext())
                {
                    var inEvent = innerContext.GeViolations.FirstOrDefault(x =>
                        x.ObjectUid == parkingEvent.Properties.ObjectUid &&
                        x.LocationUid == parkingEvent.LocationUid);
                    if (inEvent?.ParkinTime != null)
                    {
                        inEvent.ParkoutTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.EventOutDateTime =
                            parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.ViolationDuration = (long)parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId)
                            .Subtract(inEvent.ParkinTime.Value).TotalMinutes;
                        inEvent.StreetSweeping = true;
                        inEvent.ModifiedDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.IsException = true;
                        inEvent.UserAction = "Missed_Violation";
                        innerContext.SaveChanges();
                        return true;
                    }
                }
                return false;
            }
            if (parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId).TimeOfDay >= regulation.StartTime &&
                parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId).TimeOfDay <= regulation.EndTime)
            {
                Commentary.Print("*** StreetWeeping Violation");

                isVoilation = true;
                geViolation.StreetSweeping = true;
                geViolation.ObjectUid = parkingEvent.Properties.ObjectUid;
                geViolation.LocationUid = parkingEvent.LocationUid;
                if (parkingEvent.EventType.Equals("PKIN"))
                {
                    geViolation.EventInDateTime =
                        parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                    geViolation.ParkinTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                }

                if (parkingEvent.EventType.Equals("PKOUT"))
                {
                    geViolation.EventOutDateTime =
                        parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                    geViolation.ParkoutTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                }

                geViolation.RegulationId = regulation.RegualationId;
                geViolation.ViolationType = regulation.ViolationType;

                context.GeViolations.Add(geViolation);
            }

            return isVoilation;
        }

        private static bool NoParkingCheck(ParkingRegulation regulation, ParkingEvent parkingEvent,
            GeViolation geViolation, PredixContext context, Customer customer)
        {
            bool isVoilation = false;
            Commentary.Print(
                $"Location Uid: {parkingEvent.LocationUid}, Asset Uid: {parkingEvent.AssetUid}, parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToEst().TimeOfDay: {parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId).TimeOfDay:g}, regulation.StartTime : {regulation.StartTime}, regulation.EndTime:{regulation.EndTime}",
                true);
            if (parkingEvent.EventType.Equals("PKOUT"))
            {
                using (var innerContext = new PredixContext())
                {
                    var inEvent = innerContext.GeViolations.FirstOrDefault(x =>
                        x.ObjectUid == parkingEvent.Properties.ObjectUid &&
                        x.LocationUid == parkingEvent.LocationUid);
                    if (inEvent?.ParkinTime != null)
                    {
                        inEvent.ParkoutTime =
                            parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.ViolationDuration = (long) parkingEvent.Timestamp.ToUtcDateTimeOrNull()
                            .ToTimeZone(customer.TimezoneId)
                            .Subtract(inEvent.ParkinTime.Value).TotalMinutes;
                        inEvent.NoParking = true;
                        inEvent.EventOutDateTime =
                            parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.ModifiedDateTime =
                            parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.IsException = true;
                        inEvent.UserAction = "Missed_Violation";
                        innerContext.SaveChanges();
                        return true;
                    }
                }

                return false;
            }

            if (parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId).TimeOfDay >= regulation.StartTime &&
                parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId).TimeOfDay <= regulation.EndTime)
            {
                Commentary.Print("No Parkign Violation");
                isVoilation = true;
                geViolation.NoParking = true;
                geViolation.ObjectUid = parkingEvent.Properties.ObjectUid;
                geViolation.LocationUid = parkingEvent.LocationUid;
                if (parkingEvent.EventType.Equals("PKIN"))
                {
                    geViolation.EventInDateTime =
                        parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                    geViolation.ParkinTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                }

                if (parkingEvent.EventType.Equals("PKOUT"))
                {
                    geViolation.EventOutDateTime =
                        parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                    geViolation.ParkoutTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                }

                geViolation.RegulationId = regulation.RegualationId;
                geViolation.ViolationType = regulation.ViolationType;
                context.GeViolations.Add(geViolation);
            }

            return isVoilation;
        }

        private static void ViolationPercentage(List<string> latLongs, ParkingRegulation regulation,
            ParkingEvent parkingEvent)
        {
            foreach (var latLong in latLongs)
            {
                if (IsPointInPolygon4(new List<PointF>
                    {
                        new PointF(
                            float.Parse(regulation.Coodrinate1.Split(':')[0]),
                            float.Parse(regulation.Coodrinate1.Split(':')[1])),
                        new PointF(
                            float.Parse(regulation.Coodrinate2.Split(':')[0]),
                            float.Parse(regulation.Coodrinate2.Split(':')[1])),
                        new PointF(
                            float.Parse(regulation.Coodrinate3.Split(':')[0]),
                            float.Parse(regulation.Coodrinate3.Split(':')[1])),
                        new PointF(
                            float.Parse(regulation.Coodrinate4.Split(':')[0]),
                            float.Parse(regulation.Coodrinate4.Split(':')[1]))
                    }.ToArray(),
                    new PointF(float.Parse(latLong.Split(':')[0]),
                        float.Parse(latLong.Split(':')[1]))))
                {
                    parkingEvent.MatchRate += 25;
                }
            }
        }

        private static void ForceMarkViolation(ParkingEvent parkingEvent, Customer customer)
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
                        inEvent.ParkoutTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.EventOutDateTime =
                            parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.ViolationDuration = (long)parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId)
                            .Subtract(inEvent.ParkinTime.Value).TotalMinutes;
                        //inEvent.NoParking = true;
                        inEvent.ModifiedDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                        inEvent.IsException = true;
                        inEvent.UserAction = "Missed_Violation";
                        innerContext.SaveChanges();
                    }
                }

                return;
            }

            GeViolation geViolation = new GeViolation
            {
                NoParking = true,
                ObjectUid = parkingEvent.Properties.ObjectUid,
                LocationUid = parkingEvent.LocationUid
            };
            if (parkingEvent.EventType.Equals("PKIN"))
            {
                geViolation.EventInDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                geViolation.ParkinTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
            }

            if (parkingEvent.EventType.Equals("PKOUT"))
            {
                geViolation.EventOutDateTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
                geViolation.ParkoutTime = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
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

        private void Save(ParkingEvent parkingEvent, Customer customer)
        {
            if (parkingEvent == null)
                return;
            using (PredixContext context = new PredixContext())
            {
                Commentary.Print("Saving Event Data", true);
                parkingEvent.CreatedDate = parkingEvent.Timestamp.ToUtcDateTimeOrNull().ToTimeZone(customer.TimezoneId);
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
                if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y ||
                    polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y)
                {
                    if (polygon[i].X + (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) *
                        (polygon[j].X - polygon[i].X) < testPoint.X)
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