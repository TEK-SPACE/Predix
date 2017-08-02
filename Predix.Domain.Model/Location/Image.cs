﻿using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Predix.Domain.Model.Location
{
    [Table("Images", Schema = "dbo")]
    public class Image : CommonEntity
    {
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
        [JsonProperty("listOfEntries")]
        public Entries Entry { get; set; }
        [JsonIgnore]
        public int ActivityId { get; set; }
        [JsonIgnore]
        [ForeignKey("ActivityId")]
        public virtual Activity Activity { get; set; }
    }

    public class Entries
    {
        [JsonProperty("content")]
        public Content[] Contents { get; set; }
    }
    public class Content
    {
        [JsonProperty("mediaType")]
        public string MediaType { get; set; }
        [JsonProperty("mediaFileName")]
        public string MediaFileName { get; set; }
        [JsonProperty("mediaTimestamp")]
        public string MediaTimestap { get; set; }
        [JsonProperty("assetUid")]
        public string AssetUid { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
