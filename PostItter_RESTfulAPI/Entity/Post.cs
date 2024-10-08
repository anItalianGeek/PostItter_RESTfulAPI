namespace PostItter_RESTfulAPI.Entity;

public class Post
{
    public string body { get; set; } // TODO must implement reposts
    public Comment[]? comments { get; set; }
    public string[]? hashtags { get; set; }
    public string id { get; set; }
    public int likes { get; set; }
    public int reposts { get; set; }
    public int shares { get; set; }
    public User user { get; set; }
    public string? color { get; set; }
}