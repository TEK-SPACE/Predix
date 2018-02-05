using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Predix.Domain.Model.Location
{
    [Table("ParkingEventProperties", Schema = "dbo")]
    public class ParkingEventProperties : CommonEntity
    {
        [JsonIgnore]
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Local coordinates of the identified vehicle in the image. The coordinates of the four corners of the vehicle are provided.
        /// </summary>
        [JsonProperty(PropertyName = "pixelCoordinates")]
        public string PixelCoordinates { get; set; }
        ///// <summary>
        ///// The unique identifier assigned to a specific parking event.
        ///// </summary>
        //[JsonProperty(PropertyName = "eventUid")]
        //public string EventUid { get; set; }
        /// <summary>
        /// The unique identifier of a specific vehicle.
        /// </summary>
        [JsonProperty(PropertyName = "objectUid")]
        public string ObjectUid { get; set; }
        /// <summary>
        /// The GPS coordinates of a vehicle.
        /// </summary>
        [JsonProperty(PropertyName = "geoCoordinates")]
        public string GeoCoordinates { get; set; }
        /// <summary>
        /// The unique identifier of the camera that provided the image.
        /// </summary>
        [JsonProperty(PropertyName = "imageAssetUid")]
        public string ImageAssetUid { get; set; }

        //[JsonIgnore]
        //public int? ActivityId { get; set; }

        //[JsonIgnore]
        //[ForeignKey("ActivityId")]
        //public Activity Activity { get; set; }

        [JsonIgnore]
        public string LocationUid { get; set; }

        //[JsonIgnore]
        //public int ParkingEventId { get; set; }

        //[JsonIgnore]
        //[ForeignKey("ParkingEventId")]
        //public virtual ParkingEvent ParkingEvent { get; set; }

        //[JsonIgnore]
        //[ForeignKey("ImageAssetUid")]
        //public Image Image { get; set; }
    }
}
