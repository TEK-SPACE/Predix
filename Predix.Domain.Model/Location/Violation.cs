using System.ComponentModel.DataAnnotations.Schema;

namespace Predix.Domain.Model.Location
{
    public class Violation
    {
        [ForeignKey("objectUid")]
        public string ObjectUid { get; set; }
        public bool IsTimeLimitViolation  { get; set; }
        public bool IsNoParkingViolation { get; set; }
        public bool IsDistance { get; set; }
        public virtual Properties Properties { get; set; }
    }
}
