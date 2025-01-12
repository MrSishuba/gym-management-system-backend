using av_motion_api.Data;
using av_motion_api.Models;
using av_motion_api.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace av_motion_api.Services
{
    public class QualifyingMembersService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private Timer _timer;

        public QualifyingMembersService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(UpdateQualifyingMembers, null, TimeSpan.Zero, TimeSpan.FromMinutes(1)); // Check every 1 minute
            return Task.CompletedTask;
        }

        private async void UpdateQualifyingMembers(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var postedRewards = await context.Rewards
                    .Where(r => r.IsPosted)
                    .ToListAsync();

                foreach (var reward in postedRewards)
                {
                    var rewardType = await context.Reward_Types
                        .FindAsync(reward.Reward_Type_ID);

                    if (rewardType != null)
                    {
                        var qualifyingMembers = await GetQualifyingMembersAsync(rewardType.Reward_Criteria);

                        foreach (var member in qualifyingMembers)
                        {
                            var existingRewardMember = await context.Reward_Members
                                .FirstOrDefaultAsync(rm => rm.Member_ID == member.Member_ID && rm.Reward_ID == reward.Reward_ID);

                            if (existingRewardMember == null)
                            {
                                var rewardMember = new Reward_Member
                                {
                                    Member_ID = member.Member_ID,
                                    Reward_ID = reward.Reward_ID,
                                    IsRedeemed = false
                                };
                                context.Reward_Members.Add(rewardMember);
                            }
                        }
                    }
                }

                await context.SaveChangesAsync();
            }
        }

        public async Task<List<QualifyingMembersVM>> GetQualifyingMembersAsync(string criteria)
        {
            // Get the DbContext and the underlying connection
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var connection = context.Database.GetDbConnection();

                // Ensure the connection is open
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                // Define the SQL query with parameters
                var sql = "EXEC GetQualifyingMembers @Criteria";
                var parameters = new[] { new SqlParameter("@Criteria", criteria) };

                // Execute the stored procedure and map results to QualifyingMembersVM
                var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.AddRange(parameters);

                var members = new List<QualifyingMembersVM>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        members.Add(new QualifyingMembersVM
                        {
                            Member_ID = reader.GetInt32(reader.GetOrdinal("Member_ID"))
                        });
                    }
                }

                return members;
            }
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
