namespace PostItter_RESTfulAPI.Models;

public class Notification
{
    public User user { get; set; }
    public string type { get; set; }
    public string? postId { get; set; }
    public string? message { get; set; }
}