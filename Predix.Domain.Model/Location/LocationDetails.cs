using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Predix.Domain.Model.Location
{
    [Table("LocationDetails", Schema = "dbo")]
    public class LocationDetails
    {
        [JsonProperty("locationUid")][Key][StringLength(250)]
        public string LocationUid { get; set; }
        [JsonProperty("locationType")]
        public string LocationType { get; set; }
        [JsonProperty("parentLocationUid")]
        public string ParentLocationUid { get; set; }
        [JsonProperty("coordinatesType")]
        public string CoordinatesType { get; set; }
        [JsonProperty("coordinates")]
        public string Coordinates { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("city")]
        public string City { get; set; }
        [JsonProperty("state")]
        public string State { get; set; }
        [JsonProperty("country")]
        public string Country { get; set; }
        [JsonProperty("zipcode")]
        public string Zipcode { get; set; }
        [JsonProperty("timezone")]
        public string Timezone { get; set; }
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("properties")]
        public LocationDetailProperties Properties { get; set; } = new LocationDetailProperties();
        [JsonProperty("analyticCategory")]
        public LocalDetailAnalyticCategory AnalyticCategory { get; set; }
    }
}
