using System.Security.Cryptography;
using System.Text;

namespace PostItter_RESTfulAPI;

public static class Hasher
{
    public static string HashPassword(string instance)
    {
        var byteData = Encoding.ASCII.GetBytes(instance);
        var result = SHA256.HashData(byteData);
        return Convert.ToBase64String(result);
    }
}