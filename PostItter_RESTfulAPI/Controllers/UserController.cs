using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostItter_RESTfulAPI.DatabaseContext;
using PostItter_RESTfulAPI.Models.DatabaseModels;
using PostItter_RESTfulAPI.Models;

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
        
        if (await database.blockedUsers.FirstOrDefaultAsync(record => record.user == id_currentUser && record.blocked_user == numeric_id) != null)
            return StatusCode(406, "Request Not Acceptable. Requested User is Blocked.");
        
        UserDto searchedUser = await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id);
        
        if (searchedUser == null)
            return NotFound();
        
        User returnedUser = new User();
        returnedUser.id = searchedUser.user_id.ToString();
        returnedUser.bio = searchedUser.bio;

        BlockedUserDTO[] blockedUsers = await database.blockedUsers.Where(record => record.user == searchedUser.user_id).ToArrayAsync();
        returnedUser.blockedUsers = new User[blockedUsers.Length];
        for (int i = 0; i < blockedUsers.Length; i++)
        {
            UserDto theBlockedGuy = await database.users.FirstOrDefaultAsync(record => record.user_id == blockedUsers[i].blocked_user);
            returnedUser.blockedUsers[i] = new User
            {
                id = theBlockedGuy.user_id.ToString(), 
                profilePicture = theBlockedGuy.profilePicture,
                displayName = theBlockedGuy.displayname, 
                username = theBlockedGuy.username
            };
        }
        
        returnedUser.darkMode = searchedUser.darkMode;
        returnedUser.displayName = searchedUser.displayname;
        returnedUser.username = searchedUser.username;
        returnedUser.email = searchedUser.email;
        returnedUser.everyoneCanText = searchedUser.everyoneCanText;

        UserConnectionDto[] _following = await database.connections.Where(record => record.user == searchedUser.user_id).ToArrayAsync();
        UserConnectionDto[] _followers = await database.connections.Where(record => record.following_user == searchedUser.user_id).ToArrayAsync();
        returnedUser.following = new User[_following.Length];
        returnedUser.followers = new User[_followers.Length];
        for (int i = 0; i < _following.Length; i++)
        {
            UserDto interestedUser = await database.users.FirstOrDefaultAsync(record => record.user_id == _following[i].following_user);
            returnedUser.following[i] = new User
            {
                id = interestedUser.user_id.ToString(), 
                profilePicture = interestedUser.profilePicture,
                displayName = interestedUser.displayname, 
                username = interestedUser.username
            };
        }
        for (int i = 0; i < _followers.Length; i++)
        {
            UserDto interestedUser = await database.users.FirstOrDefaultAsync(record => record.user_id == _followers[i].following_user);
            returnedUser.followers[i] = new User
            {
                id = interestedUser.user_id.ToString(), 
                profilePicture = interestedUser.profilePicture,
                displayName = interestedUser.displayname, 
                username = interestedUser.username
            };
        }
        
        List<Post> commentedPosts = new List<Post>();
        List<Post> likedPosts = new List<Post>();
        
        NotificationDto[] notifications = await database.notifications.Where(record => record.user_receiver == searchedUser.user_id).ToArrayAsync();
        returnedUser.notifications = new Notification[notifications.Length];
        for (int i = 0; i < notifications.Length; i++)
        {
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
        
        PostDto[] dbPosts = await database.posts.Where(post => post.user_id == numeric_id).ToArrayAsync();
        Post[] posts = new Post[dbPosts.Length];
        for (int i = 0; i < dbPosts.Length; i++)
        {
            posts[i] = new Post();
            
            posts[i].body = dbPosts[i].body;
            
            CommentDto[] comments = await database.comments
                .Where(record => record.user == searchedUser.user_id && record.post == dbPosts[i].post_id).ToArrayAsync();
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
            
            HashtagDto[] hashes = await database.hashtags.Where(record => record.post_ref == dbPosts[i].post_id).ToArrayAsync();
            posts[i].hashtags = new String[hashes.Length];
            for (int j = 0; j < hashes.Length; j++)
            {
                posts[i].hashtags[j] = hashes[j].content;
            }
            posts[i].id = dbPosts[i].post_id.ToString();
            posts[i].likes = dbPosts[i].likes;
            posts[i].reposts = dbPosts[i].reposts;
            posts[i].shares = dbPosts[i].shares;
            
            posts[i].user = new User
            {
                id = searchedUser.user_id.ToString(), // dbPosts[i].user_id.ToString(); ??
                displayName = searchedUser.displayname,
                username = searchedUser.username,
                profilePicture = searchedUser.profilePicture
            };
            
            if (await database.likes.FirstOrDefaultAsync(record => record.user == searchedUser.user_id && record.post == dbPosts[i].post_id) != null)
                likedPosts.Add(posts[i]);
            if (await database.comments.FirstOrDefaultAsync(record => record.user == searchedUser.user_id && record.post == dbPosts[i].post_id) != null)
                commentedPosts.Add(posts[i]);
        }
        returnedUser.posts = posts;
        returnedUser.likedPosts = likedPosts.ToArray();
        returnedUser.commentedPosts = commentedPosts.ToArray();
        
        returnedUser.privateProfile = searchedUser.privateProfile;
        returnedUser.profilePicture = searchedUser.profilePicture;
        return Ok(returnedUser);
    }

    [HttpGet("{id}/followers")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<User[]>> getFollowersFromUser(string id)
    {
        if (!long.TryParse(id, out long numeric_id))
            return BadRequest("Invalid user ID");

        if (await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id) == null)
            return NotFound("User Does Not Exist.");
        
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
        }

        return Ok(followers);
    }
    
    [HttpGet("{id}/following")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<User[]>> getFollowingFromUser(string id)
    {
        if (!long.TryParse(id, out long numeric_id))
            return BadRequest("Invalid user ID");

        if (await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id) == null)
            return NotFound("User Does Not Exist.");
        
        UserConnectionDto[] connections = await database.connections.Where(record => record.user == numeric_id).ToArrayAsync();
        User[] following = new User[connections.Length];
        for (int i = 0; i < connections.Length; i++)
        {
            UserDto current = await database.users.FirstOrDefaultAsync(record => record.user_id == connections[i].user);
            following[i] = new User
            {
                id = current.user_id.ToString(),
                profilePicture = current.profilePicture,
                displayName = current.displayname,
                username = current.username
            };
        }

        return Ok(following);
    }

    /**     USELESS!! I will keep the method commented here, however, it's LoginController's job to add new users to the database with standard data!
    [HttpPost("{id}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult addNewUser(string id,[FromBody] User newUser)
    {
        UserDto newDbUser = new UserDto
        {
            bio = newUser.bio,
            darkMode = (bool)newUser.darkMode,
            displayname = newUser.displayName,
            username = newUser.username,
            email = newUser.email,
            everyoneCanText = (bool)newUser.everyoneCanText,
            privateProfile = (bool)newUser.privateProfile,
            profilePicture = newUser.profilePicture,
        };
        
        try
        {
            database.users.Add(newDbUser);
            database.SaveChanges();
            return Created();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }
    */
    
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> updateExistingUser(string id,[FromBody] User newData)
    {
        if (!long.TryParse(id, out long numeric_id))
            return BadRequest("Invalid user ID");
        
        UserDto userToUpdate = await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id);
        if (userToUpdate == null)
            return NotFound();

        if (newData.bio != userToUpdate.bio)
            userToUpdate.bio = newData.bio;
        if (newData.darkMode != userToUpdate.darkMode)
            userToUpdate.darkMode = (bool) newData.darkMode;
        if (newData.displayName != userToUpdate.displayname)
            userToUpdate.displayname = newData.displayName;
        if (newData.username != userToUpdate.username)
            userToUpdate.username = newData.username;
        if (newData.email != userToUpdate.email)
            userToUpdate.email = newData.email;
        if (newData.everyoneCanText != userToUpdate.everyoneCanText)
            userToUpdate.everyoneCanText = (bool) newData.everyoneCanText;
        if (newData.privateProfile != userToUpdate.privateProfile)
            userToUpdate.privateProfile = (bool) newData.privateProfile;
        if (newData.profilePicture != userToUpdate.profilePicture)
            userToUpdate.profilePicture = newData.profilePicture;
        userToUpdate.user_id = numeric_id;
        
        try
        {
            await database.SaveChangesAsync();
            return Accepted();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPost("{id}/block")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> blockUser(string id, [FromBody] string userToBlock)
    {
        if (!long.TryParse(id, out long numeric_id) || !long.TryParse(userToBlock, out long numeric_id_userToBlock))
            return BadRequest("Invalid user ID.");
        
        if (await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id_userToBlock) == null)
            return NotFound();

        BlockedUserDTO blockedUserDto = new BlockedUserDTO
        {
            blocked_user = numeric_id_userToBlock,
            user = numeric_id
        };

        try
        {
            var task = database.blockedUsers.AddAsync(blockedUserDto);
            
            UserConnectionDto connection = await database.connections.FirstOrDefaultAsync(record => record.following_user == numeric_id_userToBlock && record.user == numeric_id);
            if (connection != null)
                database.connections.Remove(connection);

            await task;
            await database.SaveChangesAsync();
            return Ok();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPost("{id}/unblock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> unblockUser(string id, [FromBody] string userToUnblock)
    {
        if (!long.TryParse(id, out long numeric_id) || !long.TryParse(userToUnblock, out long numeric_id_userToUnblock))
            return BadRequest("Invalid user ID.");
        
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> deleteUser(string id)
    {
        if (!long.TryParse(id, out long numeric_id))
            return BadRequest("Invalid user ID");
        
        UserDto cancelledUser = await database.users.FirstOrDefaultAsync(record => record.user_id == numeric_id);
        if (cancelledUser == null)
            return NotFound("User Does Not Exist");
        
        database.users.Remove(cancelledUser);
        await database.SaveChangesAsync();
        return NoContent();
    }
}