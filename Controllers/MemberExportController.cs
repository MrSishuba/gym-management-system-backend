using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using av_motion_api.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using av_motion_api.Data;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberExportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MemberExportController(AppDbContext context)
        {
            _context = context;
        }

        // Export members to JSON
        [HttpGet("ExportMembers")]
        public async Task<IActionResult> ExportMembersToJson()
        {
            // Fetch members with User_Type_ID = 3 and their related data
            var members = await _context.Members
                .Include(m => m.User)
                .Include(m => m.Membership_Status)
                .Where(m => m.User.User_Type_ID == 3)
                .Select(m => new
                {
                    MemberID = m.Member_ID,
                    UserName = m.User.Name,
                    UserSurname = m.User.Surname,
                    IDNumber = m.User.ID_Number,
                    PhysicalAddress = m.User.Physical_Address,
                    DateOfBirth = m.User.Date_of_Birth,
                    MembershipStatus = m.Membership_Status.Membership_Status_Description,
                    Contracts = _context.Contracts
                        .Where(c => c.Member_ID == m.Member_ID)
                        .Select(c => new
                        {
                            ContractID = c.Contract_ID,
                            SubscriptionDate = c.Subscription_Date,
                            ExpiryDate = c.Expiry_Date,
                            ApprovalStatus = c.Approval_Status,
                            ApprovalBy = c.Approval_By,
                            IsTerminated = c.IsTerminated,
                            ContractType = c.Contract_Type.Contract_Type_Name
                        })
                        .ToList()
                })
                .ToListAsync();

            // Serialize data to JSON
            var json = JsonConvert.SerializeObject(members, Formatting.Indented);

            // Create JSON file path
            var fileName = "MembersExport.json";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Exports", fileName);

            // Ensure the directory exists
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Exports")))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Exports"));
            }

            // Write JSON data to file
            await System.IO.File.WriteAllTextAsync(filePath, json);

            // Return file as download
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/json", fileName);
        }
    }
}
