using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Graduation_proj.Models
{

    public class Group
    {
        [Key]
        public int GroupId { get; set; }

        [ForeignKey("Topic")]
        public int TopicId { get; set; }

        [Required, MaxLength(40)]
        public string GroupName { get; set; }

        [MaxLength(1000)]
        public string? CommonQuestionHeader { get; set; }
        public int TotalProblems { get; set; }
        public int MainDegree { get; set; }
        public bool HasCommonHeader { get; set; }

        public virtual Topic Topic { get; set; }
        public virtual ICollection<Problem> Problems { get; set; } = new List<Problem>();
        public virtual ICollection<ExamUnit> ExamUnits { get; set; } = new List<ExamUnit>();
    }

}
