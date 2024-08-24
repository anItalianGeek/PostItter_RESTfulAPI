using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostItter_RESTfulAPI.Models.DatabaseModels;

public class UserSettingsDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long record_id { get; set; }
    public long user { get; set; }
    public bool darkMode { get; set; }
    public bool privateProfile { get; set; }
    public bool everyoneCanText { get; set; }
    public bool twoFA { get; set; }
    public bool likeNotification { get; set; }
    public bool commentNotification { get; set; }
    public bool replyNotification { get; set; }
    public bool followNotification { get; set; }
    public bool messageNotification { get; set; }
}