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
    public class LocationService : ILocation, IImage
    {
        private readonly IPredixHttpClient _predixHttpClient = new PredixHttpClient();
        private readonly IPredixWebSocketClient _predixWebSocketClient = new PredixWebSocketClient();

        public List<Location> All(string bbox, string locationType, int size)
        {
            List<Location> identifiers = new List<Location>();
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
                        ? ((JArray) jsonRespone["content"]).ToObject<List<Location>>()
                        : new List<Location>());
                    totalPages = jsonRespone["totalPages"] != null ? (int) jsonRespone["totalPages"] : 0;
                    pageNumber++;
                }
            }
            return identifiers;
        }

        public List<ParkingEvent> Get(string locationUid, string eventType, DateTime startDate, DateTime endTime)
        {
            List<ParkingEvent> details = new List<ParkingEvent>();
            Dictionary<string, string> additionalHeaders =
                new Dictionary<string, string> { { "Predix-Zone-Id", "ics-IE-PARKING" } };
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
                new Dictionary<string, string> { { "Predix-Zone-Id", "ics-IE-PARKING" } };
            string bodyMessage = $"{{\"locationUid\":\"{locationUid}\",\"eventTypes\":[\"{eventType}\"]}}";
            //bodyMessage = $"{{\"bbox\":\"32.715675:-117.161230,32.708498:-117.151681\",\"eventTypes\":[\"PKIN\"]}}";
            var response = _predixWebSocketClient.GetAllAsync(Endpoint.WebSocketUrl, bodyMessage, additionalHeaders);
            if (!string.IsNullOrWhiteSpace(response.Result))
            {
                var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
                details = jsonRespone["content"] != null
                    ? ((JArray) jsonRespone["content"]).ToObject<ParkingEvent>()
                    : new ParkingEvent();
            }
            return details;
        }

        public void SaveLocationKeys(List<Location> locationKeys)
        {
            throw new NotImplementedException();
        }

        public void SaveLocationDetails(List<ParkingEvent> locationDetails)
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
