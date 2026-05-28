using Inventory.Domain.Enums;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Services
{
    public class ExpiredAllocationCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpiredAllocationCleanupBackgroundService> _logger;

        public ExpiredAllocationCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ExpiredAllocationCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExpiredAllocationCleanupBackgroundService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredAllocationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up expired allocations.");
                }

                // Wait 5 minutes before running again
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }

            _logger.LogInformation("ExpiredAllocationCleanupBackgroundService is stopping.");
        }

        private async Task CleanupExpiredAllocationsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.UtcNow;

            // Find Draft orders whose AllocationExpiresAt has passed, and they still have allocations
            var expiredDrafts = await context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Allocations)
                        .ThenInclude(a => a.StockBatch) // We need the StockBatch to restore
                .AsSplitQuery()
                .Where(o => o.Status == OrderStatus.Draft 
                            && o.AllocationExpiresAt <= now
                            && o.Items.Any(i => i.Allocations.Any()))
                .ToListAsync(cancellationToken);

            if (!expiredDrafts.Any()) return;

            int count = 0;
            foreach (var order in expiredDrafts)
            {
                order.ReleaseAllocations();
                count++;
            }

            if (count > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Cleaned up expired allocations for {Count} draft orders.", count);
            }
        }
    }
}
