using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostItter_RESTfulAPI.Models.DatabaseModels;

public class ChatDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long record_id { get; set; }
    public long chat_id { get; set; }
    public string chat_name { get; set; }
    public long member_id { get; set; }
}