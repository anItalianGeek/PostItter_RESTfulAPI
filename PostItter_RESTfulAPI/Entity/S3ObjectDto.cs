namespace PostItter_RESTfulAPI.Entity;

public class S3ObjectDto
{
    public string? Name { get; set; }
    public string? PresignedUrl { get; set; }
}