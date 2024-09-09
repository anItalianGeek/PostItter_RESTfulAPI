using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostItter_RESTfulAPI.Entity.DatabaseModels;

public class UserConnectionDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long connection_id { get; set; }
    public long user { get; set; }
    public long following_user { get; set; }
}