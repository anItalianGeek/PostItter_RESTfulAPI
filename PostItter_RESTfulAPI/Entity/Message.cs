namespace PostItter_RESTfulAPI.Models;

public class Message
{
    public string content { get; set; }
    public string file_url { get; set; }
    public long sender_id { get; set; }
}