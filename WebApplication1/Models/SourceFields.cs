using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parser
{
    public class SourceFields
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column(TypeName = "jsonb")]
        public string Properties { get; set; }
        public int SourceId { get; set; }
        public Source Source { get; set; }
    }
}
