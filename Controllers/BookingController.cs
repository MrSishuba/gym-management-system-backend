using av_motion_api.Data;
using av_motion_api.Interfaces;
using av_motion_api.Models;
using av_motion_api.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace av_motion_api.Controllers
{
    [Route("api/booking/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        // GET: api/<BookingController>
        private readonly AppDbContext _appContext;
        public readonly IRepository _repository;
        public BookingController(AppDbContext _context, IRepository repository)
        {

            _appContext = _context;
            _repository = repository;
        }
        // GET: api/<BookingController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingViewModel>>> GetBookings()
        {
            var bookings = await _repository.GetBookings();
            return bookings;

        }

        [HttpGet("booking-time-slots")]
        public async Task<ActionResult<IEnumerable<Booking_Time_Slot>>> GetBookingTimeSlots()
        {
            var bookingTimeSlots = await _appContext.Booking_Time_Slots.ToListAsync();
            return bookingTimeSlots;

        }

        // GET api/<BookingController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BookingViewModel>> GetMemberBooking(int id, [FromQuery]int memberUserID)
        {
            if (_appContext.Bookings == null)
            {
                return NotFound();
            }
            var booking = await _repository.GetBooking(id, memberUserID);
            if (booking == null)
            {
                return NotFound();
            }

            return booking;
        }

        [HttpGet("memberBookings/{memberUserID}")]
        public async Task<ActionResult<IEnumerable<BookingViewModel>>> GetMemberBookings(int memberUserID)
        {
            if (_appContext.Bookings == null)
            {
                return NotFound();
            }
            var booking = await _repository.GetMemberBookings(memberUserID);
            if (booking == null)
            {
                return NotFound();
            }

            return booking;
        }

        [HttpGet("booking-time-slot/{id}")]
        public async Task<ActionResult<Booking_Time_Slot>> GetBookingTimeSlot(int id)
        {
            if (_appContext.Booking_Time_Slots == null)
            {
                return NotFound();
            }
            var bookingTimeSlot = await _appContext.Booking_Time_Slots.FindAsync(id);
            if (bookingTimeSlot == null)
            {
                return NotFound();
            }
            return bookingTimeSlot;

        }

        // POST api/<BookingController>
        [HttpPost]
        public async Task<ActionResult<Booking>> PostBooking([FromBody] BookingTimeSlotViewModel booking, [FromQuery] int user_ID)
        {
            Booking bookings = new Booking();

            var member = await _appContext.Members.FirstOrDefaultAsync(a => a.User_ID == user_ID);

            int memberID = member != null ? member.Member_ID : 0;

            var newBooking = new Booking
            {
                //Booking_ID = booking.booking_Id,

                Member_ID = memberID,


            };


            try
            {
                _appContext.Bookings.Add(newBooking);
                await _appContext.SaveChangesAsync();


                var timeSLot = await _appContext.Time_Slots.FindAsync(booking.timeSlot_ID);

                if (timeSLot != null)
                {
                    //find the corresponding attendance list id from the attendance list table and increment the number of bookings attribute by 1
                    var attendanceListEntry = _appContext.Attendance_Lists
                         .FirstOrDefault(a => a.Time_Slot_ID == booking.timeSlot_ID);
                    var timeSLotAVailability = _appContext.Time_Slots.FirstOrDefault(a => a.Time_Slot_ID == booking.timeSlot_ID);

                    if (attendanceListEntry != null || attendanceListEntry.Number_Of_Bookings < 20)
                    {
                        attendanceListEntry.Number_Of_Bookings += 1;
                        await _appContext.SaveChangesAsync();
                    }
                    else
                    {

                        return BadRequest("This booking slot is full. Please select another booking");
                    }

                    if (attendanceListEntry.Number_Of_Bookings == 20)
                    {
                        timeSLotAVailability.Availability = false;
                        await _appContext.SaveChangesAsync();
                    }
                }


                var bookingTimeSLot = new Booking_Time_Slot
                {
                    // Number_Of_Bookings = await _repository.GetNumberOfBookings(booking.date, booking.time),
                    Booking_ID = newBooking.Booking_ID,
                    Time_Slot_ID = booking.timeSlot_ID,



                };


                _appContext.Booking_Time_Slots.Add(bookingTimeSLot);
                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"New booking created: Booking ID '{newBooking.Booking_ID}', Member ID '{newBooking.Member_ID}', Time Slot ID '{booking.timeSlot_ID}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Booking),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(auditTrail);
                await _appContext.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                return BadRequest("Failed to create Booking. Please try again.");
            }

           
            return Ok(newBooking);
        }



        // PUT api/<BookingController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBooking(int id, [FromBody] BookingTimeSlotViewModel booking)
        {
            var bookingEntity = await _appContext.Bookings.FindAsync(id);

            if (bookingEntity == null)
            {
                return NotFound();
            }

            var existingBookingSlot = await _appContext.Booking_Time_Slots.FirstOrDefaultAsync(al => al.Booking_ID == id);
            bookingEntity.Booking_ID = id;

            var oldTimeSlotID = existingBookingSlot?.Time_Slot_ID;
            var oldAttendanceListEntry = await _appContext.Attendance_Lists.FirstOrDefaultAsync(a => a.Time_Slot_ID == oldTimeSlotID);
            var oldNumberOfBookings = oldAttendanceListEntry?.Number_Of_Bookings ?? 0;

            //checks if the booking slot exists and if the existing booking slot has changed
            if (existingBookingSlot != null && existingBookingSlot.Time_Slot_ID != booking.timeSlot_ID)
            {

                //if it has chnaged it removes the member from their initial attendance list  
                //it then adds them to the new attendance list of thier new booking
                var existingAttendanceListEntry = _appContext.Attendance_Lists
                     .FirstOrDefault(a => a.Time_Slot_ID == existingBookingSlot.Time_Slot_ID);
                existingAttendanceListEntry.Number_Of_Bookings -= 1;
                await _appContext.SaveChangesAsync();


                var newAttendanceListEntry = _appContext.Attendance_Lists
                   .FirstOrDefault(a => a.Time_Slot_ID == booking.timeSlot_ID);
                newAttendanceListEntry.Number_Of_Bookings += 1;
                await _appContext.SaveChangesAsync();


                existingBookingSlot.Time_Slot_ID = booking.timeSlot_ID;
                await _appContext.SaveChangesAsync();

                //resets availability if the member was the last member to book the previous booking
                var timeSLotAVailability = _appContext.Time_Slots.FirstOrDefault(a => a.Time_Slot_ID == existingAttendanceListEntry.Time_Slot_ID);
                if (existingAttendanceListEntry.Number_Of_Bookings < 20)
                {
                    timeSLotAVailability.Availability = true;
                    await _appContext.SaveChangesAsync();
                }
            }


            try
            {
                _appContext.Bookings.Update(bookingEntity);
                await _appContext.SaveChangesAsync();

                // Capture the new state for audit purposes
                var newTimeSlotID = existingBookingSlot?.Time_Slot_ID;
                var newAttendanceListEntry = await _appContext.Attendance_Lists
                    .FirstOrDefaultAsync(a => a.Time_Slot_ID == newTimeSlotID);
                var newNumberOfBookings = newAttendanceListEntry?.Number_Of_Bookings ?? 0;

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditChanges = new List<string>();

                if (oldTimeSlotID != booking.timeSlot_ID)
                {
                    auditChanges.Add($"Time Slot ID changed from '{oldTimeSlotID}' to '{booking.timeSlot_ID}'");
                }

                if (oldAttendanceListEntry != null)
                {
                    auditChanges.Add($"Number of bookings for old time slot ID '{oldTimeSlotID}' changed from '{oldNumberOfBookings}' to '{oldAttendanceListEntry.Number_Of_Bookings}'");
                }

                if (newAttendanceListEntry != null)
                {
                    auditChanges.Add($"Number of bookings for new time slot ID '{booking.timeSlot_ID}' is now '{newNumberOfBookings}'");
                }

                if (auditChanges.Any())
                {
                    var auditTrail = new Audit_Trail
                    {
                        Transaction_Type = "UPDATE",
                        Critical_Data = $"Booking ID '{id}' updated: {string.Join(", ", auditChanges)}",
                        Changed_By = changedBy,
                        Table_Name = nameof(Booking),
                        Timestamp = DateTime.UtcNow
                    };

                    _appContext.Audit_Trails.Add(auditTrail);
                    await _appContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                if (!BookingExists(id))
                {
                    return NotFound();
                }
                else
                {
                    return BadRequest("Failed to update booking. Please try again.");
                }
            }
            return Ok(booking);
        }

        // DELETE api/<BookingController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _appContext.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            try
            {

                var bookingSlot = await _appContext.Booking_Time_Slots.FirstOrDefaultAsync(slot => slot.Booking_ID == id);
                if (bookingSlot != null)
                {

                    _appContext.Booking_Time_Slots.Remove(bookingSlot);
                    await _appContext.SaveChangesAsync();
                }



                //decrements the attendnance list number to refelect the deleted/cancelled booking
                var deletdedAttendanceListEntry = _appContext.Attendance_Lists
                     .FirstOrDefault(a => a.Time_Slot_ID == bookingSlot.Time_Slot_ID);
                deletdedAttendanceListEntry.Number_Of_Bookings -= 1;
                await _appContext.SaveChangesAsync();

                //resets availability if the member was the last member to book the previous booking
                var timeSLotAVailability = _appContext.Time_Slots.FirstOrDefault(a => a.Time_Slot_ID == deletdedAttendanceListEntry.Time_Slot_ID);
                if (deletdedAttendanceListEntry.Number_Of_Bookings < 20)
                {
                    timeSLotAVailability.Availability = true;
                    await _appContext.SaveChangesAsync();
                }

                _appContext.Bookings.Remove(booking);
                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "DELETE",
                    Critical_Data = $"Booking deleted: Booking ID '{booking.Booking_ID}', Member ID '{booking.Member_ID}', Time Slot ID '{bookingSlot.Time_Slot_ID}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Booking),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(auditTrail);
                await _appContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to delete booking. Please try again.");
            }


            return NoContent();
        }

        private bool BookingExists(int id)
        {
            return (_appContext.Bookings?.Any(e => e.Booking_ID == id)).GetValueOrDefault();
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
