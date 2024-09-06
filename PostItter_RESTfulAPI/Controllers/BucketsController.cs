using Amazon.S3;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace PostItter_RESTfulAPI.Controllers;

[Route("api/aws/buckets")]
[ApiController]
public class BucketsController : ControllerBase
{
    private readonly IAmazonS3 _s3Client;

    public BucketsController(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }


    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateBucketAsync(string bucketName)
    {
        try
        {
            var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
            if (bucketExists) return BadRequest($"Bucket {bucketName} already exists.");
            
            await _s3Client.PutBucketAsync(bucketName);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
        
        return Created("buckets", $"Bucket {bucketName} created.");
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllBucketAsync()
    {
        try
        {
            var data = await _s3Client.ListBucketsAsync();
            var buckets = data.Buckets.Select(b => { return b.BucketName; });
            return Ok(buckets);
        }
        catch (AmazonS3Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteBucketAsync(string bucketName)
    {
        try
        {
            await _s3Client.DeleteBucketAsync(bucketName);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }

        return NoContent();
    }
}
