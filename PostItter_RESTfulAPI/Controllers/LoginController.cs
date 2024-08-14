using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostItter_RESTfulAPI.DatabaseContext;
using PostItter_RESTfulAPI.Models;
using PostItter_RESTfulAPI.Models.DatabaseModels;

namespace PostItter_RESTfulAPI.Controllers;

[Route("api/login")]
[ApiController]
public class LoginController : ControllerBase
{
    private readonly ApplicationDbContext database;
    
    public LoginController(ApplicationDbContext db)
    {
        database = db;
    }

    [HttpGet("check/{name}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> checkAvailability(string name)
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
    
    [HttpPost]
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
        else return Ok(new JwtWebToken
        {
            sub = user.user_id.ToString(),
            displayname = user.displayname,
            username = user.username,
            iat = DateTime.Now,
            exp = DateTime.Now.AddDays(7)
        });
    }

    [HttpPost("signup")]
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
            darkMode = false,
            displayname = input.displayName,
            username = input.username,
            email = input.email, // TODO Must check if there is a way to see if email exists!! (using regex)
            password = input.password,
            everyoneCanText = true,
            privateProfile = false,
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
        
        return Ok(new JwtWebToken
        {
            sub = (await database.users.OrderByDescending(e => e.user_id).FirstOrDefaultAsync()).user_id.ToString(),
            displayname = newDbUser.displayname,
            username = newDbUser.username,
            iat = DateTime.Now,
            exp = DateTime.Now.AddDays(7)
        });
    }
}