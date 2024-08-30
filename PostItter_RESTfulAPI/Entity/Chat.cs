namespace PostItter_RESTfulAPI.Models;

public class Chat
{
    public string chatId { get; set; }
    public string chatName { get; set; }
    public List<User> members { get; set; }
    public Message? lastMessage { get; set; }
}