namespace PostItter_RESTfulAPI.Entity;

public class Chat
{
    public string chatId { get; set; }
    public string chatName { get; set; }
    public List<User> members { get; set; }
    public Message? lastMessage { get; set; }
}