using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace Predix.Domain.Model.Location
{
    [Table("LocationDetails", Schema ="dbo")]
    public class Details : CommonEntity
    {
        /// <summary>
        /// <para>A unique identifier established by a customer or external resource for a specific location within the monitored area. For example, LOCATION-STG-323.</para>
        /// </summary>
        [JsonProperty(PropertyName = "locationUid")]
        public string Uid { get; set; }
        /// <summary>
        /// The customer-defined identifier for a specific asset at this location.
        /// </summary>
        [JsonProperty(PropertyName = "assetUid")]
        public string AssetUid { get; set; }

        /// <summary>
        /// Identifies this type of event:
        /// <para>PKIN: Vehicle entering parking space.</para>
        /// <para>PKOUT:Vehicle exiting parking space.</para>
        /// </summary>
        [JsonProperty(PropertyName = "eventType")]
        public string EventType { get; set; }
        /// <summary>
        /// Actual timestamp records when an event occurred. Timestamps are in EPOCH format.
        /// </summary>
        [JsonProperty(PropertyName = "timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public Properties Properties { get; set; }

        [JsonProperty(PropertyName = "measures")]
        public Measures Measures { get; set; }
        [JsonIgnore]
        public int ActivityId { get; set; }
        [JsonIgnore]
        [ForeignKey("ActivityId")]
        public virtual Activity Activity { get; set; }
    }
}