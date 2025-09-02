using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_proj.Models
{

    public class Topic
    {
        [Key]
        public int TopicId { get; set; }

        [Required, MaxLength(40)]
        public string TopicName { get; set; }

        [ForeignKey("Material")]
        public int MaterialId { get; set; }

        public virtual Material Material { get; set; }
        public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    }

}
