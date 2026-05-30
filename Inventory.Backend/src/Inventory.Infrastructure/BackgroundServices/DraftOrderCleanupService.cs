using Inventory.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.BackgroundServices
{
    public class DraftOrderCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DraftOrderCleanupService> _logger;

        
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
        
        private static readonly TimeSpan ExpirationThreshold = TimeSpan.FromHours(12);

        public DraftOrderCleanupService(IServiceProvider serviceProvider, ILogger<DraftOrderCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DraftOrderCleanupService is starting.");

            using var timer = new PeriodicTimer(Interval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await CleanupExpiredDraftsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DraftOrderCleanupService is stopping.");
            }
        }

        private async Task CleanupExpiredDraftsAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var thresholdTime = DateTime.UtcNow;

                var expiredDrafts = await orderRepository.GetExpiredDraftsForCleanupAsync(thresholdTime, cancellationToken);

                if (expiredDrafts.Any())
                {
                    _logger.LogInformation("Found {Count} expired draft orders to clean up.", expiredDrafts.Count);
                    
                    foreach (var order in expiredDrafts)
                    {
                        order.ReleaseAllocations();
                    }

                    orderRepository.DeleteRange(expiredDrafts);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation("Successfully cleaned up {Count} expired draft orders.", expiredDrafts.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while cleaning up expired draft orders.");
            }
        }
    }
}
