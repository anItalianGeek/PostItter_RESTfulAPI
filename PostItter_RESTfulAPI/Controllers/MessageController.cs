using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using PostItter_RESTfulAPI.DatabaseContext;
using PostItter_RESTfulAPI.Models;
using PostItter_RESTfulAPI.Models.DatabaseModels;

namespace PostItter_RESTfulAPI.Controllers;

[ApiController]
[Route("api/messanger")]
public class MessageController : ControllerBase
{
    private readonly ApplicationDbContext database;

    public MessageController(ApplicationDbContext db)
    {
        database = db;
    }

    [HttpGet("retrieveChats/{user_id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Chat[]>> getAllChats(string user_id)
    {
        if (!long.TryParse(user_id, out long numeric_id))
            return BadRequest("Invalid user ID");

        long currentUser;
        if (Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
        {
            string token = authHeader.ToString().Replace("Bearer ", "");
            JwtWebToken jwtWebToken = JsonSerializer.Deserialize<JwtWebToken>(token);

            string check = Base64UrlEncoder.Encode(jwtWebToken.sub) +
                           Base64UrlEncoder.Encode(jwtWebToken.displayname) +
                           Base64UrlEncoder.Encode(jwtWebToken.username) +
                           Base64UrlEncoder.Encode(jwtWebToken.iat.ToString()) +
                           Base64UrlEncoder.Encode(jwtWebToken.exp.ToString()) +
                           Base64UrlEncoder.Encode(jwtWebToken.server_signature);

            currentUser = Convert.ToInt64(jwtWebToken.sub);
            ActiveUsersDto activeUser =
                await database.activeUsers.FirstOrDefaultAsync(record => record.user_ref == currentUser);

            if (activeUser == null)
                return StatusCode(401,
                    "Cannot perform action, user might not be signed up or authorization token might be corrupted.");
            
            if (check != activeUser.encodedToken)
                return StatusCode(401, "Cannot perform action, Authorization Token might be corrupted.");
        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }
        
        try
        {
            var chatGroups = await database.chats
                .Where(chat => chat.member_id == numeric_id)
                .GroupBy(chat => new { chat.chat_id, chat.chat_name })
                .ToListAsync();

            var returnedChats = new List<Chat>();

            foreach (var group in chatGroups)
            {
                var members = await database.chats
                    .Where(c => c.chat_id == group.Key.chat_id)
                    .Join(
                        database.users,
                        chat => chat.member_id,
                        user => user.user_id,
                        (chat, user) => new User
                        {
                            id = user.user_id.ToString(),
                            username = user.username,
                            displayName = user.displayname,
                            profilePicture = user.profilePicture
                        }
                    )
                    .ToListAsync();

                MessageDto lastMessage = await database.messages.Where(record => record.sender_id == currentUser)
                    .OrderBy(e => e.sent_at).FirstOrDefaultAsync();
                returnedChats.Add(new Chat
                {
                    chatId = group.Key.chat_id.ToString(),
                    chatName = group.Key.chat_name,
                    members = members,
                    lastMessage = new Message
                    {
                        content = lastMessage.content,
                        file_url = lastMessage.file_url,
                        sender_username = members.FirstOrDefault(e => e.id == lastMessage.sender_id.ToString()).username,
                        sent_at = lastMessage.sent_at,
                    }
                });
            }

            return Ok(returnedChats);

        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error.");
        }
    }

    [HttpGet("retrieveChat/{chat_id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Chat>> getChat(string chat_id)
    {
        if (!long.TryParse(chat_id, out long numeric_id))
            return BadRequest("Invalid user ID");

        long currentUser;
        if (Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
        {
            string token = authHeader.ToString().Replace("Bearer ", "");
            JwtWebToken jwtWebToken = JsonSerializer.Deserialize<JwtWebToken>(token);

            string check = Base64UrlEncoder.Encode(jwtWebToken.sub) +
                           Base64UrlEncoder.Encode(jwtWebToken.displayname) +
                           Base64UrlEncoder.Encode(jwtWebToken.username) +
                           Base64UrlEncoder.Encode(jwtWebToken.iat.ToString()) +
                           Base64UrlEncoder.Encode(jwtWebToken.exp.ToString()) +
                           Base64UrlEncoder.Encode(jwtWebToken.server_signature);

            currentUser = Convert.ToInt64(jwtWebToken.sub);
            ActiveUsersDto activeUser =
                await database.activeUsers.FirstOrDefaultAsync(record => record.user_ref == currentUser);

            if (activeUser == null)
                return StatusCode(401,
                    "Cannot perform action, user might not be signed up or authorization token might be corrupted.");
            
            if (check != activeUser.encodedToken)
                return StatusCode(401, "Cannot perform action, Authorization Token might be corrupted.");
        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }

        try
        {
            List<ChatDto> chats = await database.chats.Where(record => record.chat_id == numeric_id).ToListAsync();
            List<User> members = new List<User>();
            foreach (ChatDto chat in chats)
            {
                UserDto member = await database.users.FirstOrDefaultAsync(record => record.user_id == chat.member_id);
                members.Add(new User
                {
                    id = member.user_id.ToString(),
                    displayName = member.displayname,
                    username = member.username,
                    profilePicture = member.profilePicture,
                });
            }

            if (chats.Count > 0)
            {
                MessageDto lastMessage = await database.messages.Where(record => record.sender_id == currentUser && record.chat_ref == numeric_id)
                    .OrderBy(e => e.sent_at).FirstOrDefaultAsync();
                
                Chat returnedChat = new Chat
                {
                    chatId = chats.FirstOrDefault().chat_id.ToString(),
                    chatName = chats.FirstOrDefault().chat_name,
                    members = members,
                    lastMessage = lastMessage == null ? null : new Message
                    {
                        content = lastMessage.content,
                        file_url = lastMessage.file_url,
                        sender_username =
                            members.FirstOrDefault(e => e.id == lastMessage.sender_id.ToString()).username,
                        sent_at = lastMessage.sent_at,
                    }
                };
                
                return Ok(returnedChat);
            }

            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error.");
        }
    }
    
    [HttpGet("chat/{chat_id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Message[]>> getMessagesFromChat(string chat_id)
    {
        if (!long.TryParse(chat_id, out long numeric_id))
            return BadRequest("Invalid user ID");

        long currentUser;
        if (Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
        {
            string token = authHeader.ToString().Replace("Bearer ", "");
            JwtWebToken jwtWebToken = JsonSerializer.Deserialize<JwtWebToken>(token);

            string check = Base64UrlEncoder.Encode(jwtWebToken.sub) +
                           Base64UrlEncoder.Encode(jwtWebToken.displayname) +
                           Base64UrlEncoder.Encode(jwtWebToken.username) +
                           Base64UrlEncoder.Encode(jwtWebToken.iat.ToString()) +
                           Base64UrlEncoder.Encode(jwtWebToken.exp.ToString()) +
                           Base64UrlEncoder.Encode(jwtWebToken.server_signature);

            currentUser = Convert.ToInt64(jwtWebToken.sub);
            ActiveUsersDto activeUser =
                await database.activeUsers.FirstOrDefaultAsync(record => record.user_ref == currentUser);

            if (activeUser == null)
                return StatusCode(401,
                    "Cannot perform action, user might not be signed up or authorization token might be corrupted.");
            
            if (check != activeUser.encodedToken)
                return StatusCode(401, "Cannot perform action, Authorization Token might be corrupted.");
        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }
        
        try
        {
            List<MessageDto> messages =
                await database.messages.Where(record => record.chat_ref == numeric_id).OrderBy(e => e.sent_at).ToListAsync();

            if (messages.Count > 0)
            {
                List<Message> returnedMessages = new List<Message>();

                foreach (MessageDto message in messages)
                {
                    returnedMessages.Add(
                        new Message
                        {
                            content = message.content,
                            file_url = message.file_url,
                            sender_username =
                                (await database.users.FirstOrDefaultAsync(record =>
                                    record.user_id == message.sender_id)).username,
                            sent_at = message.sent_at
                        }
                    );
                }

                return Ok(returnedMessages);
            }

            return NoContent();
        }
        catch (Exception e)
        {
            return StatusCode(500, $"Internal Server Error. {e.Message}");
        }
    }

    [HttpPost("createChat/{user_id}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> createChat(string user_id)
    {
        if (!long.TryParse(user_id, out long numeric_id))
            return BadRequest("Invalid user ID");
        
        long currentUserId;
        if (Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
        {
            string token = authHeader.ToString().Replace("Bearer ", "");
            JwtWebToken jwtWebToken = JsonSerializer.Deserialize<JwtWebToken>(token);

            string check = Base64UrlEncoder.Encode(jwtWebToken.sub) +
                           Base64UrlEncoder.Encode(jwtWebToken.displayname) +
                           Base64UrlEncoder.Encode(jwtWebToken.username) +
                           Base64UrlEncoder.Encode(jwtWebToken.iat.ToString()) +
                           Base64UrlEncoder.Encode(jwtWebToken.exp.ToString()) +
                           Base64UrlEncoder.Encode(jwtWebToken.server_signature);

            currentUserId = Convert.ToInt64(jwtWebToken.sub);
            ActiveUsersDto activeUser =
                await database.activeUsers.FirstOrDefaultAsync(record => record.user_ref == currentUserId);

            if (activeUser == null)
                return StatusCode(401,
                    "Cannot perform action, user might not be signed up or authorization token might be corrupted.");
            
            if (check != activeUser.encodedToken)
                return StatusCode(401, "Cannot perform action, Authorization Token might be corrupted.");
        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }
        
        UserDto currentUser = await database.users.FirstOrDefaultAsync(record => record.user_id == currentUserId);
        try
        {
            UserDto chatMember = await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id);
            if (chatMember == null)
                return NotFound("Chat cannot be created. Member doesn't exist.");
            
            List<long> chatIds = await database.chats.Select(record => record.chat_id).Distinct().ToListAsync();
            if (chatIds.Count > 1)
                chatIds.Sort((a, b) => (int)a - (int)b); // !

            ChatDto newChatSelf = new ChatDto
            {
                chat_id = chatIds.Count == 0 ? 0 : chatIds[chatIds.Count - 1],
                chat_name = chatMember.username,
                member_id = currentUser.user_id,
            };
            if (await database.chats.FirstOrDefaultAsync(record => record.chat_id == newChatSelf.chat_id && record.member_id == newChatSelf.member_id) == null)
                await database.chats.AddAsync(newChatSelf);
            
            ChatDto newChat = new ChatDto
            {
                chat_id = chatIds.Count == 0 ? 0 : chatIds[chatIds.Count - 1],
                chat_name = currentUser.username,
                member_id = chatMember.user_id
            };
            
            await database.chats.AddAsync(newChat);
            await database.SaveChangesAsync();

            MessageDto lastMsg = await database.messages.Where(record => record.chat_ref == newChat.chat_id)
                .OrderBy(e => e.sent_at).FirstOrDefaultAsync();

            List<long> usersToSearch = await database.chats.Where(record => record.chat_id == newChat.chat_id).Select(e => e.member_id).ToListAsync();
            List<User> chatMembers = new List<User>();
            foreach (long userId in usersToSearch)
            {
                UserDto member = await database.users.FirstOrDefaultAsync(record => record.user_id == userId);
                chatMembers.Add(new User
                {
                    id = member.user_id.ToString(),
                    username = member.username,
                    displayName = member.displayname,
                    profilePicture = member.profilePicture
                });
            }
                
            return Created("Chat Created.", new Chat
            {
                chatId = newChat.chat_id.ToString(),
                chatName = newChat.chat_name,
                members = chatMembers,
                lastMessage = lastMsg == null ? null : new Message
                {
                    content = lastMsg.content,
                    file_url = lastMsg.file_url,
                    sent_at = lastMsg.sent_at,
                    sender_username = (await database.users.FirstOrDefaultAsync(record => record.user_id == lastMsg.sender_id)).username,
                }
            });
        }
        catch (Exception e)
        {
            return StatusCode(500, $"Internal Server Error. {e.Message}");
        }
    }

    [HttpPost("chat/{chat_id}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendMessage(string chat_id, [FromBody] Message message)
    {
        if (!long.TryParse(chat_id, out long numericChatId))
            return BadRequest("Invalid chat ID.");

        long currentUserId;
        if (Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
        {
            string token = authHeader.ToString().Replace("Bearer ", "");
            JwtWebToken jwtWebToken = JsonSerializer.Deserialize<JwtWebToken>(token);

            string check = Base64UrlEncoder.Encode(jwtWebToken.sub) +
                           Base64UrlEncoder.Encode(jwtWebToken.displayname) +
                           Base64UrlEncoder.Encode(jwtWebToken.username) +
                           Base64UrlEncoder.Encode(jwtWebToken.iat.ToString()) +
                           Base64UrlEncoder.Encode(jwtWebToken.exp.ToString()) +
                           Base64UrlEncoder.Encode(jwtWebToken.server_signature);

            currentUserId = Convert.ToInt64(jwtWebToken.sub);
            ActiveUsersDto activeUser =
                await database.activeUsers.FirstOrDefaultAsync(record => record.user_ref == currentUserId);

            if (activeUser == null)
                return StatusCode(401,
                    "Cannot perform action, user might not be signed up or authorization token might be corrupted.");
            
            if (check != activeUser.encodedToken)
                return StatusCode(401, "Cannot perform action, Authorization Token might be corrupted.");
        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }
        
        try
        {
            ChatDto chat = await database.chats.FirstOrDefaultAsync(record => record.chat_id == numericChatId);

            if (chat == null)
                return NotFound("Requested Chat Doesn't Exist.");

            var newMessage = new MessageDto
            {
                content = message.content,
                chat_ref = numericChatId,
                file_url = message.file_url == null ? null : message.file_url,
                sender_id = currentUserId
            };

            await database.messages.AddAsync(newMessage);
            await database.SaveChangesAsync();

            return Created();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal Server Error: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    [HttpDelete("chat/{chatId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> deleteChat(string chatId)
    {
        if (!long.TryParse(chatId, out long numeric_id))
            return BadRequest("Invalid chat ID");
        
        if (Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
        {
            string token = authHeader.ToString().Replace("Bearer ", "");
            JwtWebToken jwtWebToken = JsonSerializer.Deserialize<JwtWebToken>(token);

            string check = Base64UrlEncoder.Encode(jwtWebToken.sub) +
                           Base64UrlEncoder.Encode(jwtWebToken.displayname) +
                           Base64UrlEncoder.Encode(jwtWebToken.username) +
                           Base64UrlEncoder.Encode(jwtWebToken.iat.ToString()) +
                           Base64UrlEncoder.Encode(jwtWebToken.exp.ToString()) +
                           Base64UrlEncoder.Encode(jwtWebToken.server_signature);

            long currentUser = Convert.ToInt64(jwtWebToken.sub);
            ActiveUsersDto activeUser =
                await database.activeUsers.FirstOrDefaultAsync(record => record.user_ref == currentUser);

            if (activeUser == null)
                return StatusCode(401,
                    "Cannot perform action, user might not be signed up or authorization token might be corrupted.");
            
            if (check != activeUser.encodedToken)
                return StatusCode(401, "Cannot perform action, Authorization Token might be corrupted.");

            try
            {
                ChatDto chat = await database.chats.FirstOrDefaultAsync(record =>
                    record.chat_id == numeric_id && record.member_id == currentUser);
                database.chats.Remove(chat); // on delete, cascade for messages table
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server Error.");
            }

        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }
    }
}