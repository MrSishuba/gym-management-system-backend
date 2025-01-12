using av_motion_api.Data;
using av_motion_api.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using av_motion_api.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using av_motion_api.ViewModels;
using Microsoft.AspNetCore.Cors;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [EnableCors("AllowAll")]
    [ApiController]
    public class EmployeeShiftController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmployeeShiftController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Route("StartShift")]
        public async Task<IActionResult> StartShift([FromBody] StartShiftViewModel model)
        {
            var employee = await _context.Employees.FindAsync(model.EmployeeId);
            if (employee == null)
            {
                return NotFound("Employee not found");
            }

            var shift = await _context.Shifts.FindAsync(model.ShiftId);
            if (shift == null)
            {
                return NotFound("Shift not found");
            }

            var employeeShift = new EmployeeShift
            {
                Employee_ID = model.EmployeeId,
                Shift_ID = model.ShiftId,
                Shift_Start_Time = DateTime.UtcNow
            };

            _context.EmployeeShifts.Add(employeeShift);
            await _context.SaveChangesAsync();

            return Ok(new { Status = "Success", Message = "Shift started successfully", EmployeeShiftId = employeeShift.EmployeeShift_ID });
        }

        [HttpPost]
        [Route("EndShift")]
        public async Task<IActionResult> EndShift([FromBody] int employeeShiftId)
        {
            var employeeShift = await _context.EmployeeShifts.FindAsync(employeeShiftId);
            if (employeeShift == null)
            {
                return NotFound("Employee shift not found");
            }

            employeeShift.Shift_End_Time = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Status = "Success", Message = "Shift ended successfully" });
        }

        [HttpGet]
        [Route("GetEmployeeShiftDetails")]
        public async Task<IActionResult> GetEmployeeShiftDetails(int employeeId)
        {
            var employeeShifts = await _context.EmployeeShifts
                .Where(es => es.Employee_ID == employeeId)
                .Select(es => new
                {
                    es.EmployeeShift_ID,
                    es.Shift_ID,
                    es.Shift_Start_Time,
                    es.Shift_End_Time
                })
                .ToListAsync();

            return Ok(employeeShifts);
        }

        [HttpGet]
        [Route("CalculateHoursWorked")]
        public async Task<IActionResult> CalculateHoursWorked(int employeeId, string period)
        {
            DateTime startDate;
            switch (period.ToLower())
            {
                case "daily":
                    startDate = DateTime.UtcNow.Date;
                    break;
                case "weekly":
                    startDate = DateTime.UtcNow.AddDays(-((int)DateTime.UtcNow.DayOfWeek)).Date;
                    break;
                case "monthly":
                    startDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                    break;
                default:
                    return BadRequest("Invalid period specified. Use 'daily', 'weekly', or 'monthly'.");
            }

            var employeeShifts = await _context.EmployeeShifts
                .Where(es => es.Employee_ID == employeeId && es.Shift_Start_Time >= startDate)
                .ToListAsync();

            var totalHoursWorked = employeeShifts
                .Where(es => es.Shift_End_Time.HasValue)
                .Sum(es => (es.Shift_End_Time.Value - es.Shift_Start_Time).TotalHours);

            return Ok(new { EmployeeId = employeeId, TotalHoursWorked = totalHoursWorked });
        }

        [HttpGet]
        [Route("ExportShifts")]
        public async Task<IActionResult> ExportShifts(string format)
        {
            var employeeShifts = await _context.EmployeeShifts.ToListAsync();

            switch (format.ToLower())
            {
                case "json":
                    var jsonData = ExportHelper.ExportShiftsToJson(employeeShifts);
                    return File(Encoding.UTF8.GetBytes(jsonData), "application/json", "shifts.json");

                case "excel":
                    var excelData = ExportHelper.ExportShiftsToExcel(employeeShifts);
                    return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "shifts.xlsx");

                default:
                    return BadRequest("Invalid format specified. Use 'json' or 'excel'.");
            }
        }

        [HttpPost]
        [Route("ImportShiftsFromJson")]
        public async Task<IActionResult> ImportShiftsFromJson(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty.");
            }

            List<EmployeeShift> shifts;
            using (var stream = new StreamReader(file.OpenReadStream()))
            {
                var jsonData = await stream.ReadToEndAsync();
                shifts = JsonConvert.DeserializeObject<List<EmployeeShift>>(jsonData);
            }

            // Start a new transaction
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Enable IDENTITY_INSERT to allow explicit values
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT EmployeeShifts ON");

                    foreach (var shift in shifts)
                    {
                        var existingShift = await _context.EmployeeShifts
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.EmployeeShift_ID == shift.EmployeeShift_ID);

                        if (existingShift == null)
                        {
                            // Insert new records
                            _context.EmployeeShifts.Add(shift);
                        }
                        else
                        {
                            // Update existing records if necessary
                            _context.EmployeeShifts.Update(shift);
                        }
                    }

                    // Save changes to the database
                    await _context.SaveChangesAsync();

                    // Disable IDENTITY_INSERT
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT EmployeeShifts OFF");

                    // Commit the transaction
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    // Rollback the transaction in case of an error
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }

            return Ok("Shifts imported successfully.");
        }

        [HttpPost("ImportShiftsFromExcel")]
        public async Task<IActionResult> ImportShiftsFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty or not provided.");

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var shifts = new List<EmployeeShift>();

                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    try
                    {
                        var shift = new EmployeeShift
                        {
                            Employee_ID = row.Cell(1).GetValue<int>(),
                            Shift_ID = row.Cell(2).GetValue<int>(),
                            Shift_Start_Time = row.Cell(3).GetValue<DateTime>(),
                            Shift_End_Time = row.Cell(4).GetValue<DateTime>()
                        };
                        shifts.Add(shift);
                    }
                    catch (Exception ex)
                    {
                        // Log or handle the error for the specific row
                        Console.WriteLine($"Error processing row {row.RowNumber()}: {ex.Message}");
                        // Optionally, you can return a detailed error message
                        return BadRequest($"Error processing row {row.RowNumber()}: {ex.Message}");
                    }
                }

                _context.EmployeeShifts.AddRange(shifts);
                await _context.SaveChangesAsync();
                return Ok("Shifts imported successfully.");
            }
            catch (Exception ex)
            {
                // Log or handle the general error
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetShiftsByDay")]
        public async Task<IActionResult> GetShiftsByDay()
        {
            var shifts = await _context.Shifts.ToListAsync();

            // Define shift times for weekdays and weekends
            var weekdayStartTime = new TimeSpan(6, 0, 0);
            var weekdayEndTime = new TimeSpan(22, 0, 0);
            var weekendStartTime = new TimeSpan(6, 0, 0);
            var weekendEndTime = new TimeSpan(20, 0, 0);

            var weekdays = shifts
                .Where(s => s.Start_Time >= weekdayStartTime && s.End_Time <= weekdayEndTime)
                .ToList();

            var weekends = shifts
                .Where(s => s.Start_Time >= weekendStartTime && s.End_Time <= weekendEndTime)
                .ToList();

            var categorizedShifts = new
            {
                Weekdays = weekdays,
                Weekends = weekends
            };

            return Ok(categorizedShifts);
        }

    }
}
