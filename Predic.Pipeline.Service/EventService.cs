using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
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
        public ParkingEvent GetByBoundary(string bbox, string eventType1, string eventType2)
        {
            ParkingEvent parkingEvent = null;
            Dictionary<string, string> additionalHeaders =
                new Dictionary<string, string> { { "predix-zone-id", "GPATL-IE-PARKING" } };
            string bodyMessage = $"{{\"bbox\":\"{bbox}\",\"eventTypes\":[\"{eventType1}\",\"{eventType2}\" ]}}";
            var response = _predixWebSocketClient.GetAllAsync(Endpoint.WebSocketUrlForEvents, bodyMessage, additionalHeaders);
            if (!string.IsNullOrWhiteSpace(response.Result))
            {
                var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
                parkingEvent = jsonRespone != null
                    //? ((JArray)jsonRespone["content"]).ToObject<ParkingEvent>()
                    ? (jsonRespone).ToObject<ParkingEvent>()
                    : new ParkingEvent();
            }
            //Save(parkingEvent);
            return parkingEvent;
        }
        public ParkingEvent Get(string locationUid, string eventType)
        {
            ParkingEvent parkingEvent = null;
            Dictionary<string, string> additionalHeaders =
                new Dictionary<string, string> { { "predix-zone-id", "GPATL-IE-PARKING" } };
            string bodyMessage = $"{{\"locationUid\":\"{locationUid}\",\"eventTypes\":[\"{eventType}\"]}}";
            var response = _predixWebSocketClient.GetAllAsync(Endpoint.WebSocketUrlForEvents, bodyMessage, additionalHeaders);
            if (!string.IsNullOrWhiteSpace(response.Result))
            {
                var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
                parkingEvent = jsonRespone != null
                    //? ((JArray)jsonRespone["content"]).ToObject<ParkingEvent>()
                    ? (jsonRespone).ToObject<ParkingEvent>()
                    : new ParkingEvent();
            }
            Save(parkingEvent);
            return parkingEvent;
        }

        private void Save(ParkingEvent parkingEvent)
        {
            if (parkingEvent == null)
                return;
            using (PredixContext context = new PredixContext())
            {
                context.ParkingEvents.AddOrUpdate(x => new { x.LocationUid, x.EventType }, parkingEvent);
                context.SaveChanges();
            }
        }
    }
}
