using av_motion_api.Data;
using av_motion_api.Interfaces;
using av_motion_api.Models;
using av_motion_api.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace av_motion_api.Controllers
{
    [Route("api/attendanceList/[controller]")]
    [ApiController]
    public class AttendanceListController : ControllerBase
    {
        // GET: api/<AttendanceListController>
        private readonly AppDbContext _appContext;
        private readonly IRepository _repository;
        public AttendanceListController(AppDbContext _context, IRepository repository)
        {

            _appContext = _context;
            _repository = repository;

        }
        // GET: api/<AttendanceListController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AttendanceListViewModel>>> GetAttendanceLists()
        {
            var attendance_Lists = await _repository.GenerateAttendanceLists();

            return attendance_Lists;
        }

        // GET api/<AttendanceListController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<IEnumerable<AttendanceListViewModel>>> GetAttendanceList(int id)
        {
            if (_appContext.Attendance_Lists == null)
            {
                return NotFound();
            }
            var attendance_List = await _repository.GenerateAttendanceList(id);
            if (attendance_List == null)
            {
                return NotFound();
            }

            return attendance_List;
        }

    

        // PUT api/<AttendanceListController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAttendance(int id, [FromBody] AttendanceListViewModel attendance_List)
        {
            var attendanceListEntity = await _appContext.Attendance_Lists.FindAsync(id);
            if (attendanceListEntity == null)
            {
                return NotFound();
            }

            // Preserve the original values for audit purposes
            var originalMembersPresent = attendanceListEntity.Members_Present;
            var originalMembersAbsent = attendanceListEntity.Members_Absent;

            attendanceListEntity.Members_Present = attendance_List.membersPresent;
            attendanceListEntity.Members_Absent = attendance_List.membersAbsent;
            //  attendanceListEntity.Booking_ID = attendance_List.bookingID;

            try
            {
                _appContext.Attendance_Lists.Update(attendanceListEntity);
                await _appContext.SaveChangesAsync();

                // Audit Trail - after successful save
                var changedBy = await GetChangedByAsync();
                var auditChanges = new List<string>();

                if (originalMembersPresent != attendanceListEntity.Members_Present)
                {
                    auditChanges.Add($"Members Present changed from '{originalMembersPresent}' to '{attendanceListEntity.Members_Present}'");
                }

                if (originalMembersAbsent != attendanceListEntity.Members_Absent)
                {
                    auditChanges.Add($"Members Absent changed from '{originalMembersAbsent}' to '{attendanceListEntity.Members_Absent}'");
                }

                if (auditChanges.Any())
                {
                    var auditTrail = new Audit_Trail
                    {
                        Transaction_Type = "UPDATE",
                        Critical_Data = $"Attendance list updated: ID '{id}', {string.Join(", ", auditChanges)}",
                        Changed_By = changedBy,
                        Table_Name = nameof(Attendance_List),
                        Timestamp = DateTime.UtcNow
                    };

                    _appContext.Audit_Trails.Add(auditTrail);
                    await _appContext.SaveChangesAsync();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AttendanceListExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE api/<AttendanceListController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Deleteattendance(int id)
        {
            if (_appContext.Attendance_Lists == null)
            {
                return NotFound();
            }
            var attendance_List = await _appContext.Attendance_Lists.FindAsync(id);
            if (attendance_List == null)
            {
                return NotFound();
            }

            _appContext.Attendance_Lists.Remove(attendance_List);
            await _appContext.SaveChangesAsync();

            // Audit Trail - after successful delete
            var changedBy = await GetChangedByAsync();
            var auditTrail = new Audit_Trail
            {
                Transaction_Type = "DELETE",
                Critical_Data = $"Attendance list deleted: ID '{id}', Members Present: '{attendance_List.Members_Present}', Members Absent: '{attendance_List.Members_Absent}'",
                Changed_By = changedBy,
                Table_Name = nameof(Attendance_List),
                Timestamp = DateTime.UtcNow
            };

            _appContext.Audit_Trails.Add(auditTrail);
            await _appContext.SaveChangesAsync();

            return NoContent();
        }

        private bool AttendanceListExists(int id)
        {
            return (_appContext.Attendance_Lists?.Any(e => e.Attendance_ID == id)).GetValueOrDefault();
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
            var user = await _appContext.Users.FindAsync(parsedUserId);
            if (user == null)
            {
                return "Unknown";
            }

            // Check associated roles
            var owner = await _appContext.Owners.FirstOrDefaultAsync(o => o.User_ID == user.User_ID);
            if (owner != null)
            {
                return $"{owner.User.Name} {owner.User.Surname} (Owner)";
            }

            var employee = await _appContext.Employees.FirstOrDefaultAsync(e => e.User_ID == user.User_ID);
            if (employee != null)
            {
                return $"{employee.User.Name} {employee.User.Surname} (Employee)";
            }

            var member = await _appContext.Members.FirstOrDefaultAsync(m => m.User_ID == user.User_ID);
            if (member != null)
            {
                return $"{member.User.Name} {member.User.Surname} (Member)";
            }

            return "Unknown";
        }
    }
}
