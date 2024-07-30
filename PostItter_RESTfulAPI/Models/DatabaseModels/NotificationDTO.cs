using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostItter_RESTfulAPI.Models.DatabaseModels;

public class NotificationDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long notification_id { get; set; }
    public long user_sender { get; set; }
    public long user_receiver { get; set; }
    public string type { get; set; }
    public string content { get; set; }
    public long post_ref { get; set; }
}