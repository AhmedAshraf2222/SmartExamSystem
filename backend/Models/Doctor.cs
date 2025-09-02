using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Graduation_proj.Models
{
    public class Doctor
    {
        [Key]
        public int DoctorId { get; set; }

        [Required, MaxLength(40)]
        public string? Name { get; set; }

        [Required, MaxLength(255)]
        public string? Email { get; set; }

        [Required, MaxLength(100)]
        public string? Password { get; set; }

        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
    }

}
