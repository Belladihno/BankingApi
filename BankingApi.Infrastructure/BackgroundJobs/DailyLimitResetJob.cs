using BankingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BankingApi.Infrastructure.BackgroundJobs
{
    public class DailyLimitResetJob : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private Timer? _timer;

        public DailyLimitResetJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var nextRun = now.Date.AddDays(1).AddHours(1);
            var delay = nextRun - now;

            _timer = new Timer(async _ => await ResetDailyLimitsAsync(), null, delay, TimeSpan.FromDays(1));

            return Task.CompletedTask;
        }

        private async Task ResetDailyLimitsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.ExecuteSqlRawAsync("UPDATE Accounts SET TodayWithdrawnAmount = 0");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
