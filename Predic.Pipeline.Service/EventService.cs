using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
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
    public class EventService : IEvent
    {
        private readonly IPredixHttpClient _predixHttpClient;
        private static Dictionary<string, object> _globalVariables;
        public EventService(Dictionary<string, object> globalVariables)
        {
            _predixHttpClient = new PredixHttpClient(globalVariables);
            _globalVariables = globalVariables;
        }
        private readonly IPredixWebSocketClient _predixWebSocketClient = new PredixWebSocketClient();
        public List<ParkingEvent> Get(string locationUid, string eventType, DateTime startDate, DateTime endTime)
        {
            List<ParkingEvent> details = new List<ParkingEvent>();
            Dictionary<string, string> additionalHeaders =
                new Dictionary<string, string> { { "predix-zone-id", "SDSIM-IE-PARKING" } };
            var response = _predixHttpClient.GetAllAsync(Endpoint.PkInPkOutByLocationId
                .Replace("{parking_loc}", locationUid)
                .Replace("{parkInOrOut}", eventType)
                .Replace("{startTimeInEpoch}", startDate.ToEpoch().ToString())
                .Replace("{endTimeInEpoch}", endTime.ToEpoch().ToString()), additionalHeaders);
            if (!string.IsNullOrWhiteSpace(response.Result))
            {
                var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
                details.AddRange(jsonRespone["content"] != null
                    ? ((JArray)jsonRespone["content"]).ToObject<List<ParkingEvent>>()
                    : new List<ParkingEvent>());
            }
            return details;
        }
        public void GetByBoundary(string bbox, string eventType1, string eventType2, IImage imageService)
        {
            //ParkingEvent parkingEvent = null;
            Dictionary<string, string> additionalHeaders =
                new Dictionary<string, string> { { "predix-zone-id",  Endpoint.PredixZoneIdForParking } };
            string bodyMessage = $"{{\"bbox\":\"{bbox}\",\"eventTypes\":[\"{eventType1}\",\"{eventType2}\" ]}}";
            _predixWebSocketClient.OpenAsync(Endpoint.WebSocketUrlForEvents, bodyMessage, additionalHeaders, imageService);
            //if (!string.IsNullOrWhiteSpace(response.Result))
            //{
            //    var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
            //    parkingEvent = jsonRespone != null
            //        ? (jsonRespone).ToObject<ParkingEvent>()
            //        : new ParkingEvent();
            //}
            //await SaveAsync(parkingEvent);
            //return parkingEvent;
        }
        public void GetByLocation(string locationUid, string eventType, IImage imageService)
        {
            ParkingEvent parkingEvent = null;
            Dictionary<string, string> additionalHeaders =
                new Dictionary<string, string> { { "predix-zone-id", Endpoint.PredixZoneIdForParking } };
            string bodyMessage = $"{{\"locationUid\":\"{locationUid}\",\"eventTypes\":[\"{eventType}\"]}}";
             _predixWebSocketClient.OpenAsync(Endpoint.WebSocketUrlForEvents, bodyMessage, additionalHeaders, imageService);
            //if (!string.IsNullOrWhiteSpace(response.Result))
            //{
            //    var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
            //    parkingEvent = jsonRespone != null
            //        //? ((JArray)jsonRespone["content"]).ToObject<ParkingEvent>()
            //        ? (jsonRespone).ToObject<ParkingEvent>()
            //        : new ParkingEvent();
            //}
            //await SaveAsync(parkingEvent);
            //return parkingEvent;
        }

        //private async Task SaveAsync(ParkingEvent parkingEvent)
        //{
        //    if (parkingEvent == null)
        //        return;
        //    using (PredixContext context = new PredixContext())
        //    {
        //        context.ParkingEvents.AddOrUpdate(x => new { x.LocationUid, x.EventType }, parkingEvent);
        //        await context.SaveChangesAsync();
        //    }
        //}
    }
}
