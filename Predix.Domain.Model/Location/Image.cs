using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Predix.Domain.Model.Location
{
    [Table("Images", Schema = "dbo")]
    public abstract class Image : CommonEntity
    {
        [JsonIgnore]
        public int ActivityId { get; set; }
        [JsonIgnore]
        [ForeignKey("ActivityId")]
        public virtual Activity Activity { get; set; }
    }
}
