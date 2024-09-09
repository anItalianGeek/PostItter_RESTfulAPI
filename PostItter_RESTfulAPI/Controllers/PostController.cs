using System.Text.Json;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using PostItter_RESTfulAPI.DatabaseContext;
using PostItter_RESTfulAPI.Entity;
using PostItter_RESTfulAPI.Entity.DatabaseModels;

namespace PostItter_RESTfulAPI.Controllers;

[ApiController]
[Route("api/posts")]
public class PostController : ControllerBase
{
    private readonly ApplicationDbContext database;

    public PostController(ApplicationDbContext db)
    {
        database = db;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Post[]>> getPosts([FromQuery] string id_retrieving_user)
    {
        if (!long.TryParse(id_retrieving_user, out long id))
            return BadRequest("Invalid user ID.");
        
        try
        {
            List<Post> list = new List<Post>();
            List<PostDto> postDtos = await database.posts.ToListAsync();
            List<BlockedUserDTO> blockedUsers = await database.blockedUsers.Where(record => record.user == id || record.blocked_user == id).ToListAsync();
            foreach (PostDto element in postDtos)
            {
                bool skipPost = false;
                foreach (BlockedUserDTO blockedUser in blockedUsers)
                    if (blockedUser.blocked_user == element.user_id || (id == blockedUser.blocked_user && blockedUser.user == element.user_id))
                    {
                        skipPost = true;
                        break;
                    }

                if (skipPost)
                    continue;
                
                UserDto user = await database.users.FirstOrDefaultAsync(record => record.user_id == element.user_id);
                if (user == null)
                    return NotFound("A post was requested but the user doesn't exist.");

                List<CommentDto> commentDtos = await database.comments.Where(record => record.post == element.post_id).ToListAsync();
                list.Add(new Post
                {
                    id = element.post_id.ToString(),
                    body = element.body,
                    likes = element.likes,
                    reposts = element.reposts,
                    shares = element.shares,
                    user = new User
                    {
                        username = user.username,
                        displayName = user.displayname,
                        id = user.user_id.ToString(),
                        profilePicture = user.profilePicture
                    },
                    hashtags = await database.hashtags.Where(record => record.post_ref == element.post_id).Select(e => e.content).ToArrayAsync(),
                    comments = new Comment[commentDtos.Count],
                    color = element.color
                });
            }
            
            return Ok(list);
        }
        catch (Exception e )
        {
            return StatusCode(500, $"Internal Server Error. {e.Message}");
        }
    }
    
    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status406NotAcceptable)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Post>> getPostById(string id, [FromQuery] string id_retrieving_user)
    {
        if (!long.TryParse(id, out long numeric_id))
            return BadRequest("Invalid user ID");
        if (!long.TryParse(id_retrieving_user, out long idRetrievingUser))
            return BadRequest("Invalid user ID.");
        
        try
        {
            PostDto post = await database.posts.FirstOrDefaultAsync(element => element.post_id == numeric_id);
            
            if (post == null)
                return NotFound("Requested post doesn't exist.");
            else
            {
                BlockedUserDTO isUserBlocked = await database.blockedUsers.FirstOrDefaultAsync(record => record.user == idRetrievingUser && record.blocked_user == post.user_id);
                if (isUserBlocked != null)
                    return StatusCode(406, "Cannot retrieve a post from a Blocked User.");
                
                UserDto retrievedUser = await database.users.FirstOrDefaultAsync(element => element.user_id == post.user_id);
                if (retrievedUser == null)
                    return BadRequest("A post was requested but the user doesn't exist.");
                
                Post returnedPost = new Post();
                returnedPost.body = post.body;
                returnedPost.id = post.post_id.ToString();
                returnedPost.reposts = post.reposts;
                returnedPost.likes = post.likes;
                returnedPost.shares = post.shares;
                returnedPost.color = post.color;
                returnedPost.user = new User();
                returnedPost.hashtags = await database.hashtags.Where(record => record.post_ref == numeric_id).Select(e => e.content).ToArrayAsync();
                CommentDto[] _comments = await database.comments.Where(record => record.post == numeric_id).ToArrayAsync();
                returnedPost.comments = new Comment[_comments.Length];
                for (int j = 0; j < _comments.Length; j++)
                {
                    UserDto commentingUser = await database.users.FirstOrDefaultAsync(record => record.user_id == _comments[j].user);
                    returnedPost.comments[j] = new Comment
                    {
                        user = new User
                        {
                            id = commentingUser.user_id.ToString(),
                            profilePicture = commentingUser.profilePicture,
                            displayName = commentingUser.displayname,
                            username = commentingUser.username
                        },
                        content = _comments[j].content
                    };
                }
                returnedPost.user.id = retrievedUser.user_id.ToString();
                returnedPost.user.username = retrievedUser.username;
                returnedPost.user.displayName = retrievedUser.displayname;
                returnedPost.user.profilePicture = retrievedUser.profilePicture;
                
                return Ok(returnedPost);
            }

        }
        catch (Exception e)
        {
            return BadRequest("Impossible to return the requested post." + e.Message);
        }
    }

    [HttpGet("user/{userId}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Post[]>> getAllPostsByUser(string userId, [FromQuery] string id_retrieving_user)
    {
        if (!long.TryParse(userId, out long numeric_id))
            return BadRequest("Invalid user ID");
        if (!long.TryParse(id_retrieving_user, out long idRetrievingUser))
            return BadRequest("Invalid user ID.");
        
        UserDto user = await database.users.FirstOrDefaultAsync(user => user.user_id == numeric_id);

        if (user == null)
            return NotFound("User does not Exist");
        
        BlockedUserDTO isUserBlocked = await database.blockedUsers.FirstOrDefaultAsync(record => record.user == idRetrievingUser && record.blocked_user == numeric_id);
        if (isUserBlocked != null)
            return StatusCode(406, "Cannot retrieve posts from a blocked user.");
        
        PostDto[] dbPosts = await database.posts.Where(post => post.user_id == numeric_id).ToArrayAsync();
        Post[] posts = new Post[dbPosts.Length];
        for (int i = 0; i < dbPosts.Length; i++)
        {
             posts[i] = new Post();
             
             posts[i].body = dbPosts[i].body;
             posts[i].id = dbPosts[i].post_id.ToString();
             posts[i].likes = dbPosts[i].likes;
             posts[i].reposts = dbPosts[i].reposts;
             posts[i].shares = dbPosts[i].shares;
             posts[i].color = dbPosts[i].color;
             posts[i].hashtags = await database.hashtags.Where(record => record.post_ref == numeric_id).Select(e => e.content).ToArrayAsync();
             
             // comments aren't needed since you have to view the post in detail to see the comments
             
             posts[i].user = new User
             {
                 id = user.user_id.ToString(),
                 displayName = user.displayname,
                 username = user.username,
                 profilePicture = user.profilePicture
             };
        }
        
        return Ok(posts);
    }

    [HttpGet("user/{userId}/{filter}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Post[]>> getAllPostByUserOnFilter(string userId, string filter, [FromQuery] string id_retrieving_user)
    {
        if (!long.TryParse(userId, out long user_id))
            return BadRequest("Invalid user ID");

        if (await database.users.FirstOrDefaultAsync(record => record.user_id == user_id) == null)
            return NotFound("User Does Not Exist.");
        
        if (!long.TryParse(id_retrieving_user, out long idRetrievingUser))
            return BadRequest("Invalid user ID.");
        
        List<BlockedUserDTO> blockedUsers = await database.blockedUsers.Where(record => record.user == idRetrievingUser).ToListAsync();
        foreach (BlockedUserDTO blockedUser in blockedUsers)
            if (blockedUser.blocked_user == user_id)
                return StatusCode(406, "Cannot retrieve posts from a blocked user.");
        
        Post[] posts = null;
        switch (filter)
        {
            case "likes":
                LikeDto[] likes = await database.likes.Where(record => record.user == user_id).ToArrayAsync();
                posts = new Post[likes.Length];
                for (int i = 0; i < likes.Length; i++)
                {
                    PostDto currentPost = await database.posts.FirstOrDefaultAsync(record => record.post_id == likes[i].post);
                    UserDto user = await database.users.FirstOrDefaultAsync(record => record.user_id == currentPost.user_id);
                    posts[i] = new Post();
                    posts[i].body = currentPost.body;
                    posts[i].id = currentPost.post_id.ToString();
                    posts[i].likes = currentPost.likes;
                    posts[i].reposts = currentPost.reposts;
                    posts[i].shares = currentPost.shares;
                    posts[i].color = currentPost.color;
                    posts[i].user = new User
                    {
                        id = user.user_id.ToString(),
                        displayName = user.displayname,
                        username = user.username,
                        profilePicture = user.profilePicture
                    };
                    posts[i].hashtags = await database.hashtags.Where(record => record.post_ref == currentPost.post_id).Select(e => e.content).ToArrayAsync();
                    CommentDto[] _comments = await database.comments.Where(record => record.post == currentPost.post_id).ToArrayAsync();
                    posts[i].comments = new Comment[_comments.Length];
                    for (int j = 0; j < _comments.Length; j++)
                    {
                        UserDto commentingUser = await database.users.FirstOrDefaultAsync(record => record.user_id == _comments[j].user);
                        posts[i].comments[j] = new Comment
                        {
                            user = new User
                            {
                                id = commentingUser.user_id.ToString(),
                                profilePicture = commentingUser.profilePicture,
                                displayName = commentingUser.displayname,
                                username = commentingUser.username
                            },
                            content = _comments[j].content
                        };
                    }
                }
                break;
            
            case "comments":
                CommentDto[] comments = await database.comments.Where(record => record.user == user_id).ToArrayAsync();
                PostDto[] dbPosts = new PostDto[comments.Length];
                for (int i = 0; i < comments.Length; i++)
                {
                   dbPosts[i] = await database.posts.FirstOrDefaultAsync(record => record.post_id == comments[i].post);
                }

                posts = new Post[comments.Length];
                for (int i = 0; i < dbPosts.Length; i++)
                {
                    Task<UserDto> retrieveUser = database.users.FirstOrDefaultAsync(record => record.user_id == dbPosts[i].user_id);
                    posts[i] = new Post();
                    posts[i].body = dbPosts[i].body;
                    posts[i].likes = dbPosts[i].likes;
                    posts[i].reposts = dbPosts[i].reposts;
                    posts[i].shares = dbPosts[i].shares;
                    posts[i].color = dbPosts[i].color;
                    UserDto postingPerson = await retrieveUser;
                    posts[i].user = new User
                    {
                        id = postingPerson.user_id.ToString(),
                        profilePicture = postingPerson.profilePicture,
                        displayName = postingPerson.displayname,
                        username = postingPerson.username
                    };
                    posts[i].hashtags = await database.hashtags.Where(record => record.post_ref == dbPosts[i].post_id).Select(e => e.content).ToArrayAsync();
                    List<CommentDto> postComments = await database.comments.Where(record => record.post == dbPosts[i].post_id).ToListAsync();
                    posts[i].comments = new Comment[postComments.Count];
                    for (int j = 0; j < postComments.Count; j++)
                    {
                        UserDto commentingUser = await database.users.FirstOrDefaultAsync(record => record.user_id == postComments[j].user);
                        posts[i].comments[j].user = new User
                        {
                            id = commentingUser.user_id.ToString(),
                            profilePicture = commentingUser.profilePicture,
                            displayName = commentingUser.displayname,
                            username = commentingUser.username
                        };
                        posts[i].comments[j].content = postComments[j].content;
                    }
                }
                break;
            
            case "reposts":
                PostDto[] repostedPosts = await database.posts.Where(record => record.post_ref != 0 && record.user_id == user_id).ToArrayAsync();
                posts = new Post[repostedPosts.Length];
                UserDto myself = await database.users.FirstOrDefaultAsync(record => record.user_id == user_id);
                for (int i = 0; i < posts.Length; i++)
                {
                    posts[i] = new Post();
                    posts[i].id = repostedPosts[i].post_id.ToString();
                    posts[i].body = repostedPosts[i].body;
                    posts[i].likes = repostedPosts[i].likes;
                    posts[i].reposts = repostedPosts[i].reposts;
                    posts[i].shares = repostedPosts[i].shares;
                    posts[i].color = repostedPosts[i].color;
                    posts[i].user = new User
                    {
                        id = myself.user_id.ToString(),
                        displayName = myself.displayname,
                        username = myself.username,
                        profilePicture = myself.profilePicture
                    };
                    posts[i].hashtags = await database.hashtags.Where(record => record.post_ref == repostedPosts[i].post_id).Select(e => e.content).ToArrayAsync();
                    List<CommentDto> _comments = await database.comments.Where(record => record.post == repostedPosts[i].post_id).ToListAsync();
                    posts[i].comments = new Comment[_comments.Count];
                    for (int j = 0; j < _comments.Count; j++)
                    {
                        UserDto commentingUser = database.users.FirstOrDefault(record => record.user_id == _comments[j].user);
                        posts[i].comments[j] = new Comment
                        {
                            user = new User
                            {
                                id = commentingUser.user_id.ToString(),
                                profilePicture = commentingUser.profilePicture,
                                displayName = commentingUser.displayname,
                                username = commentingUser.username
                            },
                            content = _comments[j].content
                        };
                    }
                }
                break;
        }
        
        if (posts != null)
            return Ok(posts);
        return NotFound();
    }

    [HttpPost("new")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> addNewPost([FromBody] Post post)
    {
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
        
        PostDto postDto = new PostDto
        {
            body = post.body,
            likes = 0,
            reposts = 0,
            shares = 0,
            post_ref = 0, // TODO must implement reposting
            user_id = Convert.ToInt64(post.user.id),
            color = post.color
        };

        try
        {
            await database.posts.AddAsync(postDto);
            await database.SaveChangesAsync();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
        
        postDto = await database.posts.OrderByDescending(e => e.post_id).FirstOrDefaultAsync(); 
        for (int i = 0; i < post.hashtags.Length; i++)
        {
            try
            {
                await database.hashtags.AddAsync(new HashtagDto
                {
                    content = post.hashtags[i],
                    post_ref = postDto.post_id
                });
            }
            catch (Exception e )
            {
                return StatusCode(500, $"Internal Server Error. {e.Message}");
            }
        }

        try
        {
            await database.SaveChangesAsync();
            post.id = postDto.post_id.ToString();
            return
                Created("", post); // comments aren't needed, how can a post have comments before it has been created?? hashtags are fine like this btw
        }
        catch (Exception e)
        {
            return StatusCode(500, $"Internal Server Error. {e.Message}");
        }
    }

    [HttpPut("update/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> updatePost(string id, [FromQuery] string action, [FromQuery] string id_retrieving_user, [FromBody] Comment? comment)
    {
        if (!long.TryParse(id, out long numeric_id))
            return BadRequest("Invalid user ID");

        if (!long.TryParse(id_retrieving_user, out long currentUser))
            return BadRequest("Invalid current user ID");
        
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
        } */
        
        PostDto postDto = await database.posts.FirstOrDefaultAsync(record => record.post_id == numeric_id);
        
        if (postDto == null) return NotFound("Post Does Not Exist.");
        
        switch (action)
        {
            case "add-like":
                if (await database.likes.FirstOrDefaultAsync(record =>
                        record.user == currentUser && record.post == postDto.post_id) != null)
                    return NoContent();
                
                postDto.likes++;
                LikeDto newLike = new LikeDto
                {
                    post = postDto.post_id,
                    user = currentUser
                };

                try
                {
                    await database.likes.AddAsync(newLike);
                    await database.SaveChangesAsync();
                    return Created();
                }
                catch (Exception)
                {
                    return StatusCode(500, "Internal Server Error");
                }
                break;
            
            case "remove-like":
                LikeDto like = await database.likes.FirstOrDefaultAsync(record => record.user == currentUser && record.post == postDto.post_id); 
                if (like == null)
                    return NoContent();
                
                postDto.likes--;

                try
                {
                    database.likes.Remove(like);
                    await database.SaveChangesAsync();
                    return NoContent();
                }
                catch (Exception)
                {
                    return StatusCode(500, "Internal Server Error");
                }
                break;
            
            case "add-comment":
                if (comment is null)
                    return BadRequest("Missing Comment, cannot proceed with operation.");
            
                CommentDto newComment = new CommentDto
                {
                    content = comment.content,
                    post = numeric_id,
                    user = Convert.ToInt64(comment.user.id)
                };
                try
                {
                    await database.comments.AddAsync(newComment);
                    await database.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return StatusCode(500, "Internal Server Error.");
                }
                return Created();
        
            case "remove-comment":
                if (comment is null)
                    return BadRequest("Missing Comment, cannot proceed with operation.");
            
                CommentDto commentDto = database.comments.FirstOrDefault(record => 
                    record.content == comment.content && record.post == numeric_id && record.user == Convert.ToInt64(comment.user.id)
                );
            
                if (comment == null) return NotFound("Comment Does Not Exist.");
            
                try
                {
                    database.comments.Remove(commentDto);
                    await database.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return StatusCode(500, "Internal Server Error.");
                }
                return NoContent();
            
            case "repost":
                postDto.reposts++;
                break;
            
            case "share":
                postDto.shares++;
                break;
            
            default:
                return BadRequest("Invalid action specified.");
        }

        try
        {
            await database.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, "Internal Server Error.");
        }

        return Ok();
    }
    
    [HttpDelete("delete/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> deletePost(string id)
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
        
        PostDto postToDelete = await database.posts.FirstOrDefaultAsync(record => record.post_id == numeric_id);

        if (postToDelete == null)
            return NotFound();

        try
        {
            database.posts.Remove(postToDelete);
            await database.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }
}