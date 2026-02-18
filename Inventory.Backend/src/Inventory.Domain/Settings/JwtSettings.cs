namespace Inventory.Domain.Settings
{
    public class JwtSettings
    {
        public const string SectionName = "JWT";
        public string Key { get; set; } = string.Empty;
        public int DurationInMinutes { get; set; }
    }
}
