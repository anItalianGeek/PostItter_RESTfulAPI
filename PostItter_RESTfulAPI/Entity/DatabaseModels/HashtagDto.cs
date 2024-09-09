using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostItter_RESTfulAPI.Entity.DatabaseModels;

public class HashtagDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long hashtag_id { get; set; }
    public string content { get; set; }
    public long post_ref { get; set; }
}