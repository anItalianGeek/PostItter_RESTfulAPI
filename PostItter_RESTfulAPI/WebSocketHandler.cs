using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;

public static class WebSocketHandler
{
    // Dictionary per memorizzare tutti i WebSocket per ogni chatId
    private static readonly ConcurrentDictionary<string, List<WebSocket>> ChatClients = new();

    public static async Task HandleWebSocket(HttpContext context, WebSocket webSocket, string chatId)
    {
        // Aggiungi il nuovo WebSocket alla lista dei client per questa chat
        if (!ChatClients.ContainsKey(chatId))
        {
            ChatClients[chatId] = new List<WebSocket>();
        }
        ChatClients[chatId].Add(webSocket);

        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Received message for chat {chatId}: {receivedMessage}");

            // Invia il messaggio a tutti i client connessi a questa chatId
            await BroadcastMessage(chatId, receivedMessage);

            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        // Rimuovi il WebSocket dalla lista quando la connessione si chiude
        ChatClients[chatId].Remove(webSocket);

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    private static async Task BroadcastMessage(string chatId, string message)
    {
        if (ChatClients.ContainsKey(chatId))
        {
            var serverMsg = Encoding.UTF8.GetBytes(message);

            foreach (var socket in ChatClients[chatId])
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(new ArraySegment<byte>(serverMsg, 0, serverMsg.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}
