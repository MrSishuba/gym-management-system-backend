using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using av_motion_api.Models;
using av_motion_api.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography;
using System.Text;
using av_motion_api.Services;
using System.Security.Claims;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public SubscriptionController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        [HttpGet("CheckSubscriptions")]
        public IActionResult CheckSubscriptions()
        {
            var members = _context.Members
                .Include(m => m.User)
                .Include(m => m.Membership_Status)
                .ToList();

            var response = new List<SubscriptionStatusViewModel>();

            foreach (var member in members)
            {
                var activeContract = _context.Contracts
                    .FirstOrDefault(c =>
                        c.Member_ID == member.Member_ID &&
                        !c.IsTerminated &&
                        c.Terms_Of_Agreement &&
                        c.Approval_Status
                    );

                if (activeContract != null)
                {
                    var paymentStatus = CalculateExpectedPayments(activeContract, member.Member_ID);
                    response.Add(new SubscriptionStatusViewModel
                    {
                        Member_ID = member.Member_ID,
                        Name = member.User?.Name,
                        Surname = member.User?.Surname,
                        Membership_Status_Description = member.Membership_Status?.Membership_Status_Description,
                        Monthly_Fee_Due = paymentStatus.MonthlyFeeDue,
                        Outstanding_Payment = paymentStatus.OutstandingPayment,
                        Has_Paid = paymentStatus.HasPaid,
                        Total_Sum_Paid = paymentStatus.TotalSumPaid,  // Only subscription payments are included
                        Next_Expected_Payment_Date = paymentStatus.NextExpectedPaymentDate
                    });
                }
            }

            return Ok(response);
        }



        [HttpPost("BlockMember/{memberId}")]
        public async Task<IActionResult> BlockMember(int memberId)
        {
            try
            {
                var member = _context.Members.Find(memberId);
                if (member == null || member.Membership_Status_ID == 3) // Already blocked or member not found
                {
                    return NotFound(new { message = "Member not found or already blocked." });
                }

                var contract = _context.Contracts
                    .FirstOrDefault(c => c.Member_ID == memberId && !c.IsTerminated && c.Approval_Status);

                if (contract == null)
                {
                    return NotFound(new { message = "Contract not found for the member." });
                }

                BlockMember(_context, memberId);

                // Audit Trail for BlockMember
                var changedBy = await GetChangedByAsync();
                var audit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"Member Blocked: ID '{member.Member_ID}', Name '{member.User.Name} {member.User.Surname}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Member),
                    Timestamp = DateTime.UtcNow
                };

                _context.Audit_Trails.Add(audit);
                await _context.SaveChangesAsync();

                var paymentStatus = CalculateExpectedPayments(contract, memberId);

                var emailResponse = await SendBlockEmail(new BlockEmailRequestViewModel
                {
                    Email = member.User.Email,
                    Name = $"{member.User.Name} {member.User.Surname}",
                    AmountDue = paymentStatus.OutstandingPayment,
                    NextPaymentDate = paymentStatus.NextExpectedPaymentDate
                });

                if (!(emailResponse is OkObjectResult))
                {
                    return StatusCode(500, new { message = "Member blocked and late fee applied, but failed to send block email." });
                }

                return Ok(new { message = "Member blocked and late fee applied." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }


        }

        [HttpPost("ReactivateMember/{memberId}")]
        public async Task<IActionResult> ReactivateMember(int memberId)
        {
            try
            {
                var member = _context.Members
                                     .Include(m => m.User) // Include the User entity
                                     .FirstOrDefault(m => m.Member_ID == memberId);

                if (member == null || member.Membership_Status_ID != 3) // Not blocked or member not found
                {
                    return NotFound(new { message = "Member not found or not blocked." });
                }

                // Reactivate the member and resolve outstanding payments
                ReactivateMember(_context, memberId);

                // Get the associated contract
                var contract = _context.Contracts.FirstOrDefault(c => c.Member_ID == memberId);

                if (contract == null)
                {
                    return NotFound(new { message = "No active contract found for the member." });
                }

                // Calculate payment status for email
                var paymentStatus = CalculateExpectedPayments(contract, memberId);

                // Prepare and send reactivation email
                var emailResponse = await SendReactivateEmail(new ReactivateEmailRequestViewModel
                {
                    Email = member.User.Email,
                    Name = $"{member.User.Name} {member.User.Surname}",
                    AmountPaid = paymentStatus.MonthlyFeeDue + 30, // Add late fee,
                    NextPaymentDate = paymentStatus.NextExpectedPaymentDate
                });

                // Audit Trail for ReactivateMember
                var changedBy = await GetChangedByAsync();
                var audit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"Member Reactivated: ID '{member.Member_ID}', Name '{member.User.Name} {member.User.Surname}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Member),
                    Timestamp = DateTime.UtcNow
                };

                _context.Audit_Trails.Add(audit);
                await _context.SaveChangesAsync();

                // Check if the emailResponse is an ObjectResult and if it has a successful status code
                if (emailResponse is ObjectResult result && result.StatusCode != StatusCodes.Status200OK)
                {
                    return StatusCode(500, new { message = "Member reactivated and outstanding payments added, but failed to send reactivation email." });
                }

                return Ok(new { message = "Member reactivated and outstanding payments added." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        private void BlockMember(AppDbContext context, int memberId)
        {
            var member = _context.Members
                        .Include(m => m.User) // Include the User entity
                        .FirstOrDefault(m => m.Member_ID == memberId);

            if (member != null && member.Membership_Status_ID != 3)
            {
                var outstandingPayments = context.Outstanding_Payments
                    .Where(op => op.Member_ID == memberId && op.Amount_Due > 0)
                    .ToList();


                member.Membership_Status_ID = 3; // Block the member
                context.SaveChanges();
            }
        }


        private void ReactivateMember(AppDbContext context, int memberId)
        {
            var member = context.Members.Find(memberId);
            if (member != null && member.Membership_Status_ID == 3)
            {
                var outstandingPayments = context.Outstanding_Payments
                    .Where(op => op.Member_ID == memberId && op.Amount_Due > 0)
                    .ToList();

                if (outstandingPayments.Any())
                {
                    // Determine the contract ID
                    var contract = context.Contracts.FirstOrDefault(c => c.Member_ID == memberId);
                    if (contract != null)
                    {
                        // Create a new payment record
                        var totalOutstanding = outstandingPayments.Sum(op => op.Amount_Due);
                        var payment = new Payment
                        {
                            Amount = totalOutstanding,
                            Payment_Date = DateTime.Now,
                            Contract_ID = contract.Contract_ID,
                            Payment_Type_ID = 2 // Subscription payment type
                        };

                        context.Payments.Add(payment);
                        context.SaveChanges(); // Save to get the generated Payment_ID

                        // Update existing outstanding payments
                        foreach (var outstanding in outstandingPayments)
                        {
                            outstanding.Amount_Due = 0; // Mark as resolved
                            outstanding.Payment_ID = payment.Payment_ID; // Link to the newly created payment
                        }

                        // Add new outstanding payments with Amount_Due set to 0
                        foreach (var outstanding in outstandingPayments)
                        {
                            var newOutstandingPayment = new Outstanding_Payment
                            {
                                Due_Date = DateTime.Now,
                                Amount_Due = 0, // Amount is zero since it’s resolved
                                Member_ID = memberId,
                                Payment_ID = payment.Payment_ID // Link to the newly created payment
                            };

                            context.Outstanding_Payments.Add(newOutstandingPayment);
                        }

                        // Save changes to update outstanding payments
                        context.SaveChanges();
                    }
                }

                // Reactivate the member
                member.Membership_Status_ID = 1;
                context.SaveChanges();
            }
        }




        private int GetRandomizedMonth(int contractTypeId)
        {
            int duration = GetContractDuration(contractTypeId);
            return new Random().Next(1, duration + 1); // Selects a month from 1 to duration (inclusive)
        }


        //[HttpPost("SimulateTime")]
        //public IActionResult SimulateTime(int monthsToAdvance)
        //{
        //    var activeContracts = _context.Contracts
        //        .Where(c => c.Approval_Status && !c.IsTerminated && c.Expiry_Date > DateTime.Now)
        //        .ToList();

        //    foreach (var contract in activeContracts)
        //    {
        //        // Check for any payment records with dates beyond the contract expiry date
        //        var overduePayments = _context.Payments
        //            .Where(p => p.Contract_ID == contract.Contract_ID && p.Payment_Date > contract.Expiry_Date)
        //            .ToList();

        //        if (overduePayments.Any())
        //        {
        //            // If any such records exist, terminate the contract immediately
        //            contract.IsTerminated = true;
        //            _context.Contracts.Update(contract);
        //            TerminateContract(contract);
        //            continue; // Skip further processing for this contract
        //        }

        //        DateTime originalExpiryDate = contract.Expiry_Date;
        //        contract.Subscription_Date = contract.Subscription_Date.AddMonths(monthsToAdvance);
        //        int randomizedMonth = GetRandomizedMonth(contract.Contract_Type_ID);

        //        for (int month = 1; month <= monthsToAdvance; month++)
        //        {
        //            var paymentStatus = CalculateExpectedPayments(contract, contract.Member_ID);
        //            var currentDate = DateTime.Now.AddMonths(month);

        //            // Create the payment record first with amount 0
        //            var payment = new Payment
        //            {
        //                Amount = 0,
        //                Payment_Date = currentDate,
        //                Contract_ID = contract.Contract_ID,
        //                Payment_Type_ID = 3 // Subscription payment type
        //            };
        //            _context.Payments.Add(payment);
        //            _context.SaveChanges(); // Ensure Payment_ID is generated

        //            if (month == randomizedMonth) // Apply the randomizer logic for missed payment
        //            {
        //                // Create the outstanding payment record referencing the payment
        //                var outstandingPayment = new Outstanding_Payment
        //                {
        //                    Due_Date = currentDate,
        //                    Amount_Due = paymentStatus.MonthlyFeeDue + 30, // Add late fee
        //                    Member_ID = contract.Member_ID,
        //                    Payment_ID = payment.Payment_ID // Reference the payment ID
        //                };

        //                _context.Outstanding_Payments.Add(outstandingPayment);
        //            }
        //            else // Paid as expected
        //            {
        //                // Update the payment record with the actual amount paid
        //                payment.Amount = paymentStatus.MonthlyFeeDue;
        //                _context.Payments.Update(payment);

        //                // Create an outstanding payment record with amount due 0
        //                var outstandingPayment = new Outstanding_Payment
        //                {
        //                    Due_Date = currentDate,
        //                    Amount_Due = 0, // No outstanding payment since paid on time
        //                    Member_ID = contract.Member_ID,
        //                    Payment_ID = payment.Payment_ID // Reference the payment ID
        //                };

        //                _context.Outstanding_Payments.Add(outstandingPayment);
        //            }

        //            _context.SaveChanges();

        //            // Check if the simulated time has surpassed the expiry date
        //            if (contract.Expiry_Date < currentDate)
        //            {
        //                // Create one additional payment record for 30 days after the expiry date
        //                var additionalPaymentDate = contract.Expiry_Date.AddDays(30);

        //                var additionalPayment = new Payment
        //                {
        //                    Amount = 0,
        //                    Payment_Date = additionalPaymentDate,
        //                    Contract_ID = contract.Contract_ID,
        //                    Payment_Type_ID = 3 // Subscription payment type
        //                };
        //                _context.Payments.Add(additionalPayment);
        //                _context.SaveChanges(); // Ensure Payment_ID is generated

        //                var additionalOutstandingPayment = new Outstanding_Payment
        //                {
        //                    Due_Date = additionalPaymentDate,
        //                    Amount_Due = 0, // Assuming the fee is the same as the monthly fee
        //                    Member_ID = contract.Member_ID,
        //                    Payment_ID = additionalPayment.Payment_ID // Reference the payment ID
        //                };

        //                _context.Outstanding_Payments.Add(additionalOutstandingPayment);
        //                _context.SaveChanges();

        //                // Mark the contract as terminated
        //                contract.IsTerminated = true;
        //                _context.Contracts.Update(contract);

        //                // Terminate the contract as in ContractExpirationService
        //                TerminateContract(contract);
        //            }
        //        }
        //    }

        //    return Ok($"Time advanced by {monthsToAdvance} months, and billing cycle processed.");
        //}

        [HttpPost("SimulateTime")]
        public IActionResult SimulateTime(int monthsToAdvance)
        {
            var simulationDate = DateTime.Now.AddMonths(monthsToAdvance);

            var activeContracts = _context.Contracts
                .Where(c => c.Approval_Status && !c.IsTerminated && c.Expiry_Date > DateTime.Now)
                .ToList();

            foreach (var contract in activeContracts)
            {
                // Calculate the simulated expiration date
                var simulatedExpiryDate = contract.Expiry_Date.AddMonths(monthsToAdvance);

                // Check for any payment records with dates beyond the simulated expiration date
                // Exclude payments that are order payments (Order_ID != null)
                var overduePayments = _context.Payments
                    .Where(p => p.Contract_ID == contract.Contract_ID && p.Order_ID == null && p.Payment_Date > simulatedExpiryDate)
                    .ToList();

                if (overduePayments.Any())
                {
                    // Terminate the contract immediately if overdue payments exist
                    contract.IsTerminated = true;
                    _context.Contracts.Update(contract);
                    TerminateContract(contract);
                    continue; // Skip further processing for this contract
                }

                int randomizedMonth = GetRandomizedMonth(contract.Contract_Type_ID);

                for (int month = 1; month <= monthsToAdvance; month++)
                {
                    var currentDate = DateTime.Now.AddMonths(month);

                    // Calculate the expected payment amount based on the simulated date
                    var paymentStatus = CalculateExpectedPayments(contract, contract.Member_ID);

                    // Create the payment record for subscription only (exclude Order_ID)
                    var payment = new Payment
                    {
                        Amount = 0,
                        Payment_Date = currentDate,
                        Contract_ID = contract.Contract_ID,
                        Order_ID = null,  // Ensures this is a subscription payment
                        Payment_Type_ID = 3 // Subscription payment type
                    };
                    _context.Payments.Add(payment);
                    _context.SaveChanges(); // Ensure Payment_ID is generated

                    if (month == randomizedMonth) // Apply the randomizer logic for missed payment
                    {
                        var outstandingPayment = new Outstanding_Payment
                        {
                            Due_Date = currentDate,
                            Amount_Due = paymentStatus.MonthlyFeeDue + 30, // Add late fee
                            Member_ID = contract.Member_ID,
                            Payment_ID = payment.Payment_ID // Reference the payment ID
                        };

                        _context.Outstanding_Payments.Add(outstandingPayment);
                    }
                    else
                    {
                        payment.Amount = paymentStatus.MonthlyFeeDue;
                        _context.Payments.Update(payment);

                        var outstandingPayment = new Outstanding_Payment
                        {
                            Due_Date = currentDate,
                            Amount_Due = 0, // No outstanding payment since paid on time
                            Member_ID = contract.Member_ID,
                            Payment_ID = payment.Payment_ID
                        };

                        _context.Outstanding_Payments.Add(outstandingPayment);
                    }

                    _context.SaveChanges();

                    if (simulatedExpiryDate < currentDate)
                    {
                        var additionalPaymentDate = simulatedExpiryDate.AddDays(30);

                        var additionalPayment = new Payment
                        {
                            Amount = 0,
                            Payment_Date = additionalPaymentDate,
                            Contract_ID = contract.Contract_ID,
                            Order_ID = null,  // Ensure this is a subscription payment
                            Payment_Type_ID = 3
                        };
                        _context.Payments.Add(additionalPayment);
                        _context.SaveChanges();

                        var additionalOutstandingPayment = new Outstanding_Payment
                        {
                            Due_Date = additionalPaymentDate,
                            Amount_Due = 0,
                            Member_ID = contract.Member_ID,
                            Payment_ID = additionalPayment.Payment_ID
                        };

                        _context.Outstanding_Payments.Add(additionalOutstandingPayment);
                        _context.SaveChanges();

                        contract.IsTerminated = true;
                        _context.Contracts.Update(contract);
                        TerminateContract(contract);
                    }
                }
            }

            return Ok($"Time advanced by {monthsToAdvance} months, and billing cycle processed.");
        }




        private void TerminateContract(Contract contract)
        {
            var member = _context.Members.Find(contract.Member_ID);
            if (member != null)
            {
                member.Membership_Status_ID = 2; // Set Membership_Status to Unsubscribed
                var user = _context.Users.Find(member.User_ID);
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

            _context.Contract_History.Add(contractHistory);
            _context.SaveChanges();
        }

       
        [HttpPost("SimulateBillingCycle")]
        public IActionResult SimulateBillingCycle()
        {
            var activeContracts = _context.Contracts
                .Where(c => c.Approval_Status && !c.IsTerminated && c.Expiry_Date > DateTime.Now)
                .ToList();

            foreach (var contract in activeContracts)
            {
                var paymentStatus = CalculateExpectedPayments(contract, contract.Member_ID);
                var currentDate = DateTime.Now;
                int randomizedMonth = GetRandomizedMonth(contract.Contract_Type_ID);

                // Create the payment record first with amount 0
                var payment = new Payment
                {
                    Amount = 0,
                    Payment_Date = currentDate,
                    Contract_ID = contract.Contract_ID,
                    Payment_Type_ID = 3 // Subscription payment type
                };
                _context.Payments.Add(payment);
                _context.SaveChanges(); // Ensure Payment_ID is generated

                if (randomizedMonth == paymentStatus.PaymentCount + 1) // Apply randomizer for specific month
                {
                    // Create the outstanding payment record referencing the payment
                    var outstandingPayment = new Outstanding_Payment
                    {
                        Due_Date = currentDate,
                        Amount_Due = paymentStatus.MonthlyFeeDue + 30, // Add late fee
                        Member_ID = contract.Member_ID,
                        Payment_ID = payment.Payment_ID // Reference the payment ID
                    };

                    _context.Outstanding_Payments.Add(outstandingPayment);
                }
                else // Paid as expected
                {
                    // Resolve any outstanding payments first
                    var outstandingPayments = _context.Outstanding_Payments
                        .Where(op => op.Member_ID == contract.Member_ID && op.Amount_Due > 0)
                        .ToList();

                    decimal totalOutstanding = 0;

                    foreach (var outstanding in outstandingPayments)
                    {
                        totalOutstanding += outstanding.Amount_Due;
                        outstanding.Amount_Due = 0; // Mark the outstanding as resolved
                    }

                    // Update the payment record with the actual amount paid including outstanding
                    payment.Amount = paymentStatus.MonthlyFeeDue + totalOutstanding;
                    _context.Payments.Update(payment);

                    // Create an outstanding payment record with amount due 0
                    var outstandingPayment = new Outstanding_Payment
                    {
                        Due_Date = currentDate,
                        Amount_Due = paymentStatus.MonthlyFeeDue, // No outstanding payment since paid on time
                        Member_ID = contract.Member_ID,
                        Payment_ID = payment.Payment_ID // Reference the payment ID
                    };

                    _context.Outstanding_Payments.Add(outstandingPayment);
                }

                _context.SaveChanges();
            }

            return Ok("Billing cycle simulated.");
        }


        private PaymentStatus CalculateExpectedPayments(Contract contract, int memberId)
        {
            var paymentStatus = new PaymentStatus();
            var currentDate = DateTime.Now;
            var subscriptionDate = contract.Subscription_Date;
            var monthlyFee = GetMonthlyFee(contract.Contract_Type_ID);

            // Only consider payments for subscriptions, where Order_ID is null
            var paymentsMade = _context.Payments
                .Where(p => p.Contract_ID == contract.Contract_ID && p.Order_ID == null)
                .ToList();

            paymentStatus.TotalSumPaid = paymentsMade.Sum(p => p.Amount);

            var outstandingPayments = _context.Outstanding_Payments
                .Where(op => op.Member_ID == memberId && op.Amount_Due > 0)
                .Sum(op => op.Amount_Due);

            paymentStatus.OutstandingPayment = outstandingPayments;
            paymentStatus.MonthlyFeeDue = monthlyFee;
            paymentStatus.HasPaid = outstandingPayments == 0;

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


        [HttpPost]
        [Route("SendBlockEmail")]
        public async Task<IActionResult> SendBlockEmail([FromBody] BlockEmailRequestViewModel model)
        {
            var client = new SendGridClient(_configuration["SendGrid:ApiKey"]);
            var from = new EmailAddress(_configuration["SendGrid:SenderEmail"], _configuration["SendGrid:SenderName"]);
            var subject = "Failure To Honour Debit Order";
            var to = new EmailAddress(model.Email);

            var htmlContent = $"<strong>Dear {model.Name}</strong><br><br>" +
                              $"Due to your unresolved fee of ZAR {model.AmountDue} for {model.NextPaymentDate:yyyy/MM/dd}, your membership has been temporarily blocked until further notice. Note you are no longer eligible to login as of {DateTime.Now:yyyy/MM/dd}.<br><br>" +
                              $"Failure to complete more than three  consecutive months payment can result in being suspended and subsequently banned if unresolved within two months from that point of notice, futhermore note all Gym expulsions are final, indefinite and irreversible.<br><br>" +
                              $"Ensure you resolve your contractual monthly payment due and reply to this email by sending a valid Proof of Payment for your membership to resume and for your reactivation.<br><br>" +
                              $"For any queries or disputes:<br>" +
                              $"AVSFitness - 085 345 2443<br><br>" +
                              $"Kind Regards AVSFitness Admin";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, htmlContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            var responseBody = await response.Body.ReadAsStringAsync();
            Console.WriteLine($"SendGrid Response: {responseBody}");

            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                return Ok("Block email sent.");
            }
            else
            {
                return StatusCode((int)response.StatusCode, $"Error sending block email: {responseBody}");
            }
        }

        [HttpPost]
        [Route("SendReactivateEmail")]
        public async Task<IActionResult> SendReactivateEmail([FromBody] ReactivateEmailRequestViewModel model)
        {
            var client = new SendGridClient(_configuration["SendGrid:ApiKey"]);
            var from = new EmailAddress(_configuration["SendGrid:SenderEmail"], _configuration["SendGrid:SenderName"]);
            var subject = "Reactivated: Late Payment Amended";
            var to = new EmailAddress(model.Email);

            var htmlContent = $"<strong>Dear {model.Name},</strong><br><br>" +
                              $"Thank you for resolving your late payment of ZAR {model.AmountPaid}. Your membership has been reactivated and you are now eligible to login again as of {DateTime.Now:yyyy/MM/dd}.<br><br>" +
                              $"For any queries or login issues:<br>" +
                              $"AVSFitness - 085 345 2443<br><br>" +
                              $"Kind Regards,";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, htmlContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            var responseBody = await response.Body.ReadAsStringAsync();
            Console.WriteLine($"SendGrid Response: {responseBody}");

            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                return Ok("Reactivate email sent.");
            }
            else
            {
                return StatusCode((int)response.StatusCode, $"Error sending reactivate email: {responseBody}");
            }
        }


        //Changed_By Methods
        private async Task<string> GetChangedByAsync()
        {
            var userId = User.FindFirstValue("userId");
            if (userId == null)
            {
                return "Unknown"; // Default value if userId is not available
            }

            // Convert userId to integer
            if (!int.TryParse(userId, out var parsedUserId))
            {
                return "Unknown";
            }

            // Retrieve the user
            var user = await _context.Users.FindAsync(parsedUserId);
            if (user == null)
            {
                return "Unknown";
            }

            // Check associated roles
            var owner = await _context.Owners.FirstOrDefaultAsync(o => o.User_ID == user.User_ID);
            if (owner != null)
            {
                return $"{owner.User.Name} {owner.User.Surname} (Owner)";
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.User_ID == user.User_ID);
            if (employee != null)
            {
                return $"{employee.User.Name} {employee.User.Surname} (Employee)";
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == user.User_ID);
            if (member != null)
            {
                return $"{member.User.Name} {member.User.Surname} (Member)";
            }

            return "Unknown";
        }
    }

    public class BlockEmailRequestViewModel
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public decimal AmountDue { get; set; }
        public DateTime NextPaymentDate { get; set; }
    }

    public class ReactivateEmailRequestViewModel
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime NextPaymentDate { get; set; }
    }

    public class SubscriptionStatusViewModel
    {
        public int Member_ID { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Membership_Status_Description { get; set; }
        public decimal Monthly_Fee_Due { get; set; }
        public decimal Outstanding_Payment { get; set; }
        public bool Has_Paid { get; set; }
        public decimal Total_Sum_Paid { get; set; }
        public DateTime Next_Expected_Payment_Date { get; set; }
    }

    public class PaymentStatus
    {
        public decimal TotalSumPaid { get; set; }
        public decimal MonthlyFeeDue { get; set; }
        public decimal OutstandingPayment { get; set; }
        public bool HasPaid { get; set; }
        public int PaymentCount { get; set; }
        public DateTime NextExpectedPaymentDate { get; set; }
    }

}
