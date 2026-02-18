using Inventory.Application.Interfaces;
using Inventory.Infrastructure.Resources;
using Microsoft.Extensions.Localization;

namespace Inventory.Infrastructure.Services;

public class LocalizationService : ILocalizationService
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LocalizationService(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public string GetMessage(string key)
    {
        return _localizer[key];
    }

    public string GetMessage(string key, params object[] args)
    {
        return _localizer[key, args];
    }
}
