using Microsoft.AspNetCore.Mvc;

namespace PostItter_RESTfulAPI.Controllers;

[ApiController]
[Route("api/check")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() 
    {
        return Ok("Test route is working!");
    }
}
