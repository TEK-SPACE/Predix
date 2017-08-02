using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Predic.Pipeline.Helper;
using Predic.Pipeline.Interface;
using Predix.Domain.Model.Constant;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Service
{
    public class EventService : IEvents
    {
        private readonly IPredixHttpClient _predixHttpClient = new PredixHttpClient();
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

        public ParkingEvent Get(string locationUid, string eventType)
        {
            ParkingEvent details = null;
            Dictionary<string, string> additionalHeaders =
                new Dictionary<string, string> { { "predix-zone-id", "SDSIM-IE-PARKING" } };
            //string bodyMessage = $"{{\"locationUid\":\"{locationUid}\",\"eventTypes\":[\"{eventType}\"]}}";
            string bodyMessage = $"{{\"bbox\":\"32.715675:-117.161230,32.708498:-117.151681\",\"eventTypes\":[\"PKIN\"]}}";
            var response = _predixWebSocketClient.GetAllAsync(Endpoint.WebSocketUrl, bodyMessage, additionalHeaders);
            if (!string.IsNullOrWhiteSpace(response.Result))
            {
                var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
                details = jsonRespone["content"] != null
                    ? ((JArray)jsonRespone["content"]).ToObject<ParkingEvent>()
                    : new ParkingEvent();
            }
            return details;
        }
    }
}
