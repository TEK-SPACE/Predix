using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Predix.Domain.Model.Enum;

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
        /// Vehicle Id
        /// </summary>
        public string ObjectUid { get; set; }

        /// <summary>
        /// Location or Node ID
        /// </summary>
        [StringLength(250)]
        public string LocationUid { get; set; }

        [ForeignKey("LocationUid")] public Location.Location Location { get; set; }

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

        /// <summary>
        /// Haris needs its for PEMS side
        /// </summary>
        public bool IsException { get; set; }

        /// <summary>
        /// Haris needs its for PEMS side
        /// </summary>
        public bool IsViolated { get; set; }

        public int RegulationId { get; set; }
        public ViolationType ViolationType { get; set; }
        public DateTime? EventInDateTime { get; set; }
        public DateTime? EventOutDateTime { get; set; }
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDateTime { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar")]
        [StringLength(4000)]
        public string Note { get; set; }

        [Column(TypeName = "nvarchar")]
        [StringLength(200)]
        public string UserAction { get; set; }

        [Column(TypeName = "nvarchar")]
        [StringLength(200)]
        public string NewStatus { get; set; }

        [Column(TypeName = "nvarchar")]
        [StringLength(200)]
        public string CitationNumber { get; set; }

        [Column(TypeName = "nvarchar")]
        [StringLength(200)]
        public string UserName { get; set; }

        [Column(TypeName = "nvarchar")]
        [StringLength(200)]
        public string UserId { get; set; }
    }
}