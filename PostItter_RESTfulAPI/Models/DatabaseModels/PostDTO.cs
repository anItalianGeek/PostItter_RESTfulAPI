using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostItter_RESTfulAPI.Models.DatabaseModels;

public class PostDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long post_id { get; set; }
    public string body { get; set; }
    public long post_ref { get; set; }
    public int likes { get; set; }
    public int reposts { get; set; }
    public int shares { get; set; }
    public long user_id { get; set; }
}