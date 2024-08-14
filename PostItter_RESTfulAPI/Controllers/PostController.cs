using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostItter_RESTfulAPI.DatabaseContext;
using PostItter_RESTfulAPI.Models;
using PostItter_RESTfulAPI.Models.DatabaseModels;

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

    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Post>> getPostById(string id)
    {
        if (!long.TryParse(id, out long numeric_id))
            return BadRequest("Invalid user ID");

        try
        {
            PostDto post = await database.posts.FirstOrDefaultAsync(element => element.post_id == numeric_id);

            if (post == null)
                return NotFound();
            else
            {
                Task<UserDto?> retrieveUser = database.users.FirstOrDefaultAsync(element => element.user_id == post.user_id);
                Post returnedPost = new Post();
                returnedPost.body = post.body;
                returnedPost.id = post.post_id.ToString();
                returnedPost.reposts = post.reposts;
                returnedPost.likes = post.likes;
                returnedPost.shares = post.shares;
                returnedPost.user = new User();
                UserDto user = await retrieveUser;
                returnedPost.user.id = user.user_id.ToString();
                returnedPost.user.username = user.username;
                returnedPost.user.displayName = user.displayname;
                returnedPost.user.profilePicture = user.profilePicture;
                // comments TODO
                // hashtags
                return Ok(returnedPost);
            }

        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    [HttpGet("user/{userId}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Post[]>> getAllPostsByUser(string userId)
    {
        if (!long.TryParse(userId, out long numeric_id))
            return BadRequest("Invalid user ID");
        
        UserDto user = await database.users.FirstOrDefaultAsync(user => user.user_id == numeric_id);

        if (user == null)
            return NotFound("User does not Exist");
        
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
             
             posts[i].user = new User
             {
                 id = user.user_id.ToString(), // dbPosts[i].user_id.ToString(); ??
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
    public async Task<ActionResult<Post[]>> getAllPostByUserOnFilter(string userId, string filter)
    {
        if (!long.TryParse(userId, out long user_id))
            return BadRequest("Invalid user ID");

        if (await database.users.FirstOrDefaultAsync(record => record.user_id == user_id) == null)
            return NotFound("User Does Not Exist.");
        
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
                    posts[i].user = new User
                    {
                        id = user.user_id.ToString(),
                        displayName = user.displayname,
                        username = user.username,
                        profilePicture = user.profilePicture
                    };
                    HashtagDto[] hashtags = await database.hashtags.Where(record => record.post_ref == currentPost.post_id).ToArrayAsync();
                    posts[i].hashtags = new string[hashtags.Length];
                    for (int j = 0; j < hashtags.Length; j++) posts[i].hashtags[j] = hashtags[j].content;
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
                    UserDto postingPerson = await retrieveUser;
                    posts[i].user = new User
                    {
                        id = postingPerson.user_id.ToString(),
                        profilePicture = postingPerson.profilePicture,
                        displayName = postingPerson.displayname,
                        username = postingPerson.username
                    };
                    Task<HashtagDto[]> retrieveHashtags = database.hashtags.Where(record => record.post_ref == dbPosts[i].post_id).ToArrayAsync();
                    Task<CommentDto[]> retrieveComments = database.comments.Where(record => record.post == dbPosts[i].post_id).ToArrayAsync();
                    HashtagDto[] hashtags = await retrieveHashtags;
                    CommentDto[] postComments = await retrieveComments;
                    posts[i].hashtags = new string[hashtags.Length];
                    for (int j = 0; j < hashtags.Length; j++) posts[i].hashtags[j] = hashtags[j].content;
                    posts[i].comments = new Comment[postComments.Length];
                    for (int j = 0; j < postComments.Length; j++)
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
                    posts[i].user = new User
                    {
                        id = myself.user_id.ToString(),
                        displayName = myself.displayname,
                        username = myself.username,
                        profilePicture = myself.profilePicture
                    };
                    Task<HashtagDto[]> retrieveHashtags = database.hashtags.Where(record => record.post_ref == repostedPosts[i].post_id).ToArrayAsync();
                    Task<CommentDto[]> retrieveComments = database.comments.Where(record => record.post == repostedPosts[i].post_id).ToArrayAsync();
                    HashtagDto[] hashtags = await retrieveHashtags;
                    CommentDto[] _comments = await retrieveComments;
                    posts[i].hashtags = new string[hashtags.Length];
                    for (int j = 0; j < hashtags.Length; j++) posts[i].hashtags[j] = hashtags[j].content;
                    posts[i].comments = new Comment[_comments.Length];
                    for (int j = 0; j < _comments.Length; j++)
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
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> addNewPost([FromBody] Post post)
    {
        PostDto postDto = new PostDto
        {
            body = post.body,
            likes = 0,
            reposts = 0,
            shares = 0,
            post_ref = 0, // TODO must implement reposting!!!!
            user_id = Convert.ToInt64(post.user.id)
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
            catch (Exception)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        await database.SaveChangesAsync();
        return Created(); // comments aren't needed, how can a post have comments before it has been created?? hashtags are fine like this btw
    }

    [HttpPut("update/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> updatePost(string id,[FromQuery] string action, [FromBody] Comment? comment)
    {
        if (!long.TryParse(id, out long numeric_id))
            return BadRequest("Invalid user ID");

        PostDto postDto = await database.posts.FirstOrDefaultAsync(record => record.post_id == numeric_id);
        
        if (postDto == null) return NotFound("Post Does Not Exist.");
        
        switch (action)
        {
            case "add-like":
                postDto.likes++;
                break;
            
            case "remove-like":
                postDto.likes--;
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
                await database.comments.AddAsync(newComment);
                return Created();
            
            case "remove-comment":
                if (comment is null)
                    return BadRequest("Missing Comment, cannot proceed with operation.");
                
                CommentDto commentDto = database.comments.FirstOrDefault(record => 
                    record.content == comment.content && record.post == numeric_id && record.user == Convert.ToInt64(comment.user.id)
                );
                
                if (comment == null) return NotFound("Comment Does Not Exist.");
                
                database.comments.Remove(commentDto);
                return NoContent();
            
            case "repost":
                postDto.reposts++;
                break;
            
            case "share":
                postDto.shares++;
                break;
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
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> deletePost(string id)
    {
        if (!long.TryParse(id, out long numeric_id))
            return BadRequest("Invalid user ID");

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