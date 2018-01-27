using System.ComponentModel.DataAnnotations;

namespace Predix.Domain.Model
{
    public class Boundary
    {
        [Key]
        public int Id { get; set; }
        [StringLength(500)]
        public string Range { get; set; }
        [StringLength(2000)]
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}