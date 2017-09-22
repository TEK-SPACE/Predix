using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Predix.Domain.Model.Location
{
    [Table("Images", Schema = "dbo")]
    public class Image : CommonEntity
    {
        [JsonIgnore]
        [Key][StringLength(255)]
        public string ImageAssetUid { get; set; }

        [JsonProperty("last")]
        public bool Last { get; set; }
        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }
        [JsonProperty("totalElements")]
        public int TotalElements { get; set; }
        [JsonProperty("first")]
        public bool First { get; set; }
        [JsonProperty("numberOfElements")]
        public int NumberOfElements { get; set; }
        [JsonProperty("size")]
        public int Size { get; set; }
        [JsonProperty("number")]
        public int Number { get; set; }
        public string Status { get; set; }
        [JsonProperty("listOfEntries")] [NotMapped]
        public Entries Entry { get; set; }
        [JsonIgnore]
        public int? ActivityId { get; set; }
        [JsonIgnore]
        [ForeignKey("ActivityId")]
        public virtual Activity Activity { get; set; }
        [JsonIgnore]
        public string Base64 { get; set; }
    }

    public class Entries
    {
        [JsonProperty("content")]
        public Content[] Contents { get; set; }
    }

    [Table("ImageContents", Schema = "dbo")]
    public class Content
    {
        [JsonProperty("mediaType")]
        public string MediaType { get; set; }
        [JsonProperty("mediaFileName")]
        public string MediaFileName { get; set; }
        [JsonProperty("mediaTimestamp")]
        public string MediaTimestap { get; set; }
        [JsonProperty("assetUid")]
        [Key]
        [StringLength(255)]
        public string AssetUid { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
