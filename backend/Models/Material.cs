using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_proj.Models
{

    public class Material
    {
        [Key]
        public int MaterialId { get; set; }

        [Required, MaxLength(40)]
        public string MaterialName { get; set; }

        [Required, MaxLength(10)]
        public string MaterialCode { get; set; } 

        [Required, MaxLength(10)]
        public string Level { get; set; } 

        [Required, MaxLength(50)]
        public string Department { get; set; } 

        [Required]
        public int Term { get; set; } 

        [ForeignKey("Doctor")]
        public int DoctorId { get; set; }

        public virtual Doctor Doctor { get; set; }
        public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
        public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }


}
