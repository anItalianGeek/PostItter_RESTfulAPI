using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PostItter_RESTfulAPI.DatabaseContext;
using PostItter_RESTfulAPI.Models.DatabaseModels;
using Google.Authenticator;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Primitives;
using PostItter_RESTfulAPI.Models;

namespace PostItter_RESTfulAPI.Controllers;

[ApiController]
[Route("api/2fa")]
public class TwoFactorAuthenticationController : ControllerBase
{
    private readonly ApplicationDbContext database;

    public TwoFactorAuthenticationController(ApplicationDbContext db)
    {
        database = db;
    }

    [HttpPost("authenticate")]
    public async Task<IActionResult> authenticate([FromQuery] string email_active_user, [FromBody] string code)
    {
        if (email_active_user.IsNullOrEmpty())
            return BadRequest("Identifier has not been provided.");
        if (code.IsNullOrEmpty())
            return BadRequest("Authenticator code has not been provided.");
        
        UserDto user = await database.users.FirstOrDefaultAsync(record => record.email == email_active_user);
        
        TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
        if (tfa.ValidateTwoFactorPIN(user.secureKey2fa, code))
        {
            string s_signature = "";
            string chars =
                @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!£$%&/()=?^*§°_:;><-|\{}[]#@.,'";
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
            catch (Exception)
            {
                return StatusCode(500, "An Internal Server Error has occurred. Login failed. Try Again later.");
            }

            return Ok(newToken);
        }
        else
        {
            return Unauthorized("Failed To Authenticate. Wrong Code inserted.");
        }
    }

    [HttpPut("activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> activate2fa([FromQuery] string id_active_user)
    {
        if (!long.TryParse(id_active_user, out long numeric_id))
            return BadRequest("Invalid User ID");
        
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

            long id_currentUser = Convert.ToInt64(jwtWebToken.sub);
            ActiveUsersDto activeUser =
                await database.activeUsers.FirstOrDefaultAsync(record => record.user_ref == id_currentUser);

            if (activeUser == null)
                return StatusCode(401,
                    "Cannot perform action, user might not be signed up or authorization token might be corrupted.");
            
            if (check != activeUser.encodedToken)
                return StatusCode(401, "Cannot perform action, Authorization Token might be corrupted.");
            
            if (numeric_id != id_currentUser)
                return BadRequest("User ID and Token ID didn't match.");
        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }*/
        
        UserDto currentUser = await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id);
        if (currentUser == null)
            return NotFound("Requested user does not exist.");
        
        UserSettingsDto settings = await database.settings.FirstOrDefaultAsync(record => record.user == numeric_id);
        if (settings == null)
            return NotFound("Database is missing settings for this user.");

        if (settings.twoFA)
            return BadRequest("Two-factor authentication is enabled for this user.");
        
        if (currentUser.secureKey2fa.IsNullOrEmpty())
        {
            string key = GenerateRandomKey(10);
            currentUser.secureKey2fa = key;
            TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
            SetupCode setupInfo = tfa.GenerateSetupCode("PostItter TOTP Generator", currentUser.email, key, false, 3);
            
            string qrCodeImageBase64 = setupInfo.QrCodeSetupImageUrl.Split(',')[1];

            // Crea la stringa HTML
            string html = $@"
                            <html>
                                <body>
                                    <h2>Scan the QR Code with your authenticator app</h2>
                                    <img src='data:image/png;base64,{qrCodeImageBase64}' alt='QR Code' />
                                    <p>Your secret key: {key}</p>
                                    <p><small>
                                    You have been redirected here because you wanted to enable Two Factor Authentication.
                                    </small></p>
                                </body>
                            </html>";

            settings.twoFA = true;
            try
            {
                await database.SaveChangesAsync();
                return Ok(new {content = html});
            }
            catch (Exception e)
            {
                return StatusCode(500, $"Internal Server Error. {e.Message}");
            }
        }
        else
        {
            settings.twoFA = true;
        }

        try
        {
            await database.SaveChangesAsync();
            return Ok("Two-factor authentication activation successful.");
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPut("deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> deactivate2fa([FromQuery] string id_active_user)
    {
        if (!long.TryParse(id_active_user, out long id))
            return BadRequest("Invalid User ID");
        
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

            long id_currentUser = Convert.ToInt64(jwtWebToken.sub);
            ActiveUsersDto activeUser =
                await database.activeUsers.FirstOrDefaultAsync(record => record.user_ref == id_currentUser);

            if (activeUser == null)
                return StatusCode(401,
                    "Cannot perform action, user might not be signed up or authorization token might be corrupted.");
            
            if (check != activeUser.encodedToken)
                return StatusCode(401, "Cannot perform action, Authorization Token might be corrupted.");
            
            if (id != id_currentUser)
                return BadRequest("User ID and Token ID didn't match.");
        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }*/
        
        UserSettingsDto userSettings = await database.settings.FirstOrDefaultAsync(record => record.user == id);
        if (userSettings == null)
            return NotFound("Requested user does not exist.");
        
        userSettings.twoFA = false;
        
        try
        {
            await database.SaveChangesAsync();
            return Ok();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> remove2fa([FromQuery] string id_active_user)
    {
        if (!long.TryParse(id_active_user, out long id))
            return BadRequest("Invalid User ID");
        
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

            long id_currentUser = Convert.ToInt64(jwtWebToken.sub);
            ActiveUsersDto activeUser =
                await database.activeUsers.FirstOrDefaultAsync(record => record.user_ref == id_currentUser);

            if (activeUser == null)
                return StatusCode(401,
                    "Cannot perform action, user might not be signed up or authorization token might be corrupted.");
            
            if (check != activeUser.encodedToken)
                return StatusCode(401, "Cannot perform action, Authorization Token might be corrupted.");
            
            if (id != id_currentUser)
                return BadRequest("User ID and Token ID didn't match.");
        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }*/

        UserDto user = await database.users.FirstOrDefaultAsync(record => record.user_id == id);
        UserSettingsDto settings = await database.settings.FirstOrDefaultAsync(record => record.user == id);
        if (user == null)
            return NotFound("User Does not exist");
        if (settings == null)
            return NotFound("Server is missing settings for the requested user.");

        user.secureKey2fa = "";
        settings.twoFA = false;

        try
        {
            await database.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }
    
    private string GenerateRandomKey(int length)
    {
        string allowableChars = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        
        byte[] rnd = new byte[length];
        using (var rng = new RNGCryptoServiceProvider()) rng.GetBytes(rnd);
        
        var allowable = allowableChars.ToCharArray();
        int size = allowable.Length;
        string generatedKey = "";
        for (int i = 0; i < length; i++)
            generatedKey += allowable[rnd[i] % size];
        
        return generatedKey;
    }
}