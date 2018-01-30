using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Predix.Domain.Model
{
    public class NodeMasterRegulation
    {
        [Key]
        public int Id { get; set; }

        //public int NodeMasterId { get; set; }

        //[ForeignKey("NodeMasterId")]
        //public virtual NodeMaster NodeMaster { get; set; }

        public string LocationUid { get; set; }

        [ForeignKey("LocationUid")]
        public virtual Location.Location Location { get; set; }

        public int RegulationId { get; set; }

        [ForeignKey("RegulationId")]
        public virtual ParkingRegulation ParkingRegulation { get; set; }
    }
}