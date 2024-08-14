using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostItter_RESTfulAPI.DatabaseContext;
using PostItter_RESTfulAPI.Models.DatabaseModels;
using PostItter_RESTfulAPI.Models;

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

        try
        {
            var dbNotifs = database.notifications
                .Where(n => n.user_receiver == numeric_id);

            if (dbNotifs == null)
                return Ok(null);
            
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
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> addNewNotification([FromBody] Notification notif, string destination)
    {
        if (!long.TryParse(destination, out long numeric_id))
            return BadRequest("Invalid user ID");
        
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
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
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