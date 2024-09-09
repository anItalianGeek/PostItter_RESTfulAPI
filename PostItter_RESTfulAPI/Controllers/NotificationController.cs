using System.Text.Json;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using PostItter_RESTfulAPI.DatabaseContext;
using PostItter_RESTfulAPI.Entity;
using PostItter_RESTfulAPI.Entity.DatabaseModels;

namespace PostItter_RESTfulAPI.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly ApplicationDbContext database;

    public NotificationController(ApplicationDbContext _database)
    {
        database = _database;
    }

    [HttpGet("get/{id}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Notification[]>> getNotifications(string id)
    {
        if (!long.TryParse(id, out long numeric_id))
            return BadRequest("Invalid user ID");

        /*if (Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
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
        }*/
        
        try
        {
            var dbNotifs = database.notifications
                .Where(n => n.user_receiver == numeric_id);

            if (dbNotifs == null) return Ok(null);
            
            Notification[] notifs = new Notification[dbNotifs.Count()];
            for (int i = 0; i < dbNotifs.Count(); i++)
            {
                UserDto sender = await database.users.FirstOrDefaultAsync(record => record.user_id == dbNotifs.ElementAt(i).user_sender);
                notifs[i] = new Notification
                {
                    id = dbNotifs.ElementAt(i).notification_id.ToString(),
                    message = dbNotifs.ElementAt(i).content,
                    postId = dbNotifs.ElementAt(i).post_ref.ToString(),
                    type = dbNotifs.ElementAt(i).type,
                    user = new User
                    {
                        id = sender.user_id.ToString(),
                        profilePicture = sender.profilePicture,
                        displayName = sender.displayname,
                        username = sender.username
                    }
                };
            }

            return Ok(notifs);
        } 
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPost("newTo/{destination}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> addNewNotification([FromBody] Notification notif, string destination, [FromQuery] string id_current_user)
    {
        if (!long.TryParse(destination, out long numeric_id))
            return BadRequest("Invalid user ID");
        
        if (!long.TryParse(id_current_user, out long currentUser))
            return BadRequest("Invalid current user ID");
            
        /*if (Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
        {
            string token = authHeader.ToString().Replace("Bearer ", "");
            JwtWebToken jwtWebToken = JsonSerializer.Deserialize<JwtWebToken>(token);

            string check = Base64UrlEncoder.Encode(jwtWebToken.sub) +
                           Base64UrlEncoder.Encode(jwtWebToken.displayname) +
                           Base64UrlEncoder.Encode(jwtWebToken.username) +
                           Base64UrlEncoder.Encode(jwtWebToken.iat.ToString()) +
                           Base64UrlEncoder.Encode(jwtWebToken.exp.ToString()) +
                           Base64UrlEncoder.Encode(jwtWebToken.server_signature);

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
        }*/
        
        UserSettingsDto user  = await database.settings.FirstOrDefaultAsync(record => record.user == numeric_id);
        if (user == null)
            return BadRequest("User Not Found");

        switch (notif.type) // user *should* be always notified when getting tagged in posts
        {
            case "new-message":
                if (user.messageNotification == false)
                    return Unauthorized();
                
                notif.message = " sent you a message";
                long chatId = Convert.ToInt64(notif.postId); // here i can use postId to understand the context of the chat
                if ((await database.chats.FirstOrDefaultAsync(record =>
                        record.member_id == currentUser && record.chat_id == chatId)) == null)
                    return Unauthorized();
                break;
            case "request-follow":
                notif.message = "requested to follow you.";

                if (await database.notifications.FirstOrDefaultAsync(record =>
                        record.type == notif.type && record.user_receiver == Convert.ToInt64(destination) &&
                        record.user_sender == Convert.ToInt64(notif.user.id)) != null)
                    return Unauthorized();
                break;
            case "new-follow":
                if (user.followNotification == false)
                    return Unauthorized();
                notif.message = " started following you";
                break;
            case "new-like":
                if (user.likeNotification == false)
                    return Unauthorized();
                notif.message = " liked one of your posts";
                break;
            case "new-comment":
                if (user.commentNotification == false)
                    return Unauthorized();
                notif.message = " commented on one of your posts";
                break;
            case "new-tag":
                notif.message = " tagged you.";
                break;
            default:
                return BadRequest("Invalid notification type");
        }

        NotificationDto newNotif = new NotificationDto
        {
            content = notif.message,
            post_ref = Convert.ToInt64(notif.postId),
            type = notif.type,
            user_sender = Convert.ToInt64(notif.user.id),
            user_receiver = Convert.ToInt64(destination)
        };

        try
        {
            await database.notifications.AddAsync(newNotif);
            await database.SaveChangesAsync();
            return Created();
        }
        catch (Exception e)
        {
            return StatusCode(500, $"Internal Server Error. {(e.InnerException == null ? e.InnerException.Message : e.Message)}");
        }
    }

    [HttpPost("newTo")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> tryTag([FromBody] Notification notification, [FromQuery] string mention)
    {
        /*if (Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
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
        }*/
        
        if (mention[0] == '@')
            mention = mention.Substring(1);
        
        UserDto user = await database.users.FirstOrDefaultAsync(record => record.username == mention);
        
        if (user != null)
            try
            {
                NotificationDto newNotif = new NotificationDto
                {
                    content = "tagged you.",
                    post_ref = notification.postId == null ? 0 : Convert.ToInt64(notification.postId),
                    type = notification.type,
                    user_receiver = user.user_id,
                    user_sender = Convert.ToInt64(notification.user.id)
                }; 
                    
                await database.notifications.AddAsync(newNotif);
                await database.SaveChangesAsync();
                return Created();
            }
            catch (Exception e)
            {
                return StatusCode(500, $"Internal Server Error.  {(e.InnerException != null ? e.InnerException.Message : e.Message)}");
            }

        return NoContent();
    }
    
    [HttpDelete("delete/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> deleteSingleNotification(string id)
    {
        if (!long.TryParse(id, out long numeric_id))
            return BadRequest("Invalid user ID");

        /*if (Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
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
        }*/
        
        NotificationDto notification = await database.notifications.FirstOrDefaultAsync(record => record.notification_id == numeric_id);
        
        if (notification == null)
            return NotFound();
        
        try
        {
            database.notifications.Remove(notification);
            await database.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpDelete("deleteAll/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> deleteAllNotifications(string id)
    {
        if (!long.TryParse(id, out long numeric_id))
            return BadRequest("Invalid user ID");
        
        /*if (Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
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
        }*/
        
        try
        { 
            var notifications = database.notifications.Where(n => n.user_receiver == numeric_id);
            database.notifications.RemoveRange(notifications);
            await database.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }
}