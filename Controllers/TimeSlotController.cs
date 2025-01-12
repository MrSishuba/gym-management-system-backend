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
    [Route("api/bookingtimeslot/[controller]")]
    [ApiController]
    public class TimeSlotController : ControllerBase
    {
        // GET: api/<TimeSlotController>
        private readonly AppDbContext _appContext;
        private readonly IRepository _repository;
        public TimeSlotController(AppDbContext _context, IRepository repository)
        {

            _appContext = _context;
            _repository = repository;
        }
        // GET: api/<TimeSlotController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimeSlotCalanderViewModel>>> GetTimeSlots()
        {
            var timeSlots = await _repository.GetTimeSlots();

            return timeSlots;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TimeSlotCalanderViewModel>> GetCalanderTimeSlots(int id)
        {
            var timeSlots = await _repository.GetTimeSlot(id);

            return timeSlots;
        }

        [HttpGet("lesson-plan/{id}")]
        public async Task<ActionResult<IEnumerable<TimeSlotCalanderViewModel>>> GetCalanderTimeSlotsByLessonPlan(int id)
        {
            var timeSlots = await _repository.GetTimeSlotByLessonPlan(id);

            return timeSlots;
        }

        [HttpGet("by-date/{date}")]
        public async Task<ActionResult<IEnumerable<TimeSlotCalanderViewModel>>> GetTimeSLotByDate(string date)
        {
            var parsedDate = DateTime.Parse(date);

            var timeSlots = await _repository.GetSlotByDate(parsedDate);

            return timeSlots;
        }

        // POST api/<TimeSlotController>
        [HttpPost]
        public async Task<ActionResult<Time_Slot>> PostTimeSlot([FromBody] TimeSlotViewModel timeslot)
        {
            var timeSlots = await _repository.GetSlotByDateAndTime(timeslot.date, timeslot.lesson_Plan_ID);

            if (timeSlots.Any())
            {
                return BadRequest($"There is a plan scheduled on {timeslot.date} for this lesson plan");
            }

            if (timeslot.date < DateTime.Now)
            {
                return BadRequest("Cannot create a slot for this date");
            }
            // return BadRequest($"There is a plan scheduled on {timeslot.date} at this time for lesson plan {timeslot.lesson_Plan_ID}"); }
            try
            {
                var timeSlotEntity = new Time_Slot
                {
                    Slot_Date = timeslot.date,
                    Slot_Time = timeslot.time,
                    Availability = true,
                    Employee_ID = timeslot.employee_ID,
                    Lesson_Plan_ID = timeslot.lesson_Plan_ID
                };

                _appContext.Time_Slots.Add(timeSlotEntity);
                await _appContext.SaveChangesAsync();

                var attendance_List = new Attendance_List
                {
                    Number_Of_Bookings = 0,
                    Members_Present = 0,
                    Members_Absent = 0,
                    Time_Slot_ID = timeSlotEntity.Time_Slot_ID
                };

                _appContext.Attendance_Lists.Add(attendance_List);
                await _appContext.SaveChangesAsync();

                //else
                //{
                //    return BadRequest($"There is a plan scheduled on {timeslot.date} at this time for lesson plan {timeslot.lesson_Plan_ID}");
                //}

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "CREATE",
                    Critical_Data = $"New time slot created: ID '{timeSlotEntity.Time_Slot_ID}', Date '{timeSlotEntity.Slot_Date}', Time '{timeSlotEntity.Slot_Time}', Employee_ID '{timeSlotEntity.Employee_ID}', Lesson_Plan_ID '{timeSlotEntity.Lesson_Plan_ID}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Time_Slot),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(auditTrail);
                await _appContext.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                return BadRequest("Failed to create slot. Please try again");
            }

               
            return Ok(timeslot);
        }

        // PUT api/<TimeSlotController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTimeSlot(int id, [FromBody] TimeSlotViewModel timeslot)
        {
            var timeslotEntity = await _appContext.Time_Slots.FindAsync(id);

            if (timeslotEntity == null)
            {
                return NotFound();
            }

            // Retrieve old state for auditing purposes
            var oldSlotDate = timeslotEntity.Slot_Date;
            var oldSlotTime = timeslotEntity.Slot_Time;
            var oldAvailability = timeslotEntity.Availability;
            var oldEmployeeId = timeslotEntity.Employee_ID;
            var oldLessonPlanId = timeslotEntity.Lesson_Plan_ID;

            var timeSlots = await _repository.GetSlotByDateAndTime(timeslot.date, timeslot.lesson_Plan_ID);


            if (timeSlots.Any(slot => slot.TimeSlotId != timeslot.timeSlotID))
            {
                return BadRequest($"There is already a plan scheduled on {timeslot.date} for this lesson plan");
            }

            if (timeslot.date < DateTime.Now)
            {
                return BadRequest("Cannot create a slot for this date");
            }

            timeslotEntity.Slot_Date = timeslot.date;
                timeslotEntity.Slot_Time = timeslot.time;
                timeslotEntity.Availability = timeslot.Availability;
                timeslotEntity.Lesson_Plan_ID = timeslot.lesson_Plan_ID;
                timeslotEntity.Employee_ID = timeslot.employee_ID;
                try
                {
                    _appContext.Time_Slots.Update(timeslotEntity);
                    await _appContext.SaveChangesAsync();

                    // Audit Trail
                    var changedBy = await GetChangedByAsync();
                    var auditChanges = new List<string>();

                    if (oldSlotDate != timeslot.date)
                    {
                        auditChanges.Add($"Slot Date changed from '{oldSlotDate}' to '{timeslot.date}'");
                    }

                    if (oldSlotTime != timeslot.time)
                    {
                        auditChanges.Add($"Slot Time changed from '{oldSlotTime}' to '{timeslot.time}'");
                    }

                    if (oldAvailability != timeslot.Availability)
                    {
                        auditChanges.Add($"Availability changed from '{oldAvailability}' to '{timeslot.Availability}'");
                    }

                    if (oldEmployeeId != timeslot.employee_ID)
                    {
                        auditChanges.Add($"Employee_ID changed from '{oldEmployeeId}' to '{timeslot.employee_ID}'");
                    }

                    if (oldLessonPlanId != timeslot.lesson_Plan_ID)
                    {
                        auditChanges.Add($"Lesson_Plan_ID changed from '{oldLessonPlanId}' to '{timeslot.lesson_Plan_ID}'");
                    }

                    if (auditChanges.Any())
                    {
                        var auditTrail = new Audit_Trail
                        {
                            Transaction_Type = "UPDATE",
                            Critical_Data = $"Time slot ID '{id}' updated: {string.Join(", ", auditChanges)}",
                            Changed_By = changedBy,
                            Table_Name = nameof(Time_Slot),
                            Timestamp = DateTime.UtcNow
                        };

                        _appContext.Audit_Trails.Add(auditTrail);
                        await _appContext.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TimeSlotExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                    return BadRequest("Failed to update slot. Please try again"); ;
                    }
                }
            
            

            return NoContent();
        }

        // DELETE api/<TimeSlotController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTimeSlot(int id)
        {
            if (_appContext.Time_Slots == null)
            {
                return NotFound();
            }
            var timeSlot = await _appContext.Time_Slots.FindAsync(id);
            if (timeSlot == null)
            {
                return NotFound();
            }
            var bookingtimeslot = await _appContext.Booking_Time_Slots.FirstOrDefaultAsync(slot => slot.Time_Slot_ID == id);
            if (bookingtimeslot != null)
            {
                return BadRequest("Cannot delete this timeslot as it has bookings");
            }


            // Retrieve old state for auditing purposes
            var oldSlotDate = timeSlot.Slot_Date;
            var oldSlotTime = timeSlot.Slot_Time;
            var oldAvailability = timeSlot.Availability;
            var oldEmployeeId = timeSlot.Employee_ID;
            var oldLessonPlanId = timeSlot.Lesson_Plan_ID;

            var existingAttendanceListEntry = _appContext.Attendance_Lists
                     .FirstOrDefault(a => a.Time_Slot_ID == timeSlot.Time_Slot_ID);

            try
            {

                if (existingAttendanceListEntry != null)
                {
                    _appContext.Attendance_Lists.Remove(existingAttendanceListEntry);
                    await _appContext.SaveChangesAsync();
                }


                _appContext.Time_Slots.Remove(timeSlot);
                await _appContext.SaveChangesAsync();






                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "DELETE",
                    Critical_Data = $"Time slot ID '{id}' deleted: Date '{oldSlotDate}', Time '{oldSlotTime}', Employee_ID '{oldEmployeeId}', Lesson_Plan_ID '{oldLessonPlanId}', Availability '{oldAvailability}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Time_Slot),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(auditTrail);
                await _appContext.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                return BadRequest("Failed to create slot. Please try again");
            }
        
           

            return NoContent();
        }

        private bool TimeSlotExists(int id)
        {
            return (_appContext.Time_Slots?.Any(e => e.Time_Slot_ID == id)).GetValueOrDefault();
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
