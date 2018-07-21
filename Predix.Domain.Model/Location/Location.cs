using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Predix.Domain.Model.Location
{
    [Table("Locations", Schema = "dbo")]
    public class Location : CommonEntity
    {
        //[JsonIgnore]
        //public int Id { get; set; }
     
        /// <summary>
        /// <para>A unique identifier established by a customer or external resource for a specific location within the monitored area. For example, LOCATION-STG-323.</para>
        /// </summary>
        [JsonProperty(PropertyName = "locationUid")]
        [Key]
        [StringLength(250)]
        public string LocationUid { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// <para>CITIES: Insert the following enumeration codes for a specific location type for CITIES</para>
        /// <para>PARKING_SPOT: PARKING_SPOT consists of demarcated parking spaces within the defined boundaries(Not applicable in v2).</para>
        /// <para>PARKING_ZONE: PARKING_ZONE consists of four geo-coordinates(see coordinateType: GEO) indicating non-demarcated parking spaces within the defined boundaries.</para>
        /// <para>TRAFFIC_ZONE: TRAFFIC_ZONE consists of two geo-coordinates(see coordinateType: GEO) indicating a trip wire in traffic lanes within the defined boundaries.</para>
        /// <para>WALKWAY: WALKWAY consists of two geo-coordinates(see coordinateType: GEO) indicating a trip wire in pedestrian walkways within the defined boundaries.</para>
        /// <para>OTHERS: OTHERS consists of non-standard location types within the defined boundaries.</para>
        /// <para>ENTERPRISES: Insert the following enumeration codes for a specific location type for ENTERPRISES:</para>
        /// <para>REGION: REGION is an enterprise defined geographic area that is a collection of sites.</para>
        /// <para>RETAIL_STORE: RETAIL_STORE consists of retail stores within the defined boundaries.</para>
        /// <para>SITE: SITE is an enterprise defined physical location(e.g.building) that is a collection of zones.</para>
        /// <para>ZONE: ZONE is a enterprise defined collection of assets(e.g.nodes, lights, sensors).</para>
        /// <para>OTHERS: OTHERS consists of non-standard location types.</para>
        /// </summary>
        [JsonProperty(PropertyName = "locationType")]
        public string LocationType { get; set; }
        /// <summary>
        /// The unique identifier assigned to the parent location comprising the locationUids within the monitored area.
        /// </summary>
        [JsonProperty(PropertyName = "parentLocationUid")]
        public string ParentUid { get; set; }
        /// <summary>
        /// <para>GEO: GEO indicates that the coordinate type uses GPS coordinates.</para>
        /// </summary>
        [JsonProperty(PropertyName = "coordinatesType")]
        public string CoordinatesType { get; set; }
        /// <summary>
        /// The GPS coordinates (latitude, longitude) for the referenced asset (identified by assetUid), such as 32.711653,-117.157314 that identify the location of the camera.
        /// </summary>
        [JsonProperty(PropertyName = "coordinates")]
        public string Coordinates { get; set; }
        [JsonIgnore]
        public int? ActivityId { get; set; }
        [JsonIgnore]
        [ForeignKey("ActivityId")]
        public Activity Activity { get; set; }

        //[ForeignKey("LocationUid")]
        //public virtual LocationDetails LocationDetails { get; set; }
        //[JsonIgnore]
        //[ForeignKey("Uid")]
        //public ParkingEvent LocationDetails { get; set; }
    }
}
