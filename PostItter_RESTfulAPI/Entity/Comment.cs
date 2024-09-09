namespace PostItter_RESTfulAPI.Entity;

public class Comment
{
    public User user { get; set; }
    public string content { get; set; }
}