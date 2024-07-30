using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostItter_RESTfulAPI.Models.DatabaseModels;

public class LikeDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long record_id { get; set; }
    private long user { get; set; }
    private long post { get; set; }
}