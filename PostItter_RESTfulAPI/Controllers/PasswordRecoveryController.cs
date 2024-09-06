using Microsoft.AspNetCore.Mvc;
using PostItter_RESTfulAPI.DatabaseContext;
using PostItter_RESTfulAPI.Models.DatabaseModels;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Cors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;

namespace PostItter_RESTfulAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasswordRecoveryController : ControllerBase
{
    private readonly ApplicationDbContext database;

    public PasswordRecoveryController(ApplicationDbContext db)
    {
        database = db;
    }

    [HttpGet("resetPassword")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> resetPassword([FromQuery] string token)
    {
        if (token.IsNullOrEmpty())
            return BadRequest("A token is required.");

        PasswordRecoveryTokenDto request = null;
        try
        {
            request =
                await database.passwordRecoveryTokens.FirstOrDefaultAsync(record => record.token == token);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error.");
        }

        if (request == null)
            return BadRequest("No Token was Provided");
        else if (request.expiring_at < DateTime.Now)
        {
            database.passwordRecoveryTokens.Remove(request);
            await database.SaveChangesAsync();
            return BadRequest("The Token has expired.");
        }
        else
            return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), "Views", "PasswordRecovery", "resetPassword.html"), "text/html");
    }

    [HttpPost("attemptChange")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> AttemptChangePassword([FromForm] string password, [FromForm] string confirmpassword, [FromQuery] string token)
    {
        if (password != confirmpassword)
            return BadRequest("Passwords do not match.");

        PasswordRecoveryTokenDto request = await database.passwordRecoveryTokens.FirstOrDefaultAsync(record => record.token == token);
        if (request == null)
            return BadRequest("Invalid token.");
        else if (request.expiring_at < DateTime.Now)
        {
            database.passwordRecoveryTokens.Remove(request);
            await database.SaveChangesAsync();
            return BadRequest("The Token has expired.");
        }

        try
        {
            UserDto user = await database.users.FirstOrDefaultAsync(record => record.email == request.user_email);
            if (user == null){
                database.passwordRecoveryTokens.Remove(request);
                await database.SaveChangesAsync();
                return BadRequest("User Does Not Exist.");
            }

            user.password = confirmpassword;
            database.passwordRecoveryTokens.Remove(request);
            await database.SaveChangesAsync();
        }
        catch (Exception)
        {
            return StatusCode(500, "An Error occured in the server. Try Again Later.");
        }

        return Accepted("Your password was changed successfully! You may return to the homepage of PostItter.");
    }

    
    [HttpPost("requestChange")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> requestPasswordChange([FromBody] string email)
    {
        string generatedToken = "";
        try
        {
            string chars = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random rnd = new Random();
            for (int i = 0; i < 12; i++)
                generatedToken += chars[rnd.Next(0, chars.Length)];

            await database.passwordRecoveryTokens.AddAsync(new PasswordRecoveryTokenDto
                {
                    user_email = email,
                    expiring_at = DateTime.Now.AddMinutes(30),
                    token = generatedToken
                }
            );
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error. Try Again Later.");
        }

        try
        {
            SendPasswordResetEmail(email, "https://localhost:5001/passwordRecovery?token=" + generatedToken);
        }
        catch (Exception e)
        {
            string errorMessage = "An Error Occured While Sending The Email. Try Again Later. " + e.Message;
            return StatusCode(500, errorMessage);
        }

        try
        {
            await database.SaveChangesAsync();
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occured in the server while setting up your token.");
        }
        
        return Created();
    }

    private void SendPasswordResetEmail(string toEmail, string resetLink)
    {
        var fromAddress = new MailAddress("ovasic@chilesotti.it", "PostItter Dev Team");
        var toAddress = new MailAddress(toEmail);
        const string fromPassword = "ognjenvasic10";
        const string subject = "PostItter Password Reset Request";
        string body = $"Dear User,\n" +
                      $"We have received recently a request from you to reset your password on your PostItter Account.\n" +
                      $"Don't Worry! You will soon be back into the action!\n" +
                      $"Click the following link to reset your password: {resetLink}" + 
                      $"\n" +
                      $"\n" +
                      $"Note that the link will expire in 30 minutes from when this email was sent.\n" + 
                      $"Best Regards,\n" +
                      $"The PostItter Dev Team" +
                      $"\n";

        var smtp = new SmtpClient
        {
            Host = "smtp.gmail.com",
            Port = 587,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, fromPassword),
            Timeout = 20000
        };
        using (var message = new MailMessage(fromAddress, toAddress)
               {
                   Subject = subject,
                   Body = body
               })
        {
            smtp.Send(message);
        }
    }
    
}