using System;
using System.ComponentModel.DataAnnotations;
using Predix.Domain.Model.Enum;

namespace Predix.Domain.Model
{
    public class ParkingRegulation
    {
        [Key]
        public int RegualationId { get; set; }
        public string Description { get; set; }
        public string DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool ParkingAllowed { get; set; }
        public int Duration { get; set; }
        public bool Metered { get; set; }
        public double HourlyRate { get; set; }
        public string Coodrinate1 { get; set; }
        public string Coodrinate2 { get; set; }
        public string Coodrinate3 { get; set; }
        public string Coodrinate4 { get; set; }
        public string Address { get; set; }
        public ViolationType ViolationType { get; set; }
        public bool IsActive { get; set; }
    }
}