using System.Globalization;
using System.Text.Json;
using Inventory.Application.Interfaces;

namespace Inventory.Infrastructure.Services;

public class LocalizationService : ILocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _resources;
    private const string DefaultCulture = "en";

    public LocalizationService()
    {
        _resources = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        LoadResources();
    }

    private void LoadResources()
    {
        var resourcesPath = Path.Combine(AppContext.BaseDirectory, "Resources");

        if (!Directory.Exists(resourcesPath))
            return;

        foreach (var file in Directory.GetFiles(resourcesPath, "*.json"))
        {
            var cultureName = Path.GetFileNameWithoutExtension(file);
            var json = File.ReadAllText(file);
            var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (entries is not null)
            {
                _resources[cultureName] = new Dictionary<string, string>(entries, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    public string GetMessage(string key)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        if (_resources.TryGetValue(culture, out var cultureDict) && cultureDict.TryGetValue(key, out var value))
            return value;

        if (_resources.TryGetValue(DefaultCulture, out var defaultDict) && defaultDict.TryGetValue(key, out var fallback))
            return fallback;

        return key;
    }

    public string GetMessage(string key, params object[] args)
    {
        var template = GetMessage(key);
        return string.Format(template, args);
    }
}
