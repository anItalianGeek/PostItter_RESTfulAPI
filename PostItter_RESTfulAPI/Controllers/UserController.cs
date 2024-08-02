using Microsoft.AspNetCore.Mvc;
using PostItter_RESTfulAPI.DatabaseContext;
using PostItter_RESTfulAPI.Models.DatabaseModels;
using PostItter_RESTfulAPI.Models;

namespace PostItter_RESTfulAPI.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase // TODO - IMPORTANT FIX: MUST BUILD THE API IN A WAY WHERE USERS HAVE A UNIQUE ID
{
    private readonly ApplicationDbContext database;
    
    public UserController(ApplicationDbContext _database)
    {
        database = _database;
    }

    [HttpGet("{id:string}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status406NotAcceptable)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult getUserById(string id,[FromQuery] string currentUser)
    {
        long id_currentUser = Convert.ToInt64(currentUser);
        long numeric_id = Convert.ToInt64(id);
        if (database.blockedUsers.FirstOrDefault(record => record.user == id_currentUser && record.blocked_user == numeric_id) != null)
            return StatusCode(406, "Request Not Acceptable. Requested User is Blocked.");
        
        UserDto searchedUser = database.users.FirstOrDefault(record => record.user_id == numeric_id);
        if (searchedUser == null)
            return NotFound();
        User returnedUser = new User();
        returnedUser.id = searchedUser.user_id.ToString();
        returnedUser.bio = searchedUser.bio;

        BlockedUserDTO[] blockedUsers = database.blockedUsers.Where(record => record.user == searchedUser.user_id).ToArray();
        returnedUser.blockedUsers = new User[blockedUsers.Length];
        for (int i = 0; i < blockedUsers.Length; i++)
        {
            UserDto theBlockedGuy = database.users.FirstOrDefault(record => record.user_id == blockedUsers[i].blocked_user);
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

        UserConnectionDto[] _following = database.connections.Where(record => record.user == searchedUser.user_id).ToArray();
        UserConnectionDto[] _followers = database.connections.Where(record => record.following_user == searchedUser.user_id).ToArray();
        returnedUser.following = new User[_following.Length];
        returnedUser.followers = new User[_followers.Length];
        for (int i = 0; i < _following.Length; i++)
        {
            UserDto interestedUser = database.users.FirstOrDefault(record => record.user_id == _following[i].following_user);
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
            UserDto interestedUser = database.users.FirstOrDefault(record => record.user_id == _followers[i].following_user);
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
        
        NotificationDto[] notifications = database.notifications.Where(record => record.user_receiver == searchedUser.user_id).ToArray();
        returnedUser.notifications = new Notification[notifications.Length];
        for (int i = 0; i < notifications.Length; i++)
        {
            returnedUser.notifications[i].id = notifications[i].notification_id.ToString();
            returnedUser.notifications[i].type = notifications[i].type;
            returnedUser.notifications[i].message = notifications[i].content;
            returnedUser.notifications[i].postId = notifications[i].post_ref.ToString();
            UserDto sender = database.users.FirstOrDefault(record => record.user_id == notifications[i].user_sender);
            returnedUser.notifications[i].user = new User
            {
                id = sender.user_id.ToString(),
                displayName = sender.displayname,
                username = sender.username,
                profilePicture = sender.profilePicture
            };
        }
        
        PostDto[] dbPosts = database.posts.Where(post => post.user_id == numeric_id).ToArray();
        Post[] posts = new Post[dbPosts.Length];
        for (int i = 0; i < dbPosts.Length; i++)
        {
            posts[i] = new Post();
            
            posts[i].body = dbPosts[i].body;
            
            CommentDto[] comments = database.comments
                .Where(record => record.user == searchedUser.user_id && record.post == dbPosts[i].post_id).ToArray();
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
            
            HashtagDto[] hashes = database.hashtags.Where(record => record.post_ref == dbPosts[i].post_id).ToArray();
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
            
            if (database.likes.FirstOrDefault(record => record.user == searchedUser.user_id && record.post == dbPosts[i].post_id) != null)
                likedPosts.Add(posts[i]);
            if (database.comments.FirstOrDefault(record => record.user == searchedUser.user_id && record.post == dbPosts[i].post_id) != null)
                commentedPosts.Add(posts[i]);
        }
        returnedUser.posts = posts;
        returnedUser.likedPosts = likedPosts.ToArray();
        returnedUser.commentedPosts = commentedPosts.ToArray();
        
        returnedUser.privateProfile = searchedUser.privateProfile;
        returnedUser.profilePicture = searchedUser.profilePicture;
        return Ok(returnedUser);
    }

    [HttpGet("{id:string}/followers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult getFollowersFromUser(string id)
    {
        long numeric_id = Convert.ToInt64(id);
        UserConnectionDto[] connections = database.connections.Where(record => record.following_user == numeric_id).ToArray();
        User[] followers = new User[connections.Length];
        for (int i = 0; i < connections.Length; i++)
        {
            UserDto current = database.users.FirstOrDefault(record => record.user_id == connections[i].user);
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
    
    [HttpGet("{id:string}/following")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult getFollowingFromUser(string id)
    {
        long numeric_id = Convert.ToInt64(id);
        UserConnectionDto[] connections = database.connections.Where(record => record.user == numeric_id).ToArray();
        User[] following = new User[connections.Length];
        for (int i = 0; i < connections.Length; i++)
        {
            UserDto current = database.users.FirstOrDefault(record => record.user_id == connections[i].user);
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

    [HttpPost("{id:string}")]
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

    [HttpPut("{id:string}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult updateExistingUser(string id,[FromBody] User newData)
    {
        long numeric_id = Convert.ToInt64(id);
        
        UserDto userToUpdate = database.users.FirstOrDefault(record => record.user_id == numeric_id);
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
            database.users.Update(userToUpdate);
            database.SaveChanges();
            return Accepted();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPost("{id:string}/block")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult blockUser(string id, [FromBody] User userToBlock)
    {
        long numeric_id = Convert.ToInt64(id);
        
        if (database.users.FirstOrDefault(record => record.user_id == Convert.ToInt64(userToBlock.id)) == null)
            return NotFound();
        
        BlockedUserDTO blockedUserDto = new BlockedUserDTO
        {
            blocked_user = Convert.ToInt64(userToBlock.id),
            user = numeric_id
        };

        try
        {
            database.blockedUsers.Add(blockedUserDto);
            
            UserConnectionDto connection = database.connections.FirstOrDefault(record => record.following_user == Convert.ToInt64(userToBlock.id) && record.user == numeric_id);
            if (connection != null)
                database.connections.Remove(connection);
            
            database.SaveChanges();
            return Ok();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPost("{id:string}/unblock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult unblockUser(string id, [FromBody] User userToUnblock)
    {
        long numeric_id = Convert.ToInt64(id);
        
        if (database.users.FirstOrDefault(record => record.user_id == Convert.ToInt64(userToUnblock.id)) == null)
            return NotFound();

        BlockedUserDTO unblockedUserDto = database.blockedUsers.FirstOrDefault(record => 
            record.user == numeric_id && record.blocked_user == Convert.ToInt64(userToUnblock.id)
        );
        try
        {
            database.blockedUsers.Remove(unblockedUserDto);
            database.SaveChanges();
            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }
    
    [HttpDelete("{id:string}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult deleteUser(string id)
    {
        long numeric_id = Convert.ToInt64(id);
        
        UserDto cancelledUser = database.users.FirstOrDefault(record => record.user_id == numeric_id);
        if (cancelledUser == null)
            return NotFound();
        
        database.users.Remove(cancelledUser);
        database.SaveChanges();
        return NoContent();
    }
}