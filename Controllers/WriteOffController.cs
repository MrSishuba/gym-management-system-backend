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
    [Route("api/[controller]")]
    [ApiController]
    public class WriteOffController : ControllerBase
    {
        private readonly AppDbContext _appContext;
        public readonly IRepository _repository;
        public WriteOffController(AppDbContext _context, IRepository repository)
        {

            _appContext = _context;
            _repository = repository;
        }
        // GET: api/<WriteOffController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WriteoffViewModel>>> GetWriteOffs()
        {
           
            var writeOffs = await _repository.GetWriteOffs();

            return Ok(writeOffs);
        }

        // GET api/<WriteOffController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<WriteoffViewModel>> GetWriteOff(int id)
        {
            if (_appContext.Write_Offs == null)
            {
                return NotFound();
            }
            var wrteOff = await _repository.GetWriteOff(id);
            if (wrteOff == null)
            {
                return NotFound();
            }

            return Ok(wrteOff);
        }

       // POST api/<WriteOffController>
        [HttpPost]
        public async Task<ActionResult<Write_Off>> PostWriteOff([FromBody] WriteoffViewModel writeOff)
        {
            
            var inventory = await _appContext.Inventory.FindAsync(writeOff.Inventory_ID);

            if (inventory != null && inventory.Inventory_Item_Quantity < writeOff.Write_Off_Quantity)
            {

                return BadRequest("Writeoff quantity is larger than number of items");
            }

            var newWriteOff = new Write_Off()
            {
                Date = DateTime.Now,
                Write_Off_Reason = writeOff.Write_Off_Reason,
                Inventory_ID = writeOff.Inventory_ID,
                Write_Off_Quantity = writeOff.Write_Off_Quantity,

            };



            try
            {
                _appContext.Write_Offs.Add(newWriteOff);
                await _appContext.SaveChangesAsync();


                inventory.Inventory_Item_Quantity = inventory.Inventory_Item_Quantity - writeOff.Write_Off_Quantity;
                // Update the associated product's quantity
                var product = await _appContext.Products.FindAsync(inventory.Product_ID);
                if (product != null)
                {
                    product.Quantity = product.Quantity - writeOff.Write_Off_Quantity;
                }
                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"New Write-Off created: Inventory ID '{newWriteOff.Inventory_ID}', Write-Off Quantity '{newWriteOff.Write_Off_Quantity}', Reason '{newWriteOff.Write_Off_Reason}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Write_Off),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(auditTrail);
                await _appContext.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                return BadRequest("Failed to create Writeoff. Please try again.");
            }
           

            return Ok(newWriteOff);
        }

        //PUT api/<WriteOffController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        // DELETE api/<WriteOffController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}


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
