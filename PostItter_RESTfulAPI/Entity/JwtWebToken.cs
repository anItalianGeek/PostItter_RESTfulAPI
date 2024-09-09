namespace PostItter_RESTfulAPI.Entity;

public class JwtWebToken
{
    public string sub { get; set; }
    public string username { get; set; }
    public string displayname { get; set; }
    public DateTime iat { get; set; }
    public DateTime exp { get; set; }
    public string server_signature { get; set; }
}