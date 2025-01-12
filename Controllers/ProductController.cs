using av_motion_api.Data;
using av_motion_api.Interfaces;
using av_motion_api.Models;
using av_motion_api.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _appContext;
        public readonly IRepository _repository;
        public ProductController(AppDbContext _context, IRepository repository)
        {

            _appContext = _context;
            _repository = repository;
        }
        // GET: api/<ProductController>
        [HttpGet]
        [Route("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await _appContext.Products.ToListAsync();

                return Ok(products);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        // GET api/<ProductController>/5
        [HttpGet]
        [Route("GetProductById/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var product = await _appContext.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                return Ok(product);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        //POST api/<ProductController>
        [HttpPost]
        [DisableRequestSizeLimit]
        [Route("PostProduct")]
        public async Task<IActionResult> PostProduct([FromForm] ProductViewModel product)
        {
            var formCollection = await Request.ReadFormAsync();
            var product_Img = formCollection.Files.FirstOrDefault();

            // Validate Product_Type_ID
            if (!await _appContext.Product_Types.AnyAsync(pt => pt.Product_Type_ID == product.Product_Type_ID))
            {
                Console.WriteLine($"Invalid Product Type ID: {product.Product_Type_ID}");
                return BadRequest("Invalid Product Type ID.");
            }

            // Validate Product_Category_ID
            if (!await _appContext.Product_Categories.AnyAsync(pc => pc.Product_Category_ID == product.Product_Category_ID))
            {
                Console.WriteLine($"Invalid Product Category ID: {product.Product_Category_ID}");
                return BadRequest("Invalid Product Category ID.");
            }

            using (var memoryStream = new MemoryStream())
            {
                await product_Img.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                string base64Image = Convert.ToBase64String(fileBytes);

                var newProduct = new Product()
                {
                    Product_Name = product.Product_Name,
                    Product_Description = product.Product_Description,
                    Product_Img = base64Image,
                    Quantity = product.Quantity,
                    Unit_Price = product.Unit_Price,
                    Purchase_Price = product.Purchase_Price,
                    Size = product.Size,
                    Product_Category_ID = product.Product_Category_ID,
                    Product_Type_ID = product.Product_Type_ID
                };

                if (_appContext.Products == null)
                {
                    return Problem("Entity set 'AppDbContext.Products' is null.");
                }

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                var audit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"Product Created: Name '{product.Product_Name}', Description '{product.Product_Description}', Quantity '{product.Quantity}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Product),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Products.Add(newProduct);
                await _appContext.SaveChangesAsync();

                _appContext.Audit_Trails.Add(audit);
                await _appContext.SaveChangesAsync();

                // Load the related Product_Category entity
                var productCategory = await _appContext.Product_Categories
                    .FirstOrDefaultAsync(pc => pc.Product_Category_ID == newProduct.Product_Category_ID);

                if (productCategory == null)
                {
                    return BadRequest("Product category not found.");
                }

                // Create a new Inventory record for the new product
                var newInventory = new Inventory
                {
                    Inventory_Item_Category = productCategory.Category_Name, // Adjust as needed
                    Inventory_Item_Name = newProduct.Product_Name,
                    Inventory_Item_Quantity = product.Quantity, // Initial quantity
                    Inventory_Item_Photo = newProduct.Product_Img,
                    Product_ID = newProduct.Product_ID,
                    //Supplier_ID = 0, // Set as needed
                    //Received_Supplier_Order_ID = 0 // Set as needed
                };

                _appContext.Inventory.Add(newInventory);
                await _appContext.SaveChangesAsync();

                return Ok(newProduct);
            }
        }


        // PUT api/<ProductController>/5
        [HttpPut]
        [Route("PutProduct/{id}")]
        public async Task<IActionResult> PutProduct(int id, [FromForm] ProductViewModel updatedProduct)
        {
            var existingProduct = await _appContext.Products.FindAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            var auditChanges = new List<string>();

            // Compare each property and record changes
            if (existingProduct.Product_Name != updatedProduct.Product_Name)
            {
                auditChanges.Add($"Product Name: from '{existingProduct.Product_Name}' to '{updatedProduct.Product_Name}'");
                existingProduct.Product_Name = updatedProduct.Product_Name;
            }

            if (existingProduct.Product_Description != updatedProduct.Product_Description)
            {
                auditChanges.Add($"Product Description: from '{existingProduct.Product_Description}' to '{updatedProduct.Product_Description}'");
                existingProduct.Product_Description = updatedProduct.Product_Description;
            }

            if (existingProduct.Quantity != updatedProduct.Quantity)
            {
                auditChanges.Add($"Quantity: from '{existingProduct.Quantity}' to '{updatedProduct.Quantity}'");
                existingProduct.Quantity = updatedProduct.Quantity;
            }

            if (existingProduct.Unit_Price != updatedProduct.Unit_Price)
            {
                auditChanges.Add($"Unit Price: from '{existingProduct.Unit_Price}' to '{updatedProduct.Unit_Price}'");
                existingProduct.Unit_Price = updatedProduct.Unit_Price;
            }

            if (existingProduct.Purchase_Price != updatedProduct.Purchase_Price)
            {
                auditChanges.Add($"Purchase Price: from '{existingProduct.Purchase_Price}' to '{updatedProduct.Purchase_Price}'");
                existingProduct.Purchase_Price = updatedProduct.Purchase_Price;
            }

            if (existingProduct.Size != updatedProduct.Size)
            {
                auditChanges.Add($"Size: from '{existingProduct.Size}' to '{updatedProduct.Size}'");
                existingProduct.Size = updatedProduct.Size;
            }

            if (existingProduct.Product_Category_ID != updatedProduct.Product_Category_ID)
            {
                auditChanges.Add($"Product Category ID: from '{existingProduct.Product_Category_ID}' to '{updatedProduct.Product_Category_ID}'");
                existingProduct.Product_Category_ID = updatedProduct.Product_Category_ID;
            }

            if (existingProduct.Product_Type_ID != updatedProduct.Product_Type_ID)
            {
                auditChanges.Add($"Product Type ID: from '{existingProduct.Product_Type_ID}' to '{updatedProduct.Product_Type_ID}'");
                existingProduct.Product_Type_ID = updatedProduct.Product_Type_ID;
            }

            var audit = new Audit_Trail
            {
                Transaction_Type = "UPDATE",
                Critical_Data = $"Product Updated: {string.Join(", ", auditChanges)}",
                Changed_By = changedBy,
                Table_Name = nameof(Product),
                Timestamp = DateTime.UtcNow
            };

            // Read the form data to get the photo file
            var formCollection = await Request.ReadFormAsync();
            var product_Img = formCollection.Files.FirstOrDefault();

            existingProduct.Product_Name = updatedProduct.Product_Name;
            existingProduct.Product_Description = updatedProduct.Product_Description;
            existingProduct.Quantity = updatedProduct.Quantity;
            existingProduct.Unit_Price = updatedProduct.Unit_Price;
            existingProduct.Purchase_Price = updatedProduct.Purchase_Price;
            existingProduct.Size = updatedProduct.Size;
            existingProduct.Product_Category_ID = updatedProduct.Product_Category_ID;
            existingProduct.Product_Type_ID = updatedProduct.Product_Type_ID;

            if (product_Img != null && product_Img.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await product_Img.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();
                    string base64Image = Convert.ToBase64String(fileBytes);

                    existingProduct.Product_Img = base64Image; // Store the base64 string of the photo
                }
            }

            _appContext.Products.Update(existingProduct);
            _appContext.Audit_Trails.Add(audit);
            await _appContext.SaveChangesAsync();

            // Load the related Product_Category entity
            var productCategory = await _appContext.Product_Categories
                .FirstOrDefaultAsync(pc => pc.Product_Category_ID == existingProduct.Product_Category_ID);

            if (productCategory == null)
            {
                return BadRequest("Product category not found.");
            }

            // Update the Inventory record
            var inventory = await _appContext.Inventory
                .FirstOrDefaultAsync(i => i.Product_ID == id);

            if (inventory != null)
            {
                inventory.Inventory_Item_Category = productCategory.Category_Name;
                inventory.Inventory_Item_Name = existingProduct.Product_Name;
                inventory.Inventory_Item_Quantity = existingProduct.Quantity;
                inventory.Inventory_Item_Photo = existingProduct.Product_Img;
                _appContext.Inventory.Update(inventory);
                await _appContext.SaveChangesAsync();
            }

            return Ok(existingProduct);
        }

        // DELETE api/<ProductController>/5
        [HttpDelete]
        [Route("DeleteProduct/{id}")]

        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (_appContext.Products == null)
            {
                return NotFound();
            }
            var product = await _appContext.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            var audit = new Audit_Trail
            {
                Transaction_Type = "DELETE",
                Critical_Data = $"Product Deleted: ID '{product.Product_ID}', Name '{product.Product_Name}', Description '{product.Product_Description}', Quantity '{product.Quantity}'",
                Changed_By = changedBy,
                Table_Name = nameof(Product),
                Timestamp = DateTime.UtcNow
            };

            _appContext.Products.Remove(product);
            _appContext.Audit_Trails.Add(audit);

            var inventory = await _appContext.Inventory
                .FirstOrDefaultAsync(i => i.Product_ID == id);

            if (inventory != null)
            {
                _appContext.Inventory.Remove(inventory);
            }

            await _appContext.SaveChangesAsync();

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
