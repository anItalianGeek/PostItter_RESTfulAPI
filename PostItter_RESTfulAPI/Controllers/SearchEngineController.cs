using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostItter_RESTfulAPI.DatabaseContext;
using PostItter_RESTfulAPI.Models;
using PostItter_RESTfulAPI.Models.DatabaseModels;

namespace PostItter_RESTfulAPI.Controllers;

[ApiController]
[Route("api/searchEngine")]
public class SearchEngineController : ControllerBase
{
    private readonly ApplicationDbContext database;

    public SearchEngineController(ApplicationDbContext db)
    {
        database = db;
    }

    [HttpGet("posts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> searchPosts([FromQuery] string prompt)
    {
        try
        {
            return await retrievePosts(prompt);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> searchUsers([FromQuery] string prompt)
    {
        try
        {
            return await retrieveUsers(prompt);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpGet("hashtags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> searchHashtags([FromQuery] string prompt)
    {
        try
        {
            return await retrieveHashtags(prompt);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    /** Unused method, might implement it, keeping it here just in case
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult[]> search([FromQuery] string prompt)
    {
        try
        {
            return [await retrievePosts(prompt), await retrieveUsers(prompt), await retrieveHashtags(prompt)];
        }
        catch (Exception)
        {
            return [StatusCode(500, "Internal Server Error")];
        }
    } */

    private async Task<IActionResult> retrievePosts(string prompt)
    {
        List<PostDto> searchedPosts = await database.posts.Where(record => record.body.Contains(prompt)).ToListAsync();
        List<Post> posts = new List<Post>();
        try
        {
            foreach (PostDto postDto in searchedPosts)
            {
                List<CommentDto> commentDtos = await database.comments.Where(record => record.post == postDto.post_id).ToListAsync();
                UserDto postUploader = await database.users.FirstOrDefaultAsync(record => record.user_id == postDto.user_id);
                Post post = new Post
                {
                    body = postDto.body,
                    color = postDto.color,
                    comments = new Comment[commentDtos.Count],
                    hashtags = await database.hashtags.Where(record => record.post_ref == postDto.post_id)
                        .Select(e => e.content).ToArrayAsync(),
                    id = postDto.post_id.ToString(),
                    likes = postDto.likes,
                    reposts = postDto.reposts,
                    shares = postDto.shares,
                    user = new User
                    {
                        id = postUploader.user_id.ToString(),
                        displayName = postUploader.displayname,
                        username = postUploader.username,
                        profilePicture = postUploader.profilePicture
                    }
                };
                posts.Add(post);
            }
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error");
        }

        return Ok(posts);
    }

    private async Task<IActionResult> retrieveUsers(string prompt)
    {
        List<UserDto> searchedUsers = await database.users.Where(record => record.username.Contains(prompt) || record.displayname.Contains(prompt) || record.bio.Contains(prompt)).ToListAsync();
        List<User> users = new List<User>();
        try
        {
            foreach (UserDto userDto in searchedUsers)
            {
                User user = new User
                {
                    id = userDto.user_id.ToString(),
                    profilePicture = userDto.profilePicture,
                    displayName = userDto.displayname,
                    username = userDto.username
                };
                users.Add(user);
            }

            return Ok(users);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task<IActionResult> retrieveHashtags(string prompt)
    {
        try
        {
            List<long> postIds = await database.hashtags
                .Where(record => record.content.ToLower().Contains(prompt.ToLower())).Select(e => e.post_ref)
                .ToListAsync();
            List<Post> posts = new List<Post>();
            foreach (long postId in postIds)
            {
                PostDto postDto = await database.posts.FirstOrDefaultAsync(record => record.post_id == postId);
                UserDto postingUser =
                    await database.users.FirstOrDefaultAsync(record => record.user_id == postDto.user_id);
                List<CommentDto> comments =
                    await database.comments.Where(record => record.post == postDto.post_id).ToListAsync();

                Post toAdd = new Post
                {
                    body = postDto.body,
                    id = postDto.post_id.ToString(),
                    likes = postDto.likes,
                    reposts = postDto.reposts,
                    shares = postDto.shares,
                    hashtags = await database.hashtags.Where(record => record.post_ref == postDto.post_id)
                        .Select(e => e.content).ToArrayAsync(),
                    comments = new Comment[comments.Count],
                    color = postDto.color,
                    user = new User
                    {
                        id = postingUser.user_id.ToString(),
                        profilePicture = postingUser.profilePicture,
                        username = postingUser.username,
                        displayName = postingUser.displayname
                    }
                };
                
                if (posts.FirstOrDefault(e => e.id == toAdd.id) == null)
                    posts.Add(toAdd);
            }

            return Ok(posts);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error.");
        }
    }
}