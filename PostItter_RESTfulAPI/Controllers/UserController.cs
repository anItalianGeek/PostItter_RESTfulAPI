using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using PostItter_RESTfulAPI.DatabaseContext;
using PostItter_RESTfulAPI.Entity;
using PostItter_RESTfulAPI.Entity.DatabaseModels;

namespace PostItter_RESTfulAPI.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext database;
    
    public UserController(ApplicationDbContext _database)
    {
        database = _database;
    }

    [HttpGet("darkmodeStatus")]
    public async Task<IActionResult> GetDarkModeStatus([FromQuery] string user)
    {
        long id = long.Parse(user);
        UserSettingsDto settings = await database.settings.FirstOrDefaultAsync(record => record.user == id);
        if (settings == null)
            return NotFound("Database is lacking settings for the requested user.");
        else
            return Ok(settings.darkMode);
    }
    
    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status406NotAcceptable)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<User>> getUserById(string id,[FromQuery] string currentUser)
    {
        if (!long.TryParse(currentUser, out long id_currentUser))
            return BadRequest("Invalid user ID");

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

            long currentUserJwt = Convert.ToInt64(jwtWebToken.sub);
            ActiveUsersDto activeUser =
                await database.activeUsers.FirstOrDefaultAsync(record => record.user_ref == currentUserJwt);

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
        
        if (await database.blockedUsers.FirstOrDefaultAsync(record => record.user == id_currentUser && record.blocked_user == numeric_id) != null)
            return StatusCode(406, "Request Not Acceptable. Requested User is Blocked.");
        
        if (await database.blockedUsers.FirstOrDefaultAsync(record => record.blocked_user == id_currentUser && record.user == numeric_id) != null)
            return StatusCode(406, "Request Not Acceptable. The requested user has blocked you.");
        
        UserDto searchedUser = await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id);
        UserSettingsDto userSettings = await database.settings.FirstOrDefaultAsync(record => record.user == searchedUser.user_id);
        
        if (searchedUser == null || userSettings == null)
            return NotFound("Requested user does not exist.");
        
        User returnedUser = new User();
        returnedUser.id = searchedUser.user_id.ToString();
        returnedUser.bio = searchedUser.bio;
        returnedUser.displayName = searchedUser.displayname;
        returnedUser.username = searchedUser.username;
        returnedUser.profilePicture = searchedUser.profilePicture;
        
        if (numeric_id == id_currentUser)
        {
            BlockedUserDTO[] blockedUsers = await database.blockedUsers
                .Where(record => record.user == searchedUser.user_id).ToArrayAsync();
            returnedUser.blockedUsers = new User[blockedUsers.Length];
            for (int i = 0; i < blockedUsers.Length; i++)
            {
                UserDto theBlockedGuy =
                    await database.users.FirstOrDefaultAsync(record => record.user_id == blockedUsers[i].blocked_user);
                returnedUser.blockedUsers[i] = new User
                {
                    id = theBlockedGuy.user_id.ToString(),
                    profilePicture = theBlockedGuy.profilePicture,
                    displayName = theBlockedGuy.displayname,
                    username = theBlockedGuy.username
                };
            }
            
            returnedUser.email = searchedUser.email;
            returnedUser.darkMode = userSettings.darkMode;
            returnedUser.twoFA = userSettings.twoFA;
            returnedUser.likeNotification = userSettings.likeNotification;
            returnedUser.commentNotification = userSettings.commentNotification;
            returnedUser.replyNotification = userSettings.replyNotification;
            returnedUser.followNotification = userSettings.followNotification;
            returnedUser.messageNotification = userSettings.messageNotification;
            
            NotificationDto[] notifications = await database.notifications.Where(record => record.user_receiver == searchedUser.user_id).ToArrayAsync();
            returnedUser.notifications = new Notification[notifications.Length];
            for (int i = 0; i < notifications.Length; i++)
            {
                returnedUser.notifications[i] = new Notification();
                returnedUser.notifications[i].id = notifications[i].notification_id.ToString();
                returnedUser.notifications[i].type = notifications[i].type;
                returnedUser.notifications[i].message = notifications[i].content;
                returnedUser.notifications[i].postId = notifications[i].post_ref.ToString();
                UserDto sender = await database.users.FirstOrDefaultAsync(record => record.user_id == notifications[i].user_sender);
                returnedUser.notifications[i].user = new User
                {
                    id = sender.user_id.ToString(),
                    displayName = sender.displayname,
                    username = sender.username,
                    profilePicture = sender.profilePicture
                };
            }
        }
        else
        {
            returnedUser.blockedUsers = [];
            returnedUser.notifications = [];
            returnedUser.email = null;
            returnedUser.darkMode = null;
            returnedUser.twoFA = null;
            returnedUser.likeNotification = null;
            returnedUser.commentNotification = null;
            returnedUser.replyNotification = null;
            returnedUser.followNotification = null;
            returnedUser.messageNotification = null;
        }
        
        returnedUser.everyoneCanText = userSettings.everyoneCanText;
        returnedUser.privateProfile = userSettings.privateProfile;
        
        List<UserConnectionDto> _following = await database.connections.Where(record => record.user == searchedUser.user_id).ToListAsync();
        List<UserConnectionDto> _followers = await database.connections.Where(record => record.following_user == searchedUser.user_id).ToListAsync();
        List<PostDto> dbPosts = await database.posts.Where(post => post.user_id == searchedUser.user_id).ToListAsync();
        List<long> likedPostsIds = await database.likes.Where(like => like.user == searchedUser.user_id).Select(e => e.post).ToListAsync();
        List<long> commentedPostsIds = await database.comments.Where(comment => comment.user == searchedUser.user_id).Select(e => e.post).Distinct().ToListAsync();
        
        Post[] posts = new Post[dbPosts.Count];
        returnedUser.following = new User[_following.Count];
        returnedUser.followers = new User[_followers.Count];
        
        if ((!userSettings.privateProfile || userSettings.privateProfile &&
            _followers.FirstOrDefault(e => e.following_user == id_currentUser) != null) || id_currentUser == numeric_id)
        {
            for (int i = 0; i < _following.Count; i++)
            {
                UserDto interestedUser =
                    await database.users.FirstOrDefaultAsync(record => record.user_id == _following[i].following_user);
                returnedUser.following[i] = new User
                {
                    id = interestedUser.user_id.ToString(),
                    profilePicture = interestedUser.profilePicture,
                    displayName = interestedUser.displayname,
                    username = interestedUser.username
                };
            }

            for (int i = 0; i < _followers.Count; i++)
            {
                UserDto interestedUser =
                    await database.users.FirstOrDefaultAsync(record => record.user_id == _followers[i].user);
                returnedUser.followers[i] = new User
                {
                    id = interestedUser.user_id.ToString(),
                    profilePicture = interestedUser.profilePicture,
                    displayName = interestedUser.displayname,
                    username = interestedUser.username
                };
            }
            
            for (int i = 0; i < dbPosts.Count; i++)
            {
                posts[i] = new Post();

                posts[i].body = dbPosts[i].body;

                CommentDto[] comments = await database.comments
                    .Where(record => record.user == searchedUser.user_id && record.post == dbPosts[i].post_id)
                    .ToArrayAsync();
                posts[i].comments = new Comment[comments.Length];
                for (int j = 0; j < comments.Length; j++)
                {
                    posts[i].comments[j] = new Comment();
                    posts[i].comments[j].content = comments[j].content;
                    posts[i].comments[j].user = new User
                    {
                        id = searchedUser.user_id.ToString(),
                        displayName = searchedUser.displayname,
                        username = searchedUser.username,
                        profilePicture = searchedUser.profilePicture
                    };
                }
                
                posts[i].hashtags = await database.hashtags.Where(record => record.post_ref == dbPosts[i].post_id).Select(e => e.content).ToArrayAsync();
                posts[i].id = dbPosts[i].post_id.ToString();
                posts[i].likes = dbPosts[i].likes;
                posts[i].reposts = dbPosts[i].reposts;
                posts[i].shares = dbPosts[i].shares;

                posts[i].user = new User
                {
                    id = searchedUser.user_id.ToString(),
                    displayName = searchedUser.displayname,
                    username = searchedUser.username,
                    profilePicture = searchedUser.profilePicture
                };
            }
            
            List<Post> commentedPosts = new List<Post>();
            List<Post> likedPosts = new List<Post>();
            foreach (long postId in likedPostsIds)
            {
                PostDto post = await database.posts.FirstOrDefaultAsync(record => record.post_id == postId);
                UserDto postingUser = await database.users.FirstOrDefaultAsync(record => record.user_id == post.user_id);
                likedPosts.Add(new Post
                {
                    id = post.post_id.ToString(),
                    body = post.body,
                    color = post.color,
                    comments = [],
                    hashtags = await database.hashtags.Where(record => record.post_ref == post.post_id).Select(e => e.content).ToArrayAsync(),
                    likes = post.likes,
                    reposts = post.reposts,
                    shares = post.shares,
                    user = new User
                    {
                        id = postingUser.user_id.ToString(),
                        displayName = postingUser.displayname,
                        username = postingUser.username,
                        profilePicture = postingUser.profilePicture
                    }
                });
            }

            foreach (long postId in commentedPostsIds)
            {
                PostDto post = await database.posts.FirstOrDefaultAsync(record => record.post_id == postId);
                UserDto postingUser = await database.users.FirstOrDefaultAsync(record => record.user_id == post.user_id);
                commentedPosts.Add(new Post
                {
                    id = post.post_id.ToString(),
                    body = post.body,
                    color = post.color,
                    comments = [],
                    hashtags = await database.hashtags.Where(record => record.post_ref == post.post_id).Select(e => e.content).ToArrayAsync(),
                    likes = post.likes,
                    reposts = post.reposts,
                    shares = post.shares,
                    user = new User
                    {
                        id = postingUser.user_id.ToString(),
                        displayName = postingUser.displayname,
                        username = postingUser.username,
                        profilePicture = postingUser.profilePicture
                    }
                });
            }
            
            returnedUser.likedPosts = likedPosts.ToArray();
            returnedUser.commentedPosts = commentedPosts.ToArray();
        }
        else
        {
            returnedUser.likedPosts = [];
            returnedUser.commentedPosts = [];
        }
        
        returnedUser.posts = posts;
        return Ok(returnedUser);
    }

    [HttpGet("{id}/followers")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<User[]>> getFollowersFromUser(string id, [FromQuery] string id_current_user)
    {
        if (!long.TryParse(id, out long numeric_id))
            return BadRequest("Invalid user ID");
        if (!long.TryParse(id_current_user, out long currentUser))
            return BadRequest("Invalid current user ID");

        if (!await database.users.AnyAsync(record => record.user_id == numeric_id))
            return NotFound("User Does Not Exist.");

        UserSettingsDto searchedUserSettings;
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

            long currentUserJwt = Convert.ToInt64(jwtWebToken.sub);
            ActiveUsersDto activeUser =
                await database.activeUsers.FirstOrDefaultAsync(record => record.user_ref == currentUserJwt);

            if (activeUser == null)
                return StatusCode(401,
                    "Cannot perform action, user might not be signed up or authorization token might be corrupted.");
            
            if (check != activeUser.encodedToken)
                return StatusCode(401, "Cannot perform action, Authorization Token might be corrupted.");
            
            if (await database.blockedUsers.FirstOrDefaultAsync(record => record.user == currentUserJwt && record.blocked_user == numeric_id) != null)
                return StatusCode(406, "Request Not Acceptable. Requested User is Blocked.");
        
            if (await database.blockedUsers.FirstOrDefaultAsync(record => record.blocked_user == currentUserJwt && record.user == numeric_id) != null)
                return StatusCode(406, "Request Not Acceptable. The requested user has blocked you.");
        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }*/

        if ((searchedUserSettings = await database.settings.FirstOrDefaultAsync(record => record.user == numeric_id)) == null)
            return StatusCode(500, "Server is missing settings for this user.");

        bool isCurrentUserBeingFollowed = false;
        UserConnectionDto[] connections = await database.connections.Where(record => record.following_user == numeric_id).ToArrayAsync();
        User[] followers = new User[connections.Length];
        for (int i = 0; i < connections.Length; i++)
        {
            UserDto current = await database.users.FirstOrDefaultAsync(record => record.user_id == connections[i].user);
            followers[i] = new User
            {
                id = current.user_id.ToString(),
                profilePicture = current.profilePicture,
                displayName = current.displayname,
                username = current.username
            };
            
            if (current.user_id == numeric_id)
                isCurrentUserBeingFollowed = true;    
        }


        if (currentUser == searchedUserSettings.user || (!searchedUserSettings.privateProfile || searchedUserSettings.privateProfile && isCurrentUserBeingFollowed))
            return Ok(followers);
        else
            return Unauthorized("You do not have the permission to view this user's followers");
    }
    
    [HttpGet("{id}/following")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<User[]>> getFollowingFromUser(string id, [FromQuery] string id_current_user)
    {
        if (!long.TryParse(id, out long numeric_id))
            return BadRequest("Invalid user ID");
        if (!long.TryParse(id_current_user, out long currentUser))
            return BadRequest("Invalid current user ID");
        
        if (!await database.users.AnyAsync(record => record.user_id == numeric_id))
            return NotFound("User Does Not Exist.");
        
        UserSettingsDto searchedUserSettings;
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

            long currentUserJwt = Convert.ToInt64(jwtWebToken.sub);
            ActiveUsersDto activeUser =
                await database.activeUsers.FirstOrDefaultAsync(record => record.user_ref == currentUserJwt);

            if (activeUser == null)
                return StatusCode(401,
                    "Cannot perform action, user might not be signed up or authorization token might be corrupted.");
            
            if (check != activeUser.encodedToken)
                return StatusCode(401, "Cannot perform action, Authorization Token might be corrupted.");
            
            if (await database.blockedUsers.FirstOrDefaultAsync(record => record.user == currentUserJwt && record.blocked_user == numeric_id) != null)
                return StatusCode(406, "Request Not Acceptable. Requested User is Blocked.");
        
            if (await database.blockedUsers.FirstOrDefaultAsync(record => record.blocked_user == currentUserJwt && record.user == numeric_id) != null)
                return StatusCode(406, "Request Not Acceptable. The requested user has blocked you.");
        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }*/
        
        if ((searchedUserSettings = await database.settings.FirstOrDefaultAsync(record => record.user == numeric_id)) == null)
            return StatusCode(500, "Server is missing settings for this user.");

        
        UserConnectionDto[] connections = await database.connections.Where(record => record.user == numeric_id).ToArrayAsync();
        User[] following = new User[connections.Length];
        for (int i = 0; i < connections.Length; i++)
        {
            UserDto current = await database.users.FirstOrDefaultAsync(record => record.user_id == connections[i].following_user);
            following[i] = new User
            {
                id = current.user_id.ToString(),
                profilePicture = current.profilePicture,
                displayName = current.displayname,
                username = current.username
            };
        }

        if (currentUser == searchedUserSettings.user || (!searchedUserSettings.privateProfile || searchedUserSettings.privateProfile &&
            !await database.connections.AnyAsync(record =>
                record.following_user == searchedUserSettings.user && record.user == numeric_id)))
            return Ok(following);
        else
            return Unauthorized("You do not have the permission to view this user's followings");
    }

    [HttpPost("changePassword")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> updatePassword([FromBody] string newPassword, [FromQuery] string id_retrieving_user)
    {
        if (!long.TryParse(id_retrieving_user, out long numeric_id))
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
            (await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id))
                .password = newPassword;
            await database.SaveChangesAsync();
            return Ok("Password updated.");
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error.");
        }
    }
    
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status307TemporaryRedirect)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> updateExistingUser(string id, [FromBody] User newData)
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

            if (numeric_id != currentUser)
                return StatusCode(406, "Token User ID and User ID didn't match.");
        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }*/
        
        UserDto userToUpdate = await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id);
        UserSettingsDto userSettings = await database.settings.FirstOrDefaultAsync(record => record.user == userToUpdate.user_id);
        
        if (userToUpdate == null || userSettings == null)
            return NotFound("Requested User Does Not Exist.");

        bool twoFaChanged = false;
        if (newData.bio != userToUpdate.bio)
            userToUpdate.bio = newData.bio;
        if (newData.displayName != userToUpdate.displayname)
            userToUpdate.displayname = newData.displayName;
        if (newData.username != userToUpdate.username)
            userToUpdate.username = newData.username;
        if (newData.email != userToUpdate.email)
            userToUpdate.email = newData.email;
        if (newData.profilePicture != userToUpdate.profilePicture)
            userToUpdate.profilePicture = newData.profilePicture;
        userToUpdate.user_id = numeric_id;
        
        if (newData.everyoneCanText != userSettings.everyoneCanText)
            userSettings.everyoneCanText = (bool) newData.everyoneCanText;
        if (newData.privateProfile != userSettings.privateProfile)
            userSettings.privateProfile = (bool) newData.privateProfile;
        if (newData.darkMode != userSettings.darkMode)
            userSettings.darkMode = (bool) newData.darkMode;
        if (newData.twoFA != userSettings.twoFA)
        {
            twoFaChanged = true;
        }

        if (newData.likeNotification != userSettings.likeNotification)
            userSettings.likeNotification = (bool)newData.likeNotification;
        if (newData.commentNotification != userSettings.commentNotification)
            userSettings.commentNotification = (bool)newData.commentNotification;
        if (newData.replyNotification != userSettings.replyNotification)
            userSettings.replyNotification = (bool)newData.replyNotification;
        if (newData.followNotification != userSettings.followNotification)
            userSettings.followNotification = (bool)newData.followNotification;
        if (newData.messageNotification != userSettings.messageNotification)
            userSettings.messageNotification = (bool)newData.messageNotification;
        
        try
        {
            await database.SaveChangesAsync();
            if (twoFaChanged)
                if ((bool)newData.twoFA)
                {
                    Response.Headers.Location = $"http://localhost:5265/api/2fa/activate?id_active_user={numeric_id}";
                    return StatusCode(307);
                }
                else
                {
                    Response.Headers.Location = $"http://localhost:5265/api/2fa/deactivate?id_active_user={numeric_id}";
                    return StatusCode(307);
                }
            else
                return Accepted();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPost("{id}/block/{userToBlock}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> blockUser(string id, string userToBlock)
    {
        if (!long.TryParse(id, out long numeric_id) || !long.TryParse(userToBlock, out long numeric_id_userToBlock))
            return BadRequest("Invalid user ID.");
        
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
            
            if (await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id_userToBlock) == null)
                return NotFound();
        }
        else
        {
            return StatusCode(401, "Missing Authorization Token.");
        }*/
        
        BlockedUserDTO blockedUserDto = new BlockedUserDTO
        {
            blocked_user = numeric_id_userToBlock,
            user = numeric_id
        };

        try
        {
            await database.blockedUsers.AddAsync(blockedUserDto);
            
            UserConnectionDto connection = await database.connections.FirstOrDefaultAsync(record => record.following_user == numeric_id_userToBlock && record.user == numeric_id);
            if (connection != null)
                database.connections.Remove(connection);

            var userChats = database.chats.Where(record => record.member_id == numeric_id).Select(e => e.chat_id).Distinct();
            List<ChatDto> chatsToDelete = new List<ChatDto>();
            foreach (long chatId in userChats)
            {
                ChatDto chat = await database.chats.FirstOrDefaultAsync(record => record.chat_id == chatId);
                if (chat.member_id == numeric_id_userToBlock)
                    chatsToDelete.Add(chat);
            }
                
            database.RemoveRange(chatsToDelete);
                
            await database.SaveChangesAsync();
            return Ok();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPost("{id}/follow/{followerId}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> followUser(string id, string followerId, [FromBody] Notification? notification)
    {
        if (!long.TryParse(id, out long numeric_id) || !long.TryParse(followerId, out long follower_numeric_id))
            return BadRequest("Invalid user ID.");

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
            UserSettingsDto requestedUserSettings = await database.settings.FirstOrDefaultAsync(record => record.user == follower_numeric_id);
            if (requestedUserSettings == null)
                return BadRequest("Cannot perform action, The user doesn't exist or the database is missing it's settings.");

            if (requestedUserSettings.privateProfile)
            {
                if (notification == null)
                    return Unauthorized("Cannot follow a user whose profile is private without their authorization.");
                else if (await database.notifications.FirstOrDefaultAsync(record =>
                             record.type == "request-follow" && record.user_receiver == follower_numeric_id &&
                             record.user_sender == numeric_id) == null)
                    return BadRequest("Follow request confirmation didn't match to the user.");
            }
            
            await database.connections.AddAsync(new UserConnectionDto
            {
                user = numeric_id,
                following_user = follower_numeric_id,
            });
            await database.SaveChangesAsync();
            return Created();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }
    
    [HttpDelete("{id}/unfollow/{followerId}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> unfollowUser(string id, string followerId)
    {
        if (!long.TryParse(id, out long numeric_id) || !long.TryParse(followerId, out long follower_numeric_id))
            return BadRequest("Invalid user ID.");

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
            UserConnectionDto connection = await database.connections.FirstOrDefaultAsync(record => record.user == numeric_id && record.following_user == follower_numeric_id);
            if (connection != null)
                database.connections.Remove(connection);
            else
                return NotFound("Connection between users not found.");
            
            await database.SaveChangesAsync();
            return Created();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }
    
    [HttpPost("{id}/unblock/{userToUnblock}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> unblockUser(string id, string userToUnblock)
    {
        if (!long.TryParse(id, out long numeric_id) || !long.TryParse(userToUnblock, out long numeric_id_userToUnblock))
            return BadRequest("Invalid user ID.");
        
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
        
        if (await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id_userToUnblock) == null)
            return NotFound();

        BlockedUserDTO unblockedUserDto = await database.blockedUsers.FirstOrDefaultAsync(record => 
            record.user == numeric_id && record.blocked_user == numeric_id_userToUnblock
        );
        try
        {
            database.blockedUsers.Remove(unblockedUserDto);
            await database.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }
    
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> deleteUser(string id)
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
            UserDto cancelledUser = await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id);
            if (cancelledUser == null)
                return NotFound("User Does Not Exist");

            database.users.Remove(cancelledUser);
            await database.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error.");
        }
    }
}