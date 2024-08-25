using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PostItter_RESTfulAPI.DatabaseContext;
using PostItter_RESTfulAPI.Models;
using PostItter_RESTfulAPI.Models.DatabaseModels;

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
    public async Task<ActionResult<bool>> checkAvailability([FromQuery] string name)
    {
        try
        {
            if (await database.users.FirstOrDefaultAsync(record => record.username == name) == null) return Ok(true);
            else return Ok(false);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }
    
    [HttpPost("login")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status406NotAcceptable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JwtWebToken>> login([FromBody] LoginUserInput input)
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
        
        UserDto user = await database.users.FirstOrDefaultAsync(record => record.email == input.email && record.password == input.password);

        if (user == null) return NotFound("User Does Not Exist.");
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
        
        UserDto newDbUser = new UserDto
        {
            bio = "",
            displayname = input.displayName,
            username = input.username,
            email = input.email,
            password = Hasher.HashPassword(input.password),
            profilePicture = "",
        };
        
        try
        {
            await database.users.AddAsync(newDbUser);
            await database.SaveChangesAsync();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
        
        string s_signature = "";
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        Random rnd = new Random();
        for (int i = 0; i < 64; i++)
            s_signature += chars[rnd.Next(0, chars.Length)];
        
        JwtWebToken newToken = new JwtWebToken
        {
            sub = (await database.users.OrderByDescending(e => e.user_id).FirstOrDefaultAsync()).user_id.ToString(),
            displayname = newDbUser.displayname,
            username = newDbUser.username,
            iat = DateTime.Now,
            exp = DateTime.Now.AddDays(7),
            server_signature = s_signature
        };
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
            
        return Ok(newToken);
    }

    [HttpDelete("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> logout()
    {
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
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
                database.activeUsers.Remove(activeUser);
                await database.SaveChangesAsync();
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