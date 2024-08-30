using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostItter_RESTfulAPI.Models.DatabaseModels;

public class MessageDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long message_id { get; set; }
    public string content { get; set; }
    public string file_url { get; set; }
    public long sender_id { get; set; }
    public long chat_ref { get; set; }
    
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime sent_at { get; set; }
}