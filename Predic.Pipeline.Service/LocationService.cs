using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Predic.Pipeline.DataService;
using Predic.Pipeline.Interface;
using Predix.Domain.Model.Constant;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Service
{
    public class LocationService : ILocation
    {
        private readonly IPredixHttpClient _predixHttpClient;
        private static Dictionary<string, object> _globalVariables;
        public LocationService(Dictionary<string, object> globalVariables)
        {
            _predixHttpClient = new PredixHttpClient(globalVariables);
            _globalVariables = globalVariables;
        }
        public List<Location> All(string bbox, string locationType, int size)
        {
            List<Location> identifiers = new List<Location>();
            int pageNumber = 0;
            int totalPages = 1;
            Dictionary<string, string> additionalHeaders =
                 //new Dictionary<string, string> {{"Predix-Zone-Id", "ics-IE-PARKING"}};
                new Dictionary<string, string> { { "predix-zone-id", "SDSIM-IE-PARKING" } };
            while (totalPages - 1 >= pageNumber)
            {
                var response = _predixHttpClient.GetAllAsync(Endpoint.GetListOfLocation
                    .Replace("{bbox}", bbox)
                    .Replace("{locationType}", locationType)
                    .Replace("{pageNumber}", pageNumber.ToString())
                    .Replace("{pageSize}", size.ToString()), additionalHeaders);
                if (string.IsNullOrWhiteSpace(response.Result)) continue;
                var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
                var locations = jsonRespone["content"] != null
                    ? ((JArray) jsonRespone["content"]).ToObject<List<Location>>()
                    : new List<Location>();
                SaveLocationKeys(locations);
                identifiers.AddRange(locations);
                totalPages = jsonRespone["totalPages"] != null ? (int) jsonRespone["totalPages"] : 0;
                pageNumber++;
            }
            return identifiers;
        }



        public void SaveLocationKeys(List<Location> locationKeys)
        {
            if (!locationKeys.Any())
                return;
            using (PredixContext context = new PredixContext())
            {
                locationKeys.ForEach(x => x.ActivityId = Convert.ToInt32(_globalVariables["ActivityId"]));
                context.Identifiers.AddRange(locationKeys);
                context.SaveChanges();
            }
        }

        public void SaveLocationDetails(List<ParkingEvent> locationDetails)
        {
            throw new NotImplementedException();
        }
    }
}
