using av_motion_api.Data;
using av_motion_api.Models;
using av_motion_api.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace av_motion_api.Controllers
{
    [Route("api/supplier/[controller]")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly AppDbContext _appContext;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(AppDbContext context, ILogger<SupplierController> logger)
        {
            _appContext = context;
            _logger = logger;
        }

        // GET: api/supplier/GetSuppliers
        [HttpGet]
        [Route("GetSuppliers")]
        //[Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers()
        {
            var suppliers = await _appContext.Suppliers.ToListAsync();
            return suppliers;
        }

        // GET: api/supplier/GetSupplier/{id}
        [HttpGet]
        [Route("GetSupplier/{id}")]
        //[Authorize(Roles = "Administrator")]
        public async Task<ActionResult<Supplier>> GetSupplier(int id)
        {
            if (_appContext.Suppliers == null)
            {
                return NotFound();
            }
            var supplier = await _appContext.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return supplier;
        }

        // POST: api/supplier/PostSupplier
        [HttpPost]
        [Route("PostSupplier")]
        //[Authorize(Roles = "Administrator")]
        public async Task<ActionResult<Supplier>> PostSupplier([FromBody] Supplier supplier)
        {
        
            var supplierEntity = new Supplier
            {
                Name = supplier.Name,
                Contact_Number = supplier.Contact_Number,
                Email_Address = supplier.Email_Address,
                Physical_Address = supplier.Physical_Address,
            };

            try
            {
                _appContext.Suppliers.Add(supplierEntity);
                await _appContext.SaveChangesAsync();

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail for creating supplier
                var supplierAudit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"Supplier created: Name '{supplierEntity.Name}', Contact Number '{supplierEntity.Contact_Number}', Email '{supplierEntity.Email_Address}', Physical Address '{supplierEntity.Physical_Address}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Supplier),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(supplierAudit);
                await _appContext.SaveChangesAsync();  // Ensure the audit trail is saved
            }
            catch(Exception ex)
            {
                return BadRequest("Failed to create Supplier. Please try again.");
            }

          

            return Ok(supplier);
        }

        // PUT: api/supplier/PutSupplier/{id}
        [HttpPut]
        [Route("PutSupplier/{id}")]
        //[Authorize(Roles = "Administrator")]
        public async Task<IActionResult> PutSupplier(int id, [FromBody] SupplierViewModel supplier)
        {
            var supplierEntity = await _appContext.Suppliers.FindAsync(id);

            if (supplierEntity == null)
            {
                return NotFound();
            }

            // Capture the original values for audit trail comparison
            var originalName = supplierEntity.Name;
            var originalContactNumber = supplierEntity.Contact_Number;
            var originalEmailAddress = supplierEntity.Email_Address;
            var originalPhysicalAddress = supplierEntity.Physical_Address;

            // Update the supplier entity with new values
            supplierEntity.Name = supplier.Name;
            supplierEntity.Contact_Number = supplier.Contact_Number;
            supplierEntity.Email_Address = supplier.Email_Address;
            supplierEntity.Physical_Address = supplier.Physical_Address;

            try
            {
                _appContext.Suppliers.Update(supplierEntity);
                await _appContext.SaveChangesAsync();

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Prepare audit trail if any changes were made
                var auditChanges = new List<string>();

                if (originalName != supplierEntity.Name)
                {
                    auditChanges.Add($"Name: from '{originalName}' to '{supplierEntity.Name}'");
                }

                if (originalContactNumber != supplierEntity.Contact_Number)
                {
                    auditChanges.Add($"Contact Number: from '{originalContactNumber}' to '{supplierEntity.Contact_Number}'");
                }

                if (originalEmailAddress != supplierEntity.Email_Address)
                {
                    auditChanges.Add($"Email Address: from '{originalEmailAddress}' to '{supplierEntity.Email_Address}'");
                }

                if (originalPhysicalAddress != supplierEntity.Physical_Address)
                {
                    auditChanges.Add($"Physical Address: from '{originalPhysicalAddress}' to '{supplierEntity.Physical_Address}'");
                }

                if (auditChanges.Any())
                {
                    var supplierAudit = new Audit_Trail
                    {
                        Transaction_Type = "UPDATE",
                        Critical_Data = $"Supplier updated: ID '{supplierEntity.Supplier_ID}', {string.Join(", ", auditChanges)}",
                        Changed_By = changedBy,
                        Table_Name = nameof(Supplier),
                        Timestamp = DateTime.UtcNow
                    };

                    _appContext.Audit_Trails.Add(supplierAudit);
                    await _appContext.SaveChangesAsync();  // Ensure the audit trail is saved
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SupplierExists(id))
                {
                    return NotFound();
                }
                else
                {
                    return BadRequest("Failed to update Workout. Please try again.");
                }
            }

            return NoContent();
        }

        // DELETE: api/supplier/DeleteSupplier/{id}
        [HttpDelete]
        [Route("DeleteSupplier/{id}")]
        //[Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            if (_appContext.Suppliers == null)
            {
                return NotFound();
            }
            var supplier = await _appContext.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            var supplierOrder = await _appContext.Supplier_Orders.FirstOrDefaultAsync(order => order.Supplier_ID == id);
            if (supplierOrder != null)
            {
                return BadRequest("Cannot delete this supplier.");
            }
            try
            {
                _appContext.Suppliers.Remove(supplier);
                await _appContext.SaveChangesAsync();

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail for deleting supplier
                var supplierAudit = new Audit_Trail
                {
                    Transaction_Type = "DELETE",
                    Critical_Data = $"Supplier deleted: ID '{supplier.Supplier_ID}', Name '{supplier.Name}', Contact Number '{supplier.Contact_Number}', Email '{supplier.Email_Address}', Physical Address '{supplier.Physical_Address}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Supplier),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(supplierAudit);
                await _appContext.SaveChangesAsync();  // Ensure the audit trail is saved
            }
            catch(Exception ex)
            {
                return BadRequest("Failed to delete Supplier. Please try again.");
            }

            

            return NoContent();
        }

        private bool SupplierExists(int id)
        {
            return (_appContext.Suppliers?.Any(e => e.Supplier_ID == id)).GetValueOrDefault();
        }

        // GET: api/supplier/GetAllSupplierOrders
        [HttpGet]
        [Route("GetAllSupplierOrders")]
        //[Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAllSupplierOrders()
        {
            var orders = await _appContext.Supplier_Orders
                    .Include(o => o.Supplier)
                    .Include(o => o.Owner)
                    .Include(o => o.Supplier_Order_Lines)
                    .ThenInclude(sol => sol.Product)
                    .ToListAsync();

            var supplierOrderViewModels = orders.Select(order => new SupplierOrderViewModel
            {
                Supplier_Order_ID = order.Supplier_Order_ID,
                Date = order.Date,
                Supplier_ID = order.Supplier_ID,
                Supplier_Name = order.Supplier.Name,
                Owner_ID = order.Owner_ID,
                Status = order.Status,
                Supplier_Order_Details = order.Supplier_Order_Details,
                OrderLines = order.Supplier_Order_Lines.Select(sol => new SupplierOrderLineViewModel
                {
                    Supplier_Order_Line_ID = sol.Supplier_Order_Line_ID,
                    Product_ID = sol.Product_ID,
                    Product_Name = sol.Product.Product_Name,
                    Supplier_Quantity = sol.Supplier_Quantity,
                    Purchase_Price = sol.Purchase_Price
                }).ToList(),
                Total_Price = (decimal)order.Supplier_Order_Lines.Sum(sol => sol.Supplier_Quantity * sol.Purchase_Price)
            }).ToList();

            return Ok(supplierOrderViewModels);
        }

        [HttpGet]
        [Route("GetProductCategories")]
        public async Task<IActionResult> GetProductCategories()
        {
            var categories = await _appContext.Product_Categories.ToListAsync();
            return Ok(categories);
        }

        // Controller to fetch products by category ID
        [HttpGet]
        [Route("GetProductsByCategory/{categoryId}")]
        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            var products = await _appContext.Products.Where(p => p.Product_Category_ID == categoryId).ToListAsync();
            return Ok(products);
        }
        
        [HttpPost]
        [Route("PlaceSupplierOrder")]
        public async Task<IActionResult> PlaceSupplierOrder([FromBody] SupplierOrderViewModel orderVm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Create a new supplier order
            var supplierOrder = new Supplier_Order
            {
                Date = DateTime.UtcNow,
                Supplier_Order_Details = orderVm.Supplier_Order_Details,
                Supplier_ID = orderVm.Supplier_ID,
                Owner_ID = orderVm.Owner_ID,
                Status = orderVm.Status,
                Supplier_Order_Lines = new List<Supplier_Order_Line>()
            };

            decimal totalOrderPrice = 0;
            var orderLineDetails = new List<string>();

            Console.WriteLine($"Creating Supplier_Order with ID {supplierOrder.Supplier_Order_ID}");
            Console.WriteLine($"OrderLines count: {orderVm.OrderLines?.Count ?? 0}");



            try
            {
                // Process each order line
                foreach (var orderLine in orderVm.OrderLines)
                {

                    // Fetch the product details
                    var product = await _appContext.Products.FindAsync(orderLine.Product_ID);
                    if (product == null)
                    {
                        return BadRequest($"Product with ID {orderLine.Product_ID} not found.");
                    }

                    if (product != null)
                    {
                        var purchasePrice = product.Purchase_Price;
                        if (purchasePrice == null)
                        {
                            return BadRequest($"Purchase price for product ID {orderLine.Product_ID} is not set.");
                        }

                        // Create a new supplier order line
                        var supplierOrderLine = new Supplier_Order_Line
                        {
                            Product_ID = orderLine.Product_ID,
                            Supplier_Quantity = orderLine.Supplier_Quantity,
                            Purchase_Price = purchasePrice // Set the purchase price from product
                        };

                        // Add the order line to the supplier order
                        supplierOrder.Supplier_Order_Lines.Add(supplierOrderLine);

                        // Update the Inventory with Supplier_ID
                        var inventory = await _appContext.Inventory.FirstOrDefaultAsync(i => i.Product_ID == orderLine.Product_ID);
                        if (inventory != null)
                        {
                            inventory.Supplier_ID = orderVm.Supplier_ID;
                            _appContext.Inventory.Update(inventory);
                        }

                        // Calculate total price
                        totalOrderPrice += orderLine.Supplier_Quantity * (decimal)purchasePrice;

                        // Capture order line details for audit
                        orderLineDetails.Add($"Product_ID '{orderLine.Product_ID}', Quantity '{orderLine.Supplier_Quantity}', Purchase Price '{orderLine.Purchase_Price}'");
                    }

                    Console.WriteLine($"Processing OrderLine: Product_ID = {orderLine.Product_ID}, Quantity = {orderLine.Supplier_Quantity}");
                }

                // Set total price on supplier order
                supplierOrder.Total_Price = totalOrderPrice;

                // Save the supplier order to the database
                _appContext.Supplier_Orders.Add(supplierOrder);
                await _appContext.SaveChangesAsync();

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail for placing supplier order
                var orderAudit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"Supplier Order created: ID '{supplierOrder.Supplier_Order_ID}', {string.Join("; ", orderLineDetails)}, Total Price '{supplierOrder.Total_Price}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Supplier_Order),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(orderAudit);
                await _appContext.SaveChangesAsync();  // Ensure the audit trail is saved
            }
            catch( Exception ex )
            {
                return BadRequest("Failed to place supplier order. Please try again.");
            }

           

            return Ok(supplierOrder);
        }

        // POST: api/supplier/receivesupplierorder
        [HttpPost]
        [Route("receivesupplierorder")]

        public async Task<IActionResult> ReceiveSupplierOrder([FromBody] ReceivedSupplierOrderViewModel receiveOrderVm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var receivedSupplierOrder = new Received_Supplier_Order
            {
                Supplies_Received_Date = receiveOrderVm.Supplies_Received_Date,
                Discrepancies = receiveOrderVm.Discrepancies,
                Accepted = receiveOrderVm.Accepted ?? false,
                Received_Supplier_Order_Lines = new List<Received_Supplier_Order_Line>()
            };

            try
            {
                _appContext.Received_Supplier_Orders.Add(receivedSupplierOrder);
                await _appContext.SaveChangesAsync();

                var receiveOrderLineDetails = new List<string>();

                if (receivedSupplierOrder.Accepted)
                {
                    foreach (var line in receiveOrderVm.Received_Supplier_Order_Lines)
                    {
                        var product = await _appContext.Products.FindAsync(line.Product_ID);

                        // Debugging and Logging
                        Console.WriteLine($"Processing Product ID: {line.Product_ID}");

                        if (product == null)
                        {
                            Console.WriteLine($"Product ID {line.Product_ID} not found.");
                            return BadRequest($"Product ID {line.Product_ID} not found.");
                        }

                        product.Quantity += line.Received_Supplies_Quantity;
                        _appContext.Products.Update(product);

                        // Update the Inventory record with the Received_Supplier_Order_ID
                        var inventory = await _appContext.Inventory.FirstOrDefaultAsync(i => i.Product_ID == line.Product_ID);
                        if (inventory != null)
                        {
                            inventory.Received_Supplier_Order_ID = receivedSupplierOrder.Received_Supplier_Order_ID;
                            inventory.Inventory_Item_Quantity = product.Quantity; // Sync quantity with product quantity
                            _appContext.Inventory.Update(inventory);
                        }

                        var receivedOrderLine = new Received_Supplier_Order_Line
                        {
                            Received_Supplier_Order_ID = receivedSupplierOrder.Received_Supplier_Order_ID,
                            Received_Supplies_Quantity = line.Received_Supplies_Quantity,
                            Supplier_Order_Line_ID = line.Supplier_Order_Line_ID,
                            Product_ID = line.Product_ID // Ensure this is set
                        };

                        _appContext.Received_Supplier_Order_Lines.Add(receivedOrderLine);

                        // Capture order line details for audit
                        receiveOrderLineDetails.Add($"Product_ID '{line.Product_ID}', Quantity '{line.Received_Supplies_Quantity}'");
                    }

                    await _appContext.SaveChangesAsync();
                }

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail for receiving supplier order
                var receiveOrderAudit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"Supplier Order Received: ID '{receivedSupplierOrder.Received_Supplier_Order_ID}', {string.Join("; ", receiveOrderLineDetails)}",
                    Changed_By = changedBy,
                    Table_Name = nameof(Received_Supplier_Order),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(receiveOrderAudit);
                await _appContext.SaveChangesAsync();  // Ensure the audit trail is saved
            }
            catch( Exception ex )
            {
                return BadRequest("Failed to receive supplier order. Please try again");
            }

           

            return Ok(receivedSupplierOrder);
        }

        [HttpPost]
        [Route("UpdateSupplierOrderStatus")]
        public async Task<IActionResult> UpdateSupplierOrderStatus([FromBody] UpdateSupplierOrderStatusViewModel statusUpdateVm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Fetch the specific supplier order entry
            var supplierOrder = await _appContext.Supplier_Orders
                .FirstOrDefaultAsync(o => o.Supplier_Order_ID == statusUpdateVm.Supplier_Order_ID);

            if (supplierOrder == null)
            {
                return BadRequest($"Supplier order with ID {statusUpdateVm.Supplier_Order_ID} not found.");
            }



            try
            {
                // Capture the original status for audit trail comparison
                var originalStatus = supplierOrder.Status;

                // Update the status based on the Accepted boolean
                supplierOrder.Status = statusUpdateVm.Status; // Ensure this is directly assigned

                // Save changes to the database
                await _appContext.SaveChangesAsync();

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail if status has changed
                if (originalStatus != supplierOrder.Status)
                {
                    var statusUpdateAudit = new Audit_Trail
                    {
                        Transaction_Type = "UPDATE",
                        Critical_Data = $"Supplier Order Status updated: ID '{supplierOrder.Supplier_Order_ID}', from '{originalStatus}' to '{supplierOrder.Status}'",
                        Changed_By = changedBy,
                        Table_Name = nameof(Supplier_Order),
                        Timestamp = DateTime.UtcNow
                    };

                    _appContext.Audit_Trails.Add(statusUpdateAudit);
                    await _appContext.SaveChangesAsync();  // Ensure the audit trail is saved
                }
            }
            catch ( Exception ex ) {

                return BadRequest("Failed to update supplier order status. Please try again.");
            }
           

            return Ok(supplierOrder);
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