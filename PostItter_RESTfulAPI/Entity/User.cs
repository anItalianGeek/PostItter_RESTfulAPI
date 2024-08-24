namespace PostItter_RESTfulAPI.Models;

public class User
{
    public string? bio { get; set; }
    public User[]? blockedUsers { get; set; }
    public Post[]? commentedPosts { get; set; }
    public bool? darkMode { get; set; }
    public bool? privateProfile { get; set; }
    public bool? everyoneCanText { get; set; }
    public bool? twoFA { get; set; }
    public bool? likeNotification { get; set; }
    public bool? commentNotification { get; set; }
    public bool? replyNotification { get; set; }
    public bool? followNotification { get; set; }
    public bool? messageNotification { get; set; }
    public string displayName { get; set; }
    public string? email { get; set; }
    public User[]? followers { get; set; }
    public User[]? following { get; set; }
    public string id { get; set; }
    public Post[]? likedPosts { get; set; }
    public Notification[]? notifications { get; set; }
    public Post[]? posts { get; set; }
    public string profilePicture { get; set; }
    public string username { get; set; }
}