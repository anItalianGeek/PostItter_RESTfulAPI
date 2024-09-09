namespace PostItter_RESTfulAPI.Entity;

public class LoginUserInput
{
    public string email { get; set; }
    public string password { get; set; }
    public string? displayName { get; set; }
    public string? username { get; set; }
}