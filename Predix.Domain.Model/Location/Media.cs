using Newtonsoft.Json;

namespace Predix.Domain.Model.Location
{
   public  class Media
   {
        [JsonProperty("pollUrl")]
        public string PollUrl { get; set; }
        [JsonProperty("noOfElements")]
        public int NoOfElements { get; set; }
    }
}
