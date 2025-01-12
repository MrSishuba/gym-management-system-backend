using av_motion_api.Data;
using av_motion_api.Models;
using av_motion_api.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("GetPayments")]
        public async Task<IActionResult> GetPayments()
        {
            var payments = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.Member)
                    .ThenInclude(m => m.User)
                .Include(p => p.Payment_Type)
                .Where(p => p.Order_ID != null)
                .Select(p => new
                {
                    p.Payment_ID,
                    MemberName = p.Order.Member.User.Name,
                    p.Amount,
                    p.Payment_Date,
                    PaymentTypeName = p.Payment_Type.Payment_Type_Name
                })
                .ToListAsync();

            return Ok(payments);
        }

        [HttpGet]
        [Route("GetContractByMemberId/{memberId}")]
        public async Task<IActionResult> GetContractByMemberId(int memberId)
        {
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Member_ID == memberId);

            if (contract == null)
            {
                return NotFound("Contract not found for the member.");
            }

            return Ok(contract);
        }


        [HttpPost]
        [Route("CreatePayment")]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentViewModel payment)
        {
            if (payment == null)
            {
                return BadRequest("Invalid payment data.");
            }

            // Retrieve the order associated with the payment
            var order = await _context.Orders
                .Include(o => o.Member)
                .FirstOrDefaultAsync(o => o.Order_ID == payment.Order_ID);

            if (order == null)
            {
                return NotFound("Order not found.");
            }

            // Retrieve the contract associated with the member
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Member_ID == order.Member_ID);

            var newPayment = new Payment
            {
                Amount = payment.Amount,
                Payment_Date = DateTime.Now,
                Order_ID = payment.Order_ID,
                Contract_ID = contract.Contract_ID,
                Payment_Type_ID = payment.Payment_Type_ID
            };

            _context.Payments.Add(newPayment);
            await _context.SaveChangesAsync();

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail for creating payment
            var paymentAudit = new Audit_Trail
            {
                Transaction_Type = "INSERT",
                Critical_Data = $"Payment created: For Order_ID '{newPayment.Order_ID}', Amount '{newPayment.Amount}'",
                Changed_By = changedBy,
                Table_Name = nameof(Payment),
                Timestamp = DateTime.UtcNow
            };

            _context.Audit_Trails.Add(paymentAudit);
            await _context.SaveChangesAsync();  // Ensure the audit trail is saved

            return Ok(newPayment);
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
}
