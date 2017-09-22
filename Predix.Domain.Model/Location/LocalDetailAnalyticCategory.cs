using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Predix.Domain.Model.Location
{
    //[Table("LocationDetailAnalyticCategory", Schema = "dbo")]
    public class LocalDetailAnalyticCategory
    {
        [JsonProperty("PKOCC")]
        public string PkoCc { get; set; }
        [JsonProperty("PKOVLP")]
        public string PkoVlp { get; set; }
    }
}
