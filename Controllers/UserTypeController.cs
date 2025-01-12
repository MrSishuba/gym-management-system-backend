using av_motion_api.Data;
using av_motion_api.Models;
using av_motion_api.ViewModels;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [EnableCors("AllowAll")]
    [ApiController]
    public class UserTypeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserTypeController(AppDbContext context)
        {
            _context = context;
        }


        [HttpPost]
        [Route("addUserType")]
        public async Task<IActionResult> AddUserType(UserTypeViewModel utv)
        {
                var userType = new User_Type
                {
                    User_Type_Name = utv.User_Type_Name
                };

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail
                var audit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"User type created: Name '{userType.User_Type_Name}'", // Critical data for create operation
                    Changed_By = changedBy,
                    Table_Name = nameof(User_Type),
                    Timestamp = DateTime.UtcNow
                };

                _context.User_Types.Add(userType);
                await _context.SaveChangesAsync();

                _context.Audit_Trails.Add(audit);
                await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserTypeById), new { id = userType.User_Type_ID }, userType);
        }


        [HttpGet]
        [Route("getAllUserTypes")]
        public async Task<IActionResult> GetAllUserTypes()
        {
            try
            {
                var userTypes = await _context.User_Types.ToListAsync();
                return Ok(userTypes);
            }
            catch (Exception)
            {

                return BadRequest("An Error occured, Please try again");
            }
        }


        [HttpGet]
        [Route("getUserTypeById/{id}")]
        public async Task<IActionResult> GetUserTypeById(int id)
        {
            try
            {
                var userType = await _context.User_Types.FindAsync(id);

                if (userType == null)
                {
                    return NotFound("User_Type not found.");
                }

                return Ok(userType);
            }
            catch (Exception)
            {
                return BadRequest("An Error occured, Please try again");
            }
        }


        [HttpPut]
        [Route("updateUserType/{id}")]
        public async Task<IActionResult> UpdateUserType(int id, [FromBody] UserTypeViewModel utv)
        {
                var userType = await _context.User_Types.FindAsync(id);

                if (userType == null)
                {
                    return NotFound("User_Type not found.");
                }

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail
                var audit = new Audit_Trail
                {
                    Transaction_Type = "UPDATE",
                    Critical_Data = $"User type updated from '{userType.User_Type_Name}' to '{utv.User_Type_Name}'", // Critical data for update operation
                    Changed_By = changedBy,
                    Table_Name = nameof(User_Type),
                    Timestamp = DateTime.UtcNow
                };

                userType.User_Type_Name = utv.User_Type_Name;

                _context.Entry(userType).State = EntityState.Modified;
                _context.Audit_Trails.Add(audit);
                await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpDelete]
        [Route("deleteUserType/{id}")]
        public async Task<IActionResult> DeleteUserType(int id)
        {
                var userType = await _context.User_Types.FindAsync(id);

                if (userType == null)
                {
                    return NotFound("User_Type not found.");
                }

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail
                var audit = new Audit_Trail
                {
                    Transaction_Type = "DELETE",
                    Critical_Data = $"User type deleted: ID '{userType.User_Type_ID}', Name '{userType.User_Type_Name}'", // Critical data for delete operation
                    Changed_By = changedBy,
                    Table_Name = nameof(User_Type),
                    Timestamp = DateTime.UtcNow
                };

                _context.User_Types.Remove(userType);
                _context.Audit_Trails.Add(audit);
                await _context.SaveChangesAsync();

            return NoContent();
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
