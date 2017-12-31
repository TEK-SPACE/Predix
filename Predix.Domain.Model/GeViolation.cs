using System;
using System.ComponentModel.DataAnnotations;

namespace Predix.Domain.Model
{
    public class GeViolation
    {
        [Key]
        public int Id { get; set; }
        public int NodeId { get; set; }
        public bool NoParking { get; set; }
        public bool ExceedParkingLimit { get; set; }
        public bool StreetSweeping { get; set; }

        public TimeSpan? ParkinTime { get; set; }
        public TimeSpan? ParkoutTime { get; set; }
        /// <summary>
        /// Duration in Minutes
        /// </summary>
        public long ViolationDuration { get; set; }
    }
}