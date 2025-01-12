using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using av_motion_api.Data; // Adjust the namespace to match your project
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using av_motion_api.Models;

namespace av_motion_api.Services
{
    public class OrderStatusUpdater : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptionsMonitor<OverdueSettings> _settings;
        private long _remainingIntervals;
        private const long MaxInterval = Int32.MaxValue - 2;

        public OrderStatusUpdater(IServiceProvider serviceProvider, IOptionsMonitor<OverdueSettings> settings)
        {
            _serviceProvider = serviceProvider;
            _settings = settings;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ScheduleOrderStatusUpdate();
            _settings.OnChange(settings => ScheduleOrderStatusUpdate());
            return Task.CompletedTask;
        }

        private void ScheduleOrderStatusUpdate()
        {
            var interval = GetTimeSpan(_settings.CurrentValue.OverdueTimeValue, _settings.CurrentValue.OverdueTimeUnit);
            _remainingIntervals = (long)Math.Ceiling(interval.TotalMilliseconds / MaxInterval);

            _timer?.Dispose();
            _timer = new Timer(OnTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(MaxInterval));
        }

        private void OnTimerElapsed(object state)
        {
            if (--_remainingIntervals <= 0)
            {
                UpdateOrderStatuses(state);
                ScheduleOrderStatusUpdate(); // Reschedule for the next interval
            }
        }

        private void UpdateOrderStatuses(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Calculate the overdue threshold time
                var overdueThreshold = DateTime.UtcNow.Subtract(GetTimeSpan(_settings.CurrentValue.OverdueTimeValue, _settings.CurrentValue.OverdueTimeUnit));

                var connection = context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UpdateOrderStatuses";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    // Add the required parameters
                    command.Parameters.Add(new SqlParameter("@OverdueTimeValue", _settings.CurrentValue.OverdueTimeValue));
                    command.Parameters.Add(new SqlParameter("@OverdueTimeUnit", _settings.CurrentValue.OverdueTimeUnit));

                    // Execute the stored procedure
                    command.ExecuteNonQuery();
                }
            }
        }

        private TimeSpan GetTimeSpan(int value, string unit)
        {
            if (string.IsNullOrEmpty(unit))
            {
                throw new ArgumentNullException(nameof(unit), "Unit cannot be null or empty.");
            }

            return unit.ToLower() switch
            {
                "minutes" => TimeSpan.FromMinutes(value),
                "hours" => TimeSpan.FromHours(value),
                "days" => TimeSpan.FromDays(value),
                "weeks" => TimeSpan.FromDays(value * 7),
                "months" => TimeSpan.FromDays(value * 30), // Approximation
                "years" => TimeSpan.FromDays(value * 365), // Approximation
                _ => TimeSpan.FromMinutes(value),
            };
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
