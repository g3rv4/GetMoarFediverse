using System.Security.Cryptography;
using System.Text;

namespace ImportDataAsRelay.Helpers;

public static class CryptoHelper
{
    public static string GetSHA256Digest(string content)
    {
        using var sha256 = SHA256.Create();
        var hashValue = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hashValue);
    }

    public static string ConvertPemToBase64(string filename)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(filename).ToCharArray());
        return Convert.ToBase64String(rsa.ExportRSAPrivateKey());
    }

    public static string Sign(string stringToSign)
    {
        using var rsaProvider = new RSACryptoServiceProvider();
        using var sha256 = SHA256.Create();
        rsaProvider.ImportRSAPrivateKey(Convert.FromBase64String(Environment.GetEnvironmentVariable("PRIVATE_KEY")), out _);
        
        var signature = rsaProvider.SignData(Encoding.UTF8.GetBytes(stringToSign), sha256);
        return Convert.ToBase64String(signature);
    }
}
