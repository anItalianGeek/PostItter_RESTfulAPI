using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostItter_RESTfulAPI.Models.DatabaseModels;

public class PasswordRecoveryTokenDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long record_id { get; set; }
    public string user_email { get; set; }
    public DateTime expiring_at { get; set; }
    public string token { get; set; }
}