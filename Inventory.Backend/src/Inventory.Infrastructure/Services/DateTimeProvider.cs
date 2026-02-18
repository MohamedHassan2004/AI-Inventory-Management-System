using Inventory.Application.Interfaces;
using Inventory.Domain.Interfaces;

namespace Inventory.Infrastructure.Services
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}