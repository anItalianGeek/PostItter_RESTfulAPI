using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic.CompilerServices;
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

    [HttpGet("/retrieveChats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Chat[]>> getAllChats(string user_id)
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
            List<ChatDto> chats = await database.chats.Where(record => record.member_id == numeric_id).ToListAsync();
            List<Chat> returnedChats = new List<Chat>();
            
            foreach (ChatDto chat in chats)
            {
                returnedChats.Add(
                    new Chat
                    {
                        chat_id = chat.chat_id.ToString(),
                        chat_name = chat.chat_name,
                        member_id = chat.member_id
                    }
                );
            }
            
            return Ok(returnedChats);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error.");
        }
    }

    [HttpGet("/chat/{chat_id}")]
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
                await database.messages.Where(record => record.chat_ref == numeric_id).ToListAsync();
            List<Message> returnedMessages = new List<Message>();

            foreach (MessageDto message in messages)
            {
                returnedMessages.Add(
                    new Message
                    {
                        content = message.content,
                        file_url = message.file_url,
                        sender_id = message.sender_id
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

    [HttpPost("/createChat")]
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
            await database.chats.AddAsync(new ChatDto
                {
                    chat_id = chatIds[chatIds.Count - 1],
                    chat_name = chatMember.username,
                    member_id = chatMember.user_id
                }
            );
            await database.SaveChangesAsync();

            return Created();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error.");
        }
    }

    [HttpPost("/chat/{chat_id}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> sendMessage(string chat_id, [FromBody] Message message)
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
            ChatDto chat = await database.chats.FirstOrDefaultAsync(record => record.chat_id == numeric_id);

            if (chat == null)
                return NotFound("Requested Chat Doesn't Exist.");

            await database.messages.AddAsync(new MessageDto
                {
                    content = message.content,
                    chat_ref = numeric_id,
                    file_url = message.file_url,
                    sender_id = message.sender_id
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
}