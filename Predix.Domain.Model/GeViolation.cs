using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Predix.Domain.Model
{
    public class GeViolation
    {
        /// <summary>
        /// Vehicle Id
        /// </summary>
        public string ObjectUid;

        /// <summary>
        /// Unique ID
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Location or Node ID
        /// </summary>
        [StringLength(250)]
        public string LocationUid { get; set; }
        [ForeignKey("LocationUid")]
        public Location.Location Location { get; set; }
        /// <summary>
        /// Is a No Parking Vioaltion
        /// </summary>
        public bool NoParking { get; set; }
        /// <summary>
        /// Parking Limit Exceeded
        /// </summary>
        public bool ExceedParkingLimit { get; set; }
        /// <summary>
        /// Parked on Street Sweeping time range
        /// </summary>
        public bool StreetSweeping { get; set; }
        /// <summary>
        /// Event In Time
        /// </summary>
        public TimeSpan? ParkinTime { get; set; }
        /// <summary>
        /// Event Out Time
        /// </summary>
        public TimeSpan? ParkoutTime { get; set; }
        /// <summary>
        /// Duration in Minutes
        /// </summary>
        public long ViolationDuration { get; set; }
    }
}