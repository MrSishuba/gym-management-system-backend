using av_motion_api.Data;
using av_motion_api.Interfaces;
using av_motion_api.Models;
using av_motion_api.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace av_motion_api.Controllers
{
    [Route("api/inspection/[controller]")]
    [ApiController]
    public class InspectionController : ControllerBase
    {
        // GET: api/<InspectionController>
        private readonly AppDbContext _appContext;
        public readonly IRepository _repository;
        public InspectionController(AppDbContext _context, IRepository repository)
        {

            _appContext = _context;
            _repository = repository;
        }
        // GET: api/<InspectionController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InspectionViewModel>>> GetInspection()
        {
            var inspections = await _repository.GetInspectionsDetails();

            return Ok(inspections);
        }

        // GET api/<InspectionController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<InspectionViewModel>> GetInspection(int id)
        {
            if (_appContext.Inspection == null)
            {
                return NotFound();
            }
            var inspection = await _repository.GetInspectionDetails(id);
            if (inspection == null)
            {
                return NotFound();
            }

            return Ok(inspection);
        }

        // POST api/<InspectionController>
        [HttpPost]
        public async Task<ActionResult<Inspection>> PostInspection([FromBody] CreateInspectionViewModel inspections)
        {
            var inspectionEntity = new Inspection()
            {
                Inspection_Date = inspections.Inspection_Date,
                Inspection_Notes = inspections.inspection_notes,
                Equipment_ID = inspections.equipment_id,
                Inventory_ID = inspections.inventory_id,
                Inspection_Type_ID = inspections.inspection_type_id,
                Inspection_Status_ID = inspections.inspection_status_id,


            };


            try
            {
                _appContext.Inspection.Add(inspectionEntity);
                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                string entityType = inspectionEntity.Equipment_ID.HasValue ? "Equipment" : "Inventory";
                string entityId = inspectionEntity.Equipment_ID.HasValue ? inspectionEntity.Equipment_ID.ToString() : inspectionEntity.Inventory_ID.ToString();

                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"Inspection created for {entityType}: ID '{entityId}', Notes '{inspectionEntity.Inspection_Notes}', Date '{inspectionEntity.Inspection_Date}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Inspection),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(auditTrail);
                await _appContext.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                return BadRequest("Unable to create Inspection. Please try again.");
            }
            
            return Ok(inspectionEntity);
        }

        // PUT api/<InspectionController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutInspection(int id, [FromBody] InspectionViewModel inspection)
        {
            var inspectionEntity = await _appContext.Inspection.FindAsync(id);
            if (inspectionEntity == null)
            {
                return NotFound();
            }

            inspectionEntity.Inspection_Date = inspection.Inspection_Date;
            inspectionEntity.Inspection_Notes = inspection.inspection_notes;
            inspectionEntity.Equipment_ID = inspection.equipment_id;
            inspectionEntity.Inventory_ID = inspection.inventory_id;
            inspectionEntity.Inspection_Type_ID = inspection.inspection_type_id;
            inspectionEntity.Inspection_Status_ID = inspection.inspection_status_id;



            try
            {
                _appContext.Inspection.Update(inspectionEntity);
                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditChanges = new List<string>();

                // Compare and record changes
                if (inspectionEntity.Inspection_Date != inspection.Inspection_Date)
                {
                    auditChanges.Add($"Date from '{inspectionEntity.Inspection_Date}' to '{inspection.Inspection_Date}'");
                }
                if (inspectionEntity.Inspection_Notes != inspection.inspection_notes)
                {
                    auditChanges.Add($"Notes from '{inspectionEntity.Inspection_Notes}' to '{inspection.inspection_notes}'");
                }
                if (inspectionEntity.Equipment_ID != inspection.equipment_id)
                {
                    auditChanges.Add($"Equipment ID from '{inspectionEntity.Equipment_ID}' to '{inspection.equipment_id}'");
                }
                if (inspectionEntity.Inventory_ID != inspection.inventory_id)
                {
                    auditChanges.Add($"Inventory ID from '{inspectionEntity.Inventory_ID}' to '{inspection.inventory_id}'");
                }
                if (inspectionEntity.Inspection_Type_ID != inspection.inspection_type_id)
                {
                    auditChanges.Add($"Type ID from '{inspectionEntity.Inspection_Type_ID}' to '{inspection.inspection_type_id}'");
                }
                if (inspectionEntity.Inspection_Status_ID != inspection.inspection_status_id)
                {
                    auditChanges.Add($"Status ID from '{inspectionEntity.Inspection_Status_ID}' to '{inspection.inspection_status_id}'");
                }

                if (auditChanges.Any())
                {
                    var auditTrail = new Audit_Trail
                    {
                        Transaction_Type = "UPDATE",
                        Critical_Data = $"Inspection updated: ID '{inspection.Inspection_ID}', {string.Join(", ", auditChanges)}",
                        Changed_By = changedBy,
                        Table_Name = nameof(Inspection),
                        Timestamp = DateTime.UtcNow
                    };

                    _appContext.Audit_Trails.Add(auditTrail);
                    await _appContext.SaveChangesAsync();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InspectionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    return BadRequest("Unable to update Inspection. Please try again.");
                }
            }

            return NoContent();
        }

        // DELETE api/<InspectionController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInspection(int id)
        {
            if (_appContext.Inspection == null)
            {
                return NotFound();
            }
            var inspection = await _appContext.Inspection.FindAsync(id);
            if (inspection == null)
            {
                return NotFound();
            }

            try
            {
                _appContext.Inspection.Remove(inspection);
                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                string entityType = inspection.Equipment_ID.HasValue ? "Equipment" : "Inventory";
                string entityId = inspection.Equipment_ID.HasValue ? inspection.Equipment_ID.ToString() : inspection.Inventory_ID.ToString();

                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "DELETE",
                    Critical_Data = $"Inspection deleted for {entityType}: ID '{entityId}', Notes '{inspection.Inspection_Notes}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Inspection),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(auditTrail);
                await _appContext.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                return BadRequest("Unable to delete inspection");
            }
          
            return NoContent();
        }

        private bool InspectionExists(int id)
        {
            return (_appContext.Inspection?.Any(e => e.Inspection_ID == id)).GetValueOrDefault();
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
