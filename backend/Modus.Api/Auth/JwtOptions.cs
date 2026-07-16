namespace Modus.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "modus";
    public string Audience { get; set; } = "modus-web";
    public string SigningKey { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 480;
}
