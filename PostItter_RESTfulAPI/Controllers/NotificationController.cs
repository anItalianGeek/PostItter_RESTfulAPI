using Microsoft.AspNetCore.Mvc;
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

    [HttpGet("get/{id:string}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult getNotifications(string id)
    {
        long numeric_id = Convert.ToInt64(id);
        try
        {
            NotificationDto[] dbNotifs = database.notifications.Where(record => record.user_receiver == numeric_id).ToArray();
            Notification[] notifs = new Notification[dbNotifs.Length];
            for (int i = 0; i < dbNotifs.Length; i++)
            {
                UserDto sender = database.users.FirstOrDefault(record => record.user_id == dbNotifs[i].user_sender);
                notifs[i] = new Notification
                {
                    id = dbNotifs[i].notification_id.ToString(),
                    message = dbNotifs[i].content,
                    postId = dbNotifs[i].post_ref.ToString(),
                    type = dbNotifs[i].type,
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

    [HttpPost("newTo/{destination:string}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult addNewNotification([FromBody] Notification notif, string destination)
    {
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
            database.notifications.Add(newNotif);
            database.SaveChanges();
            return Created();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }
    
    [HttpDelete("delete/{id:string}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult deleteSingleNotification(string id)
    {
        NotificationDto notification = database.notifications.FirstOrDefault(record => record.notification_id == Convert.ToInt64(id));
        if (notification == null)
            return NotFound();
        try
        {
            database.notifications.Remove(notification);
            database.SaveChanges();
            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpDelete("deleteAll/{id:string}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult deleteAllNotifications(string id)
    {
        long numeric_id = Convert.ToInt64(id);
        try
        { 
            NotificationDto[] notifs = database.notifications.Where(record => record.user_receiver == numeric_id).ToArray();
            database.notifications.RemoveRange(notifs);
            database.SaveChanges();
            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }
}