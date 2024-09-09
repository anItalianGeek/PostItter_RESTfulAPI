using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostItter_RESTfulAPI.Entity.DatabaseModels;

public class UserDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long user_id { get; set; }
    public string bio { get; set; }
    public string profilePicture { get; set; }
    public string displayname { get; set; }
    public string username { get; set; }
    public string email { get; set; }
    public string password { get; set; }
    public string secureKey2fa { get; set; }
}