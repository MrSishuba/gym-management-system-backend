using av_motion_api.Data;
using av_motion_api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VATController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VATController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("GetVAT")]
        public IActionResult GetVAT()
        {
            var vat = _context.VAT.FirstOrDefault();
            if (vat == null)
                return NotFound();
            return Ok(vat);
        }

        [HttpPut]
        [Route("UpdateVAT/{id}")]
        public async Task <IActionResult> UpdateVAT(int id, [FromBody] VAT vat)
        {
            var existingVAT = _context.VAT.Find(id);
            if (existingVAT == null)
                return NotFound();

            // Get current user's information for audit trail
            var changedBy = await GetChangedByAsync();

            // Log audit trail
            var audit = new Audit_Trail
            {
                Transaction_Type = "UPDATE",
                Critical_Data = $"VAT Updated From '{existingVAT.VAT_Percentage}%' to '{vat.VAT_Percentage}%'",
                Changed_By = changedBy,
                Table_Name = nameof(VAT),
                Timestamp = DateTime.UtcNow
            };

            existingVAT.VAT_Percentage = vat.VAT_Percentage;
            existingVAT.VAT_Date = DateTime.Now;

            // Save changes
            await _context.SaveChangesAsync();

            // Add audit trail entry
            _context.Audit_Trails.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(existingVAT);
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
