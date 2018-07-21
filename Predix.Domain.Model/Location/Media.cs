using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Predix.Domain.Model.Location
{
    public class Media
    {
        [JsonIgnore] [Key] public int Id { get; set; }
        [JsonIgnore] [StringLength(250)] public string ImageAssetUid { get; set; }
        [JsonProperty("pollUrl")] public string PollUrl { get; set; }
        [JsonProperty("noOfElements")] public int NoOfElements { get; set; }
    }
}