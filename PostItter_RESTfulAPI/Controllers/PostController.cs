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

    PostController(ApplicationDbContext db)
    {
        database = db;
    }

    [HttpGet("{id:string}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult getPostById(string id)
    {
        long numeric_id = Convert.ToInt64(id);
        try
        {
            PostDto post = database.posts.FirstOrDefault(element => element.post_id == numeric_id);

            if (post == null)
                return NotFound();
            else
            {
                Post returnedPost = new Post();
                returnedPost.body = post.body;
                returnedPost.id = post.post_id.ToString();
                returnedPost.reposts = post.reposts;
                returnedPost.likes = post.likes;
                returnedPost.shares = post.shares;
                UserDto user = database.users.FirstOrDefault(element => element.user_id == post.user_id);
                returnedPost.user = new User();
                returnedPost.user.id = user.user_id.ToString();
                returnedPost.user.username = user.username;
                returnedPost.user.displayName = user.displayname;
                returnedPost.user.profilePicture = user.profilePicture;
                // comments
                // hashtags
                return Ok(returnedPost); // TODO must check if everything is returned correctly
            }

        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    [HttpGet("/user/{userId:string}")]
    public IActionResult getAllPostsByUser(string userId)
    {
        long numeric_id = Convert.ToInt64(userId);
        UserDto user = database.users.FirstOrDefault(user => user.user_id == numeric_id);
        PostDto[] dbPosts = database.posts.Where(post => post.user_id == numeric_id).ToArray();
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

    [HttpGet("/user/{userId:string}/{filter:string}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult getAllPostByUserOnFilter(string userId, string filter)
    {
        long user_id = Convert.ToInt64(userId);
        Post[] posts = null;
        switch (filter)
        {
            case "likes":
                LikeDto[] likes = database.likes.Where(record => record.user == user_id).ToArray();
                posts = new Post[likes.Length];
                for (int i = 0; i < likes.Length; i++)
                {
                    PostDto currentPost = database.posts.FirstOrDefault(record => record.post_id == likes[i].post);
                    UserDto user = database.users.FirstOrDefault(record => record.user_id == currentPost.user_id);
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
                    HashtagDto[] hashtags = database.hashtags.Where(record => record.post_ref == currentPost.post_id).ToArray();
                    posts[i].hashtags = new string[hashtags.Length];
                    for (int j = 0; j < hashtags.Length; j++) posts[i].hashtags[j] = hashtags[j].content;
                    CommentDto[] _comments = database.comments.Where(record => record.post == currentPost.post_id).ToArray();
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
            
            case "comments":
                CommentDto[] comments = database.comments.Where(record => record.user == user_id).ToArray();
                PostDto[] dbPosts = new PostDto[comments.Length];
                for (int i = 0; i < comments.Length; i++)
                {
                   dbPosts[i] = database.posts.FirstOrDefault(record => record.post_id == comments[i].post);
                }

                posts = new Post[comments.Length];
                for (int i = 0; i < dbPosts.Length; i++)
                {
                    posts[i] = new Post();
                    posts[i].body = dbPosts[i].body;
                    posts[i].likes = dbPosts[i].likes;
                    posts[i].reposts = dbPosts[i].reposts;
                    posts[i].shares = dbPosts[i].shares;
                    UserDto postingPerson = database.users.FirstOrDefault(record => record.user_id == dbPosts[i].user_id);
                    posts[i].user = new User
                    {
                        id = postingPerson.user_id.ToString(),
                        profilePicture = postingPerson.profilePicture,
                        displayName = postingPerson.displayname,
                        username = postingPerson.username
                    };
                    HashtagDto[] hashtags = database.hashtags.Where(record => record.post_ref == dbPosts[i].post_id).ToArray();
                    posts[i].hashtags = new string[hashtags.Length];
                    for (int j = 0; j < hashtags.Length; j++) posts[i].hashtags[j] = hashtags[j].content;
                    CommentDto[] postComments = database.comments.Where(record => record.post == dbPosts[i].post_id).ToArray();
                    posts[i].comments = new Comment[postComments.Length];
                    for (int j = 0; j < postComments.Length; j++)
                    {
                        UserDto commentingUser = database.users.FirstOrDefault(record => record.user_id == postComments[j].user);
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
                PostDto[] repostedPosts = database.posts.Where(record => record.post_ref != 0 && record.user_id == user_id).ToArray();
                posts = new Post[repostedPosts.Length];
                UserDto myself = database.users.FirstOrDefault(record => record.user_id == user_id);
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
                    HashtagDto[] hashtags = database.hashtags.Where(record => record.post_ref == repostedPosts[i].post_id).ToArray();
                    posts[i].hashtags = new string[hashtags.Length];
                    for (int j = 0; j < hashtags.Length; j++) posts[i].hashtags[j] = hashtags[j].content;
                    CommentDto[] _comments = database.comments.Where(record => record.post == repostedPosts[i].post_id).ToArray();
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
}