using av_motion_api.Controllers;
using av_motion_api.Data;
using av_motion_api.Controllers;
using av_motion_api.Data;
using av_motion_api.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace av_motion_api.Services
{
    public class BillingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly int _billingIntervalInMinutes = 1; // Set the interval to run every 1 minute for testing

        public BillingService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Trigger the billing cycle
                    ProcessBillingCycle(context);

                    // Save changes
                    await context.SaveChangesAsync(stoppingToken);
                }

                // Wait for the next interval
                await Task.Delay(TimeSpan.FromMinutes(_billingIntervalInMinutes), stoppingToken);
            }
        }

        private void ProcessBillingCycle(AppDbContext context)
        {
            var activeContracts = context.Contracts
                .Where(c => c.Approval_Status && !c.IsTerminated && c.Expiry_Date > DateTime.Now)
                .ToList();

            foreach (var contract in activeContracts)
            {
                var paymentStatus = CalculateExpectedPayments(context, contract);
                var currentDate = DateTime.Now;

                // Check if the next expected payment date is after the contract's expiry date
                if (paymentStatus.NextExpectedPaymentDate > contract.Expiry_Date)
                {
                    // Terminate the contract if the next expected payment is beyond the expiry date
                    TerminateContract(context, contract);
                    continue; // Skip further processing for this contract
                }

                // Process the payment if due
                if (currentDate >= paymentStatus.NextExpectedPaymentDate)
                {
                    var lastPaymentId = context.Payments
                        .Where(p => p.Contract_ID == contract.Contract_ID && p.Order_ID == null) // Only contract payments
                        .Max(p => (int?)p.Payment_ID) ?? 0;

                    if (!paymentStatus.HasPaid) // If there are outstanding payments
                    {
                        // Handle outstanding payments by recording them
                        var outstandingPayment = new Outstanding_Payment
                        {
                            Due_Date = paymentStatus.NextExpectedPaymentDate,
                            Amount_Due = paymentStatus.MonthlyFeeDue,
                            Member_ID = contract.Member_ID,
                            Payment_ID = lastPaymentId + 1
                        };

                        context.Outstanding_Payments.Add(outstandingPayment);
                    }
                    else // Handle a regular payment
                    {
                        // Resolve any existing outstanding payments
                        var outstandingPayments = context.Outstanding_Payments
                            .Where(op => op.Member_ID == contract.Member_ID && op.Amount_Due > 0)
                            .ToList();

                        decimal totalOutstanding = 0;
                        foreach (var outstanding in outstandingPayments)
                        {
                            totalOutstanding += outstanding.Amount_Due;
                            outstanding.Amount_Due = 0; // Mark the outstanding payment as paid
                        }

                        var payment = new Payment
                        {
                            Amount = paymentStatus.MonthlyFeeDue + totalOutstanding,
                            Payment_Date = currentDate,
                            Contract_ID = contract.Contract_ID,
                            Payment_Type_ID = 3, // Subscription payment type
                            Payment_ID = lastPaymentId + 1,
                            Order_ID = null // Explicitly indicate that this is a contract payment
                        };

                        context.Payments.Add(payment);
                    }
                }
            }

            context.SaveChanges(); // Save changes after processing all contracts
        }



        private void TerminateContract(AppDbContext context, Contract contract)
        {
            contract.IsTerminated = true;
            context.Contracts.Update(contract);

            // Update the member's status to unsubscribed
            var member = context.Members.Find(contract.Member_ID);
            if (member != null)
            {
                member.Membership_Status_ID = 2; // Set Membership_Status to Unsubscribed

                // Deactivate the associated user account
                var user = context.Users.Find(member.User_ID);
                if (user != null)
                {
                    user.User_Status_ID = 2; // Set User_Status to Deactivated
                }
            }

            // Record the contract in the contract history
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

            // Save all changes to the database
            context.SaveChanges();
        }

        private PaymentStatus CalculateExpectedPayments(AppDbContext context, Contract contract)
        {
            var paymentStatus = new PaymentStatus();
            var currentDate = DateTime.Now;
            var subscriptionDate = contract.Subscription_Date;
            var monthlyFee = GetMonthlyFee(contract.Contract_Type_ID);

            // Fetch payments for this contract where Order_ID is null (i.e., contract payments only)
            var paymentsMade = context.Payments
                .Where(p => p.Contract_ID == contract.Contract_ID && p.Order_ID == null) // Exclude order payments
                .ToList();

            paymentStatus.TotalSumPaid = paymentsMade.Sum(p => p.Amount);

            // Calculate total outstanding payments
            var outstandingPayments = context.Outstanding_Payments
                .Where(op => op.Member_ID == contract.Member_ID && op.Amount_Due > 0)
                .Sum(op => op.Amount_Due);

            paymentStatus.OutstandingPayment = outstandingPayments;
            paymentStatus.MonthlyFeeDue = monthlyFee;
            paymentStatus.HasPaid = outstandingPayments == 0; // True if no outstanding payments

            // Determine the next expected payment date
            paymentStatus.NextExpectedPaymentDate = subscriptionDate.AddMonths(paymentsMade.Count + 1);
            paymentStatus.PaymentCount = paymentsMade.Count;

            return paymentStatus;
        }



        private decimal GetMonthlyFee(int contractTypeId)
        {
            return contractTypeId switch
            {
                1 => 600m, // 3-month
                2 => 500m, // 6-month
                3 => 400m, // 12-month
                _ => 0m
            };
        }

        private int GetContractDuration(int contractTypeId)
        {
            return contractTypeId switch
            {
                1 => 3, // 3-month
                2 => 6, // 6-month
                3 => 12, // 12-month
                _ => 0
            };
        }

        private int GetRandomizedMonth(int contractTypeId)
        {
            int duration = GetContractDuration(contractTypeId);
            return new Random().Next(1, duration + 1); // Selects a month from 1 to duration (inclusive)
        }
    }

    public class PaymentStatus
    {
        public decimal TotalSumPaid { get; set; }
        public decimal OutstandingPayment { get; set; }
        public decimal MonthlyFeeDue { get; set; }
        public bool HasPaid { get; set; }
        public DateTime NextExpectedPaymentDate { get; set; }
        public int PaymentCount { get; set; }
    }
}