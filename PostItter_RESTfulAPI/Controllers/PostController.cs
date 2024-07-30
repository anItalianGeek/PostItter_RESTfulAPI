using Microsoft.AspNetCore.Mvc;
using PostItter_RESTfulAPI.Models;

namespace PostItter_RESTfulAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostController : ControllerBase
{
    private ILogger<PostController> logger;

    PostController(ILogger<PostController> _logger)
    {
        logger = _logger;
    }
    
    //[HttpGet("{id:string}")]
}