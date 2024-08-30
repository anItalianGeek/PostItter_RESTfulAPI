namespace PostItter_RESTfulAPI.Models;

public class Message
{
    public string content { get; set; }
    public string? file_url { get; set; }
    public string sender_username { get; set; }
    public DateTime? sent_at  { get; set; }
}