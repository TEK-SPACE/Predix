using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Predix.Domain.Model;
using Predix.Domain.Model.Constant;
using Predix.Domain.Model.Location;
using Predix.Pipeline.Helper;
using Predix.Pipeline.Interface;

namespace Predix.Pipeline.Service
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
        public List<ParkingEvent> Get(string locationUid, string eventType, string epochStartTime, string epochEndTime)
        {
            Dictionary<string, string> additionalHeaders =
                new Dictionary<string, string> { { "Predix-Zone-Id", Endpoint.PredixZoneIdForParking } };
            var response = _predixHttpClient.GetAllAsync(Endpoint.GetEventsByLocation
                   .Replace("{locationUid}", locationUid)
                   .Replace("{eventType}", eventType)
                   .Replace("{epochStartTime}", epochStartTime + "000")
                   .Replace("{epochEndTime}", epochEndTime + "000"), additionalHeaders);
            if (string.IsNullOrWhiteSpace(response.Result))
                return new List<ParkingEvent>();

            var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
            var parkingEvents = jsonRespone["content"].ToObject<List<ParkingEvent>>();
            return parkingEvents ?? new List<ParkingEvent>();
        }
        public void GetByBoundaryAsync(string bbox, string eventType1, string eventType2, IImage imageService, Options options, Customer customer)
        {
            //ParkingEvent parkingEvent = null;
            Dictionary<string, string> additionalHeaders =
                new Dictionary<string, string> { { "predix-zone-id",  Endpoint.PredixZoneIdForParking } };
            string bodyMessage = $"{{\"bbox\":\"{bbox}\",\"eventTypes\":[\"{eventType1}\",\"{eventType2}\" ]}}";
            while (true)
            {
                _predixWebSocketClient.OpenAsync(Endpoint.WebSocketUrlForEvents, bodyMessage, additionalHeaders,
                    imageService, options, customer);
            }

            //if (!string.IsNullOrWhiteSpace(response.Result))
            //{
            //    var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
            //    parkingEvent = jsonRespone != null
            //        ? (jsonRespone).ToObject<ParkingEvent>()
            //        : new ParkingEvent();
            //}
            //await SaveAsync(parkingEvent);
            //return parkingEvent;
            // ReSharper disable once FunctionNeverReturns
        }
        public void GetByLocation(string locationUid, string eventType, IImage imageService,Options options, Customer customer)
        {
            ParkingEvent parkingEvent = null;
            Dictionary<string, string> additionalHeaders =
                new Dictionary<string, string> { { "predix-zone-id", Endpoint.PredixZoneIdForParking } };
            string bodyMessage = $"{{\"locationUid\":\"{locationUid}\",\"eventTypes\":[\"{eventType}\"]}}";
             _predixWebSocketClient.OpenAsync(Endpoint.WebSocketUrlForEvents, bodyMessage, additionalHeaders, imageService, options, customer: customer);
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
