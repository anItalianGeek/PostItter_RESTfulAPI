using Microsoft.AspNetCore.SignalR;

namespace PostItter_RESTfulAPI.Entity;

public class ChatHub : Hub
{
    public async Task SendMessage(string chatId, string content, string file_url, string sender_username, string sent_at)
    {
        await Clients.Group(chatId).SendAsync("ReceiveMessage", content, file_url, sender_username, sent_at);
    }

    public async Task JoinChat(string chatId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
    }

    public async Task LeaveChat(string chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);
    }
}