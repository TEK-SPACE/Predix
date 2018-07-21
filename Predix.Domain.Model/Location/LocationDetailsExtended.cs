using System.ComponentModel.DataAnnotations;

namespace Predix.Domain.Model.Location
{
    public class LocationDetailsExtended
    {
        [Key]
        [StringLength(250)]
        public string LocationUid { get; set; }
        public string Block { get; set; }
        public string Street { get; set; }
    }
}