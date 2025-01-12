using av_motion_api.Data;
using av_motion_api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscountController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DiscountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("GetDiscount")]
        public IActionResult GetDiscount()
        {
            var discount = _context.Discounts.FirstOrDefault();
            if (discount == null)
                return NotFound();
            return Ok(discount);
        }

        [HttpPut]
        [Route("UpdateDiscount/{id}")]
        public async Task <IActionResult> UpdateDiscount(int id, [FromBody] Discount discount)
        {
            var existingDiscount = _context.Discounts.Find(id);
            if (existingDiscount == null)
                return NotFound();

            // Get current user's information for audit trail
            var changedBy = await GetChangedByAsync();

            // Log audit trail before update
            var audit = new Audit_Trail
            {
                Transaction_Type = "UPDATE",
                Critical_Data = $"Discount Updated From '{existingDiscount.Discount_Percentage}%' to '{discount.Discount_Percentage}%'",
                Changed_By = changedBy,
                Table_Name = nameof(Discount),
                Timestamp = DateTime.UtcNow
            };

            existingDiscount.Discount_Code = GenerateDiscountCode();
            existingDiscount.Discount_Percentage = discount.Discount_Percentage;
            existingDiscount.Discount_Date = DateTime.Now;
            existingDiscount.End_Date = DateTime.Now.AddDays(30);

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Add audit trail entry after update
            await _context.Audit_Trails.AddAsync(audit);
            await _context.SaveChangesAsync();

            return Ok(existingDiscount);
        }

        private string GenerateDiscountCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            return $"{code.Substring(0, 3)}-{code.Substring(3, 3)}";
        }

        [HttpGet]
        [Route("ValidateDiscount/{code}")]
        public IActionResult ValidateDiscount(string code)
        {
            var discount = _context.Discounts
                .Where(d => d.Discount_Code == code && d.End_Date > DateTime.Now)
                .FirstOrDefault();

            if (discount == null)
            {
                return NotFound("Invalid or expired discount code.");
            }

            return Ok(new { discount.Discount_Percentage });
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
