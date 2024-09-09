using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostItter_RESTfulAPI.Entity.DatabaseModels;

public class CommentDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long comment_id { get; set; }
    public string content { get; set; }
    public long user { get; set; }
    public long post { get; set; }
}