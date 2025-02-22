using CCMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CCMS.Service
{
    public class ScheduledTaskService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly ApplicationDbContext context;
        private readonly ReportsDbContext _reportsDbContext;

        public ScheduledTaskService(ApplicationDbContext context, ReportsDbContext reportsDbContext)
        {
            this.context = context;
            _reportsDbContext = reportsDbContext;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            var scheduledTime = new DateTime(now.Year, now.Month, now.Day, 13, 5, 0);

            if (now > scheduledTime)
                scheduledTime = scheduledTime.AddDays(1); // Schedule for the next day if the time has passed.

            var initialDelay = scheduledTime - now;

            // Wrap ExecuteTask in a sync callback using Task.Run
            _timer = new Timer(async _ =>
            {
                await Task.Run(() => ExecuteTask(null));
            }, null, initialDelay, TimeSpan.FromHours(24)); // Run daily.

            return Task.CompletedTask;
        }

        public async Task ExecuteTaskManually()
        {
            // Call the ExecuteTask method with null or any parameter as needed
            await ExecuteTask(null);
        }

        public async Task ExecuteTask(object state)
        {
            
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
