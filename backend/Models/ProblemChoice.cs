using Graduation_proj.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Graduation_proj.Models
{
    public class ProblemChoice
    {
        [Key]
        public int ChoiceId { get; set; }

        [Required, MaxLength(255)]
        public string Choices { get; set; }
        public string? ChoiceImagePath { get; set; }

        public int UnitOrder { get; set; }

        [ForeignKey("Problem")]
        public int ProblemId { get; set; }

        public virtual Problem Problem { get; set; }
    }
}
