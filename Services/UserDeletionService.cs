using av_motion_api.Data; 
using av_motion_api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace av_motion_api.Services
{
    public class UserDeletionService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptionsMonitor<DeletionSettings> _settings;
        private Timer _timer;
        private long _remainingIntervals;
        private const long MaxInterval = Int32.MaxValue - 2;

        public UserDeletionService(IServiceProvider serviceProvider, IOptionsMonitor<DeletionSettings> settings)
        {
            _serviceProvider = serviceProvider;
            _settings = settings;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ScheduleDeletionTask();
            _settings.OnChange(settings => ScheduleDeletionTask());
            return Task.CompletedTask;
        }

        private void ScheduleDeletionTask()
        {
            var interval = GetTimeSpan(_settings.CurrentValue.DeletionTimeValue, _settings.CurrentValue.DeletionTimeUnit);
            _remainingIntervals = (long)Math.Ceiling(interval.TotalMilliseconds / MaxInterval);

            _timer?.Dispose();
            _timer = new Timer(OnTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(MaxInterval));
        }



        private void OnTimerElapsed(object state)
        {
            if (--_remainingIntervals <= 0)
            {
                DeleteDeactivatedUsers(state);
                ScheduleDeletionTask(); // Reschedule for the next interval
            }
        }

        private void DeleteDeactivatedUsers(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var deletionThreshold = DateTime.UtcNow.Subtract(GetTimeSpan(_settings.CurrentValue.DeletionTimeValue, _settings.CurrentValue.DeletionTimeUnit));

                var connection = context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UpdateUserDeletionSettings";
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@DeletionTimeValue", _settings.CurrentValue.DeletionTimeValue));
                    command.Parameters.Add(new SqlParameter("@DeletionTimeUnit", _settings.CurrentValue.DeletionTimeUnit));

                    command.ExecuteNonQuery();
                }
            }
        }


        private TimeSpan GetTimeSpan(int value, string unit)
        {
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
