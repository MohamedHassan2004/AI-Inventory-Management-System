namespace Inventory.API.Settings;

public class RefreshTokenCookieSettings
{
    public const string SectionName = "RefreshTokenCookie";

    public string CookieName { get; set; } = "refreshToken";
    public string Path { get; set; } = "/api/auth/refresh-token";
    public bool HttpOnly { get; set; } = true;
    public bool SecurePolicyAlways { get; set; } = true;
    public bool IsEssential { get; set; } = true;
    public string SameSite { get; set; } = "Strict";
}
