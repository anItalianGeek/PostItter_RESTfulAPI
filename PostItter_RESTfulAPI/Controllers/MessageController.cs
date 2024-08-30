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
            foreach (var chat in chats)
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

            MessageDto lastMessage = await database.messages.Where(record => record.sender_id == currentUser)
                .OrderBy(e => e.sent_at).FirstOrDefaultAsync();
            Chat returnedChat = new Chat
            {
                chatId = chats.FirstOrDefault().chat_id.ToString(),
                chatName = chats.FirstOrDefault().chat_name,
                members = members,
                lastMessage = new Message
                {
                    content = lastMessage.content,
                    file_url = lastMessage.file_url,
                    sender_username = members.FirstOrDefault(e => e.id == lastMessage.sender_id.ToString()).username,
                    sent_at = lastMessage.sent_at,
                }
            };
            
            return Ok(returnedChat);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error.");
        }
    }
    
    [HttpGet("chat/{chat_id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Message[]>> getMessagesFromChat(string chat_id)
    {
        if (!long.TryParse(chat_id, out long numeric_id))
            return BadRequest("Invalid user ID");

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
        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }
        
        try
        {
            List<MessageDto> messages =
                await database.messages.Where(record => record.chat_ref == numeric_id).OrderBy(e => e.sent_at).ToListAsync();
            List<Message> returnedMessages = new List<Message>();

            foreach (MessageDto message in messages)
            {
                returnedMessages.Add(
                    new Message
                    {
                        content = message.content,
                        file_url = message.file_url,
                        sender_username = (await database.users.FirstOrDefaultAsync(record => record.user_id == message.sender_id)).username,
                        sent_at = message.sent_at
                    }
                );
            }

            return Ok(returnedMessages);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error.");
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
        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }
        
        try
        {
            UserDto chatMember = await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id);
            if (chatMember == null)
                return NotFound("Chat cannot be created. Member doesn't exist.");
            
            List<long> chatIds = await database.chats.Select(record => record.chat_id).Distinct().ToListAsync();
            chatIds.Sort((a, b) => (int)a - (int)b); // !

            ChatDto newChat = new ChatDto
            {
                chat_id = chatIds[chatIds.Count - 1],
                chat_name = chatMember.username,
                member_id = chatMember.user_id
            };
            await database.chats.AddAsync(newChat);
            await database.SaveChangesAsync();
            
            return Created("Chat Created.", new Chat
            {
                chatId = newChat.chat_id.ToString(),
                chatName = newChat.chat_name,
                members = [new User
                {
                    profilePicture = chatMember.profilePicture,
                    username = chatMember.username,
                    displayName = chatMember.displayname,
                    id = chatMember.user_id.ToString(),
                }],
                lastMessage = null
            });
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error.");
        }
    }

    [HttpPost("chat/{chat_id}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> sendMessage(string chat_id, [FromBody] Message message)
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
            ChatDto chat = await database.chats.FirstOrDefaultAsync(record => record.chat_id == numeric_id);

            if (chat == null)
                return NotFound("Requested Chat Doesn't Exist.");

            await database.messages.AddAsync(new MessageDto
                {
                    content = message.content,
                    chat_ref = numeric_id,
                    file_url = message.file_url,
                    sender_id = currentUser
                }
            );
            await database.SaveChangesAsync();
            
            return Created();
        }
        catch (Exception)
        {
            return StatusCode(500, "Interal Server Error.");
        }
    }

    [HttpDelete("/chat/{chatId}")]
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