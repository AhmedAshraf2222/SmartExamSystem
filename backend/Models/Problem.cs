using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_proj.Models
{
    public class Problem
    {
        [Key]
        public int ProblemId { get; set; }

        [ForeignKey("Group")]
        public int GroupId { get; set; }

        [Required, MaxLength(40)]
        public string ProblemName { get; set; }

        [MaxLength(600)]
        public string ProblemHeader { get; set; }
        public string? ProblemImagePath { get; set; }
        public int ChoicesNumber { get; set; }
        public int RightAnswer { get; set; }
        public bool Shuffle { get; set; }
        public int MainDegree { get; set; }

        public virtual Group Group { get; set; }
        public virtual ICollection<ProblemChoice> ProblemChoices { get; set; } = new List<ProblemChoice>();
    }

}
