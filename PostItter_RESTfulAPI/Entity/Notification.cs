namespace PostItter_RESTfulAPI.Entity;

public class Notification
{
    public string id { get; set; }
    public User user { get; set; }
    public string type { get; set; }
    public string? postId { get; set; }
    public string? message { get; set; }
}