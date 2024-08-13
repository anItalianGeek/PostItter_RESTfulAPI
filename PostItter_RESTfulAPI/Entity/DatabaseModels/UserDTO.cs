using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace PostItter_RESTfulAPI.Models.DatabaseModels;

public class UserDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long user_id { get; set; }
    public string bio { get; set; }
    public bool darkMode { get; set; }
    public string displayname { get; set; }
    public string username { get; set; }
    public string email { get; set; }
    public string password { get; set; }
    public bool everyoneCanText { get; set; }
    public bool privateProfile { get; set; }
    public string profilePicture { get; set; }
}