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
    public class InventoriesController : ControllerBase
    {
        private readonly AppDbContext _appContext;
        public readonly IRepository _repository;

        public InventoriesController(AppDbContext _context, IRepository repository)
        {

            _appContext = _context;
            _repository = repository;
        }
        // GET: api/<InventoriesController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryViewModel>>> GetInventories()
        {
            var inventories = await _repository.GetInventoryDetails();

            return Ok(inventories);
        }


        // GET api/<InventoriesController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryViewModel>> GetInventoryItem(int id)
        {
            if (_appContext.Inventory == null)
            {
                return NotFound();
            }
            var inventoryItem = await _repository.GetInventoryItem(id);
            if (inventoryItem == null)
            {
                return NotFound();
            }

            return Ok(inventoryItem);
        }

        // POST api/<InventoriesController>
        [HttpPost]
        public async Task<ActionResult<Inventory>> PostInventoryItem([FromBody] InventoryViewModel inventoryItem)
        {

            var item = new Inventory
            {

                Inventory_Item_Category = inventoryItem.category,
                Inventory_Item_Name = inventoryItem.itemName,
                Inventory_Item_Quantity = inventoryItem.quantity,
                Inventory_Item_Photo = inventoryItem.photo,
                Supplier_ID = inventoryItem.supplierID,
                Received_Supplier_Order_ID = inventoryItem.received_supplier_order_id
            };

            try
            {
                _appContext.Inventory.Add(item);
                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"Inventory item created: ID {item.Inventory_ID}, Name '{item.Inventory_Item_Name}', Category '{item.Inventory_Item_Category}', Quantity {item.Inventory_Item_Quantity}",
                    Changed_By = changedBy,
                    Table_Name = nameof(Inventory),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(auditTrail);
                await _appContext.SaveChangesAsync();

            }
            catch (Exception ex) {

                return BadRequest("Failed to create Inventory Item. Please try again");
            }

         

            return Ok(item);
        }

        // PUT api/<InventoriesController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutInventoryItem(int id, [FromBody] InventoryViewModel inventoryItem)
        {
            if (id != inventoryItem.inventoryID)
            {
                return BadRequest();
            }
            var existingitem = await _appContext.Inventory.FindAsync(id);

            if (existingitem == null)
            {
                return NotFound();
            }

            // Store original values
            var originalName = existingitem.Inventory_Item_Name;
            var originalCategory = existingitem.Inventory_Item_Category;
            var originalQuantity = existingitem.Inventory_Item_Quantity;
            var originalPhoto = existingitem.Inventory_Item_Photo;
            var originalSupplierId = existingitem.Supplier_ID;

            // Update entity with new values
            existingitem.Inventory_Item_Name = inventoryItem.itemName;
            existingitem.Inventory_Item_Category = inventoryItem.category;
            existingitem.Inventory_Item_Quantity = inventoryItem.quantity;
            existingitem.Supplier_ID = inventoryItem.supplierID;
            existingitem.Inventory_Item_Photo = originalPhoto;

            //  _appContext.Entry(inventoryItem).State = EntityState.Modified;

            try
            {
                _appContext.Inventory.Update(existingitem);    
                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditChanges = new List<string>();

                // Compare and record changes
                if (originalName != inventoryItem.itemName)
                {
                    auditChanges.Add($"Name changed from '{originalName}' to '{inventoryItem.itemName}'");
                }
                if (originalCategory != inventoryItem.category)
                {
                    auditChanges.Add($"Category changed from '{originalCategory}' to '{inventoryItem.category}'");
                }
                if (originalQuantity != inventoryItem.quantity)
                {
                    auditChanges.Add($"Quantity changed from '{originalQuantity}' to '{inventoryItem.quantity}'");
                }
                if (originalPhoto != inventoryItem.photo)
                {
                    auditChanges.Add($"Photo changed from '{originalPhoto}' to '{inventoryItem.photo}'");
                }
                if (originalSupplierId != inventoryItem.supplierID)
                {
                    auditChanges.Add($"Supplier ID changed from '{originalSupplierId}' to '{inventoryItem.supplierID}'");
                }

                if (auditChanges.Any())
                {
                    var auditTrail = new Audit_Trail
                    {
                        Transaction_Type = "UPDATE",
                        Critical_Data = $"Inventory item updated: ID {id}, {string.Join(", ", auditChanges)}",
                        Changed_By = changedBy,
                        Table_Name = nameof(Inventory),
                        Timestamp = DateTime.UtcNow
                    };

                    _appContext.Audit_Trails.Add(auditTrail);
                    await _appContext.SaveChangesAsync();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InventoryStatusExists(id))
                {
                    return NotFound();
                }
                else
                {
                    return BadRequest("Failed to update Inventory Item. Please try again.");
                }
            }

            return NoContent();
        }


        // DELETE api/<InventoriesController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (_appContext.Inventory == null)
            {
                return NotFound();
            }
            var item = await _appContext.Inventory.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            var inspected = await _appContext.Inspection.FirstOrDefaultAsync(inspect => inspect.Inventory_ID == id);
            if (inspected != null)
            {
                return BadRequest("Cannot delete this item as it has a recorded inspection.");
            }


            try
            {
                _appContext.Inventory.Remove(item);
                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "DELETE",
                    Critical_Data = $"Inventory Item deleted: ID '{id}', Name '{item.Inventory_Item_Name}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Inventory),
                    Timestamp = DateTime.UtcNow
                };



                _appContext.Audit_Trails.Add(auditTrail);
                await _appContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return BadRequest("Failed to delete Inventory Item" + e.ToString());
            }
        

            return NoContent();

        }

        private bool InventoryStatusExists(int id)
        {
            return (_appContext.Inventory?.Any(e => e.Inventory_ID == id)).GetValueOrDefault();
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
