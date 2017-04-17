using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Predix.Domain.Model.Location
{
    [Table("LocationMeasures", Schema = "dbo")]
    public abstract class Measures : CommonEntity
    {
        [JsonIgnore]
        public int ActivityId { get; set; }

        [JsonIgnore]
        [ForeignKey("ActivityId")]
        public virtual Activity Activity { get; set; }
        [JsonIgnore]
        public int LocationUid { get; set; }

        [JsonIgnore]
        [ForeignKey("LocationUid")]
        public Details LocationDetails { get; set; }
    }
}
