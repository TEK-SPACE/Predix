using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Predix.Domain.Model.Location
{
    //[Table("LocationDetailProperties", Schema = "dbo")]
    public class LocationDetailProperties
    {
        [JsonProperty("PARK_DIRECTION")]
        public string ParkDirection { get; set; }
    }
}
