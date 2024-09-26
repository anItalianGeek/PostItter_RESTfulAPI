using System.Text.Json;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using PostItter_RESTfulAPI.DatabaseContext;
using PostItter_RESTfulAPI.Entity;
using PostItter_RESTfulAPI.Entity.DatabaseModels;

namespace PostItter_RESTfulAPI.Controllers;

[Route("api/authguard")]
[ApiController]
public class LoginController : ControllerBase
{
    private readonly ApplicationDbContext database;
    
    public LoginController(ApplicationDbContext db)
    {
        database = db;
    }

    [HttpGet("check")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> checkAvailability([FromQuery] string name = "", [FromQuery] string email = "")
    {
        bool[] values = new bool[2]{ false, false };
        try
        {
            if (!name.IsNullOrEmpty() && await database.users.FirstOrDefaultAsync(record => record.username == name) == null)
                values[0] = true;
            if (!email.IsNullOrEmpty() && await database.users.FirstOrDefaultAsync(record => record.email == email) == null)
                values[1] = true;

            return Ok(values);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpGet("2faCheck")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> check2fa([FromQuery] string email_user)
    {
        try
        {
            UserDto user = await database.users.FirstOrDefaultAsync(record => record.email == email_user);
            if (user == null)
                return NotFound("User does not exist");
            
            if ((await database.settings.FirstOrDefaultAsync(record => record.user == user.user_id)).twoFA) return Ok(true);
            else return Ok(false);
        }
        catch (Exception e)
        {
            return StatusCode(500, $"Internal Server Error. {e.Message}");
        }
    }
    
    [HttpPost("login")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status406NotAcceptable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> login([FromBody] LoginUserInput input)
    {
        if (
            input.password.Contains("'") || 
            input.email.Contains("'") ||
            input.password.Contains("<") || 
            input.email.Contains("<") ||
            input.password.Contains(">") || 
            input.email.Contains(">") ||
            input.password.Contains("&") || 
            input.email.Contains("&")
        ) return StatusCode(406, "Input not acceptable");

        string hashedPassword = Hasher.HashPassword(input.password);
        UserDto user = await database.users.FirstOrDefaultAsync(record => record.email == input.email && record.password == hashedPassword);
        UserSettingsDto settings = await database.settings.FirstOrDefaultAsync(record => record.user == user.user_id);
        
        if (user == null) return NotFound("User Does Not Exist.");
        else if (settings == null) return NotFound("Server is missing settings for the desired user.");
        else if (settings.twoFA) return RedirectPreserveMethod($"http://localhost:5265/api/2fa/authenticate?id_active_user={user.user_id}");
        else
        {
            string s_signature = "";
            string chars = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!£$%&/()=?^*§°_:;><-|\{}[]#@.,'";
            Random rnd = new Random();
            for (int i = 0; i < 64; i++)
                s_signature += chars[rnd.Next(0, chars.Length)];

            JwtWebToken newToken = new JwtWebToken
            {
                sub = user.user_id.ToString(),
                displayname = user.displayname,
                username = user.username,
                iat = DateTime.Now,
                exp = DateTime.Now.AddDays(7),
                server_signature = s_signature
            };
            try
            {
                await database.activeUsers.AddAsync(new ActiveUsersDto
                    {
                        user_ref = user.user_id,
                        encodedToken = Base64UrlEncoder.Encode(newToken.sub) +
                                       Base64UrlEncoder.Encode(newToken.displayname) +
                                       Base64UrlEncoder.Encode(newToken.username) +
                                       Base64UrlEncoder.Encode(newToken.iat.ToString()) +
                                       Base64UrlEncoder.Encode(newToken.exp.ToString()) +
                                       Base64UrlEncoder.Encode(newToken.server_signature)
                    }
                );
                await database.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return StatusCode(500, $"Internal Server Error. {(e.InnerException == null ? e.InnerException.Message : e.Message)}");
            }

            return Ok(newToken);
        }
    }

    [HttpPost("signup")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status406NotAcceptable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JwtWebToken>> signup([FromBody] LoginUserInput input)
    {
        if (
            input.password.Contains("'") || 
            input.email.Contains("'") ||
            input.password.Contains("<") || 
            input.email.Contains("<") ||
            input.password.Contains(">") || 
            input.email.Contains(">") ||
            input.password.Contains("&") || 
            input.email.Contains("&")
        ) return StatusCode(406, "Input not acceptable");
        
        if (await database.users.FirstOrDefaultAsync(record => record.email == input.email || record.password == input.password) != null)
            return StatusCode(406, "User Already Exists.");
        
        try
        {
            await database.users.AddAsync(new UserDto
            {
                bio = "",
                displayname = input.displayName,
                username = input.username,
                email = input.email,
                password = Hasher.HashPassword(input.password),
                profilePicture = "blank.jpeg",
                secureKey2fa = ""
            });
            
            await database.SaveChangesAsync();
            
            await database.settings.AddAsync(new UserSettingsDto
            {
                twoFA = false,
                commentNotification = true,
                darkMode = false,
                everyoneCanText = true,
                followNotification = true,
                likeNotification = true,
                messageNotification = true,
                privateProfile = false,
                replyNotification = true,
                user = (await database.users.FirstOrDefaultAsync(record => record.email == input.email)).user_id,
            });
            
            await database.SaveChangesAsync();
        }
        catch (Exception e)
        {
            return StatusCode(500, $"Internal Server Error Occurred while adding new data. {e.Message}");
        }
        
        string s_signature = "";
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        Random rnd = new Random();
        for (int i = 0; i < 64; i++)
            s_signature += chars[rnd.Next(0, chars.Length)];
        
        JwtWebToken newToken = new JwtWebToken
        {
            sub = (await database.users.OrderByDescending(e => e.user_id).FirstOrDefaultAsync()).user_id.ToString(),
            displayname = input.displayName,
            username = input.username,
            iat = DateTime.Now,
            exp = DateTime.Now.AddDays(7),
            server_signature = s_signature
        };

        try
        {
            await database.activeUsers.AddAsync(new ActiveUsersDto
                {
                    user_ref = Convert.ToInt64(newToken.sub),
                    encodedToken = Base64UrlEncoder.Encode(newToken.sub) +
                                   Base64UrlEncoder.Encode(newToken.displayname) +
                                   Base64UrlEncoder.Encode(newToken.username) +
                                   Base64UrlEncoder.Encode(newToken.iat.ToString()) +
                                   Base64UrlEncoder.Encode(newToken.exp.ToString()) +
                                   Base64UrlEncoder.Encode(newToken.server_signature)
                }
            );
            await database.SaveChangesAsync();
        }
        catch (Exception e)
        {
            return StatusCode(500, $"Internal Server Error. {e.Message}");
        }

        return Ok(newToken);
    }

    [HttpDelete("logout/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> logout(string id)
    {
        if (!long.TryParse(id, out long userId))
            return StatusCode(400, $"User not found.");
        
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
            //database.activeUsers.Remove(activeUser);
            database.activeUsers.Remove(await database.activeUsers.FirstOrDefaultAsync(record => record.user_ref == userId));
            await database.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error.");
        }
    }
    
}