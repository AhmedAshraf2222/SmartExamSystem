using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_proj.Models
{
    public class Exam
    {
        [Key]
        public int ExamId { get; set; }

        [ForeignKey("Material")]
        public int MaterialId { get; set; }

        [Required, MaxLength(40)]
        public string? ExamName { get; set; }
        public int MainDegree { get; set; }
        public int TotalProblems { get; set; }
        public bool Shuffle { get; set; }
        [Required]
        public int ExamDuration { get; set; }
        [Required]
        public DateTime ExamDate { get; set; }

        [MaxLength(100)]
        public string UniversityName { get; set; }

        [MaxLength(100)]
        public string CollegeName { get; set; }

        public virtual Material? Material { get; set; }
        public virtual ICollection<ExamUnit> ExamUnits { get; set; } = new List<ExamUnit>();
    }

}
