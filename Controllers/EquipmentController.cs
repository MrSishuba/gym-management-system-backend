using av_motion_api.Data;
using av_motion_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace av_motion_api.Controllers
{
    [Route("api/equipment/[controller]")]
    [ApiController]
    public class EquipmentController : ControllerBase
    {
        // GET: api/<EqupimentController>
        private readonly AppDbContext _appContext;
        public EquipmentController(AppDbContext _context)
        {

            _appContext = _context;
        }
        // GET: api/<EqupimentController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Equipment>>> GetEquipments()
        {
            var equipments = await _appContext.Equipment.ToListAsync();

            return equipments;
        }

        // GET api/<EqupimentController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Equipment>> GetEquipment(int id)
        {
            if (_appContext.Equipment == null)
            {
                return NotFound();
            }
            var equipemt = await _appContext.Equipment.FindAsync(id);
            if (equipemt == null)
            {
                return NotFound();
            }

            return equipemt;
        }

        // POST api/<EqupimentController>
        [HttpPost]
        public async Task<ActionResult<Equipment>> PostEquipment([FromBody] Equipment equipment)
        {
            if (_appContext.Equipment == null)
            {
                return Problem("Entity set 'AppDbContext.Equipment'  is null.");
            }
            var equupimentPiece = await _appContext.Equipment.FirstOrDefaultAsync(wo => wo.Equipment_Name == equipment.Equipment_Name);

            if (equupimentPiece != null && equupimentPiece.Equipment_ID != equipment.Equipment_ID)
            {
                return BadRequest("A piece of equpiment with this name already exists");
            }
            else
            {
                try
                {
                    _appContext.Equipment.Add(equipment);
                    await _appContext.SaveChangesAsync();

                    // Audit Trail
                    var changedBy = await GetChangedByAsync();
                    var auditTrail = new Audit_Trail
                    {
                        Transaction_Type = "INSERT",
                        Critical_Data = $"New equipment added: ID '{equipment.Equipment_ID}', Name '{equipment.Equipment_Name}, Description '{equipment.Equipment_Description}'",
                        Changed_By = changedBy,
                        Table_Name = nameof(Equipment),
                        Timestamp = DateTime.UtcNow
                    };

                    _appContext.Audit_Trails.Add(auditTrail);
                    await _appContext.SaveChangesAsync();

                }
                catch (Exception ex)
                {
                    return BadRequest("Failed to create Equipment. Please try again");
                }
              
            }
            
            return Ok(equipment);
        }

        // PUT api/<EqupimentController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEquipment(int id, [FromBody] Equipment equipment)
        {
            if (id != equipment.Equipment_ID)
            {
                return BadRequest();
            }

            var existingEquipment = await _appContext.Equipment.AsNoTracking().FirstOrDefaultAsync(e => e.Equipment_ID == id);
            if (existingEquipment == null)
            {
                return NotFound();
            }



            try
            {
                _appContext.Entry(equipment).State = EntityState.Modified;

                var equupimentPiece = await _appContext.Equipment.FirstOrDefaultAsync(wo => wo.Equipment_Name == equipment.Equipment_Name);

                if (equupimentPiece != null && equupimentPiece.Equipment_ID != equipment.Equipment_ID)
                {
                    return BadRequest("A piece of equpiment with this name already exists");
                }
                else
                {

                    try
                    {
                        await _appContext.SaveChangesAsync();

                        // Audit Trail
                        var changedBy = await GetChangedByAsync();
                        var auditChanges = new List<string>();

                        // Compare and record changes
                        if (existingEquipment.Equipment_Name != equipment.Equipment_Name)
                        {
                            auditChanges.Add($"Name from '{existingEquipment.Equipment_Name}' to '{equipment.Equipment_Name}'");
                        }
                        if (existingEquipment.Equipment_Description != equipment.Equipment_Description)
                        {
                            auditChanges.Add($"Description from '{existingEquipment.Equipment_Description}' to '{equipment.Equipment_Description}'");
                        }
                        if (existingEquipment.Size != equipment.Size)
                        {
                            auditChanges.Add($"Size from '{existingEquipment.Size}' to '{equipment.Size}'");
                        }

                        if (auditChanges.Any())
                        {
                            var auditTrail = new Audit_Trail
                            {
                                Transaction_Type = "UPDATE",
                                Critical_Data = $"Equipment updated: ID '{equipment.Equipment_ID}'  {string.Join(", ", auditChanges)}",
                                Changed_By = changedBy,
                                Table_Name = nameof(Equipment),
                                Timestamp = DateTime.UtcNow
                            };

                            _appContext.Audit_Trails.Add(auditTrail);
                            await _appContext.SaveChangesAsync();
                        }
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!EquipmentExists(id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                return BadRequest("Failed to update Equipment. Please try again.");
            }
           
            return NoContent();
        }

        // DELETE api/<EqupimentController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEquipment(int id)
        {
            if (_appContext.Equipment == null)
            {
                return NotFound();
            }
            var equipment = await _appContext.Equipment.FindAsync(id);
            if (equipment == null)
            {
                return NotFound();
            }

            var inspection = await _appContext.Inspection.FirstOrDefaultAsync(slot => slot.Equipment_ID == id);
            if (inspection != null)
            {
                return BadRequest("Cannot delete this equipment.");
            }


            try
            {
                _appContext.Equipment.Remove(equipment);
                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "DELETE",
                    Critical_Data = $"Equipment deleted: ID '{equipment.Equipment_ID}', Name '{equipment.Equipment_Name}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Equipment),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(auditTrail);
                await _appContext.SaveChangesAsync();
            }catch(Exception ex) { 
            
                return BadRequest("Failed to delete equipment. Please try again.");
            
            }
            

            return NoContent();
        }

        private bool EquipmentExists(int id)
        {
            return (_appContext.Equipment?.Any(e => e.Equipment_ID == id)).GetValueOrDefault();
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
