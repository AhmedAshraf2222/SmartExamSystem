using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_proj.Models
{
    public class ExamUnit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UnitOrder { get; set; }

        [ForeignKey("Exam")]
        public int ExamId { get; set; }

        [ForeignKey("Group")]
        public int GroupId { get; set; }

        public int MainDegree { get; set; }
        public int TotalProblems { get; set; }
        public bool Shuffle { get; set; }
        public int AllProblems { get; set; }
        public virtual Exam Exam { get; set; }
        public virtual Group Group { get; set; }
    }

}
