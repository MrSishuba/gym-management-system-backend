using av_motion_api.Data;
using av_motion_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace av_motion_api.Services
{
    public class ContractExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptionsMonitor<ContractDeletionSettings> _deletionSettings;

        public ContractExpirationService(IServiceProvider serviceProvider, IOptionsMonitor<ContractDeletionSettings> deletionSettings)
        {
            _serviceProvider = serviceProvider;
            _deletionSettings = deletionSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAndTerminateExpiredContracts();
                await PermanentlyDeleteOldContracts();
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                //await Task.Delay(TimeSpan.FromDays(1), stoppingToken); // Runs daily
            }
        }

        private async Task CheckAndTerminateExpiredContracts()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var expiredContracts = await context.Contracts
                    .Where(c => c.Expiry_Date <= DateTime.UtcNow && !c.IsTerminated)
                    .ToListAsync();

                foreach (var contract in expiredContracts)
                {
                    contract.IsTerminated = true; // Mark contract as terminated
                    var member = await context.Members.FindAsync(contract.Member_ID);
                    if (member != null)
                    {
                        member.Membership_Status_ID = 2; // Set Membership_Status to Unsubscribed
                        var user = await context.Users.FindAsync(member.User_ID);
                        if (user != null)
                        {
                            user.User_Status_ID = 2; // Set User_Status to Deactivated
                        }
                    }

                    // Add to contract history
                    var contractHistory = new Contract_History
                    {
                        Contract_ID = contract.Contract_ID,
                        Member_ID = contract.Member_ID,
                        Subscription_Date = contract.Subscription_Date,
                        Expiry_Date = contract.Expiry_Date,
                        Approval_Date = contract.Approval_Date,
                        Terms_Of_Agreement = contract.Terms_Of_Agreement,
                        Approval_Status = contract.Approval_Status,
                        Approval_By = contract.Approval_By,
                        Contract_Type_ID = contract.Contract_Type_ID,
                        Payment_Type_ID = contract.Payment_Type_ID,
                        Employee_ID = contract.Employee_ID,
                        Owner_ID = contract.Owner_ID,
                        Filepath = contract.Filepath,
                        Termination_Date = DateTime.UtcNow,
                        Termination_Reason = "Contract period has elapsed", // Reason for expiration
                        Termination_Reason_Type = "Contract Expired",
                        IsTerminated = true
                    };

                    context.Contract_History.Add(contractHistory);

                }

                await context.SaveChangesAsync();
            }
        }


        private async Task PermanentlyDeleteOldContracts()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var deletionThreshold = DateTime.UtcNow.Subtract(GetDeletionTimeSpan());

                // Find old contracts in the contract history where termination date has passed the deletion threshold
                var oldContracts = await context.Contract_History
                    .Where(ch => ch.Termination_Date <= deletionThreshold && ch.IsTerminated)
                    .ToListAsync();

                foreach (var oldContract in oldContracts)
                {
                    // Find the contract in the Contracts table with the matching Contract_ID
                    var contractToDelete = await context.Contracts
                        .FirstOrDefaultAsync(c => c.Contract_ID == oldContract.Contract_ID);

                    // If the contract exists, delete it
                    if (contractToDelete != null)
                    {
                        context.Contracts.Remove(contractToDelete);
                    }
                }

                // Remove the old contracts from the contract history
                context.Contract_History.RemoveRange(oldContracts);

                await context.SaveChangesAsync();
            }
        }


        private TimeSpan GetDeletionTimeSpan()
        {
            return _deletionSettings.CurrentValue.DeletionTimeUnit.ToLower() switch
            {
                "minutes" => TimeSpan.FromMinutes(_deletionSettings.CurrentValue.DeletionTimeValue),
                "hours" => TimeSpan.FromHours(_deletionSettings.CurrentValue.DeletionTimeValue),
                "days" => TimeSpan.FromDays(_deletionSettings.CurrentValue.DeletionTimeValue),
                "weeks" => TimeSpan.FromDays(_deletionSettings.CurrentValue.DeletionTimeValue * 7),
                "months" => TimeSpan.FromDays(_deletionSettings.CurrentValue.DeletionTimeValue * 30),
                "years" => TimeSpan.FromDays(_deletionSettings.CurrentValue.DeletionTimeValue * 365),
                _ => TimeSpan.FromDays(1095) // Default to 3 years
            };
        }
    }
}
