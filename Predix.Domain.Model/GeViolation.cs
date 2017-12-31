using System;
using System.ComponentModel.DataAnnotations;

namespace Predix.Domain.Model
{
    public class GeViolation
    {
        /// <summary>
        /// Unique ID
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Location or Node ID
        /// </summary>
        public int NodeId { get; set; }
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