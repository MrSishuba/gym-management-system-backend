using av_motion_api.Data;
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
    public class ProductCategoryController : ControllerBase
    {
        private readonly AppDbContext _appContext;
        public ProductCategoryController(AppDbContext _context)
        {

            _appContext = _context;
        }
        // GET: api/<ProductCategoryController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product_Category>>> GetProductCategories()
        {
            var productCategories = await _appContext.Product_Categories.ToListAsync();

            return productCategories;
        }
        // GET api/<ProductCategoryController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product_Category>> GetProductCategory(int id)
        {
            if (_appContext.Product_Categories == null)
            {
                return NotFound();
            }
            var productCategory = await _appContext.Product_Categories.FindAsync(id);
            if (productCategory == null)
            {
                return NotFound();
            }

            return productCategory;
        }

        [HttpGet]
        [Route("getAllProdCategories")]
        public async Task<IActionResult> GetAllProdCategories()
        {
            try
            {
                var categories = await _appContext.Product_Categories.ToListAsync();
                return Ok(categories);
            }
            catch (Exception)
            {

                return BadRequest("An Error occured, Please try again");
            }
        }


        [HttpGet]
        [Route("getProdCategoryById/{id}")]
        public async Task<IActionResult> GetProdCategoryById(int id)
        {
            try
            {
                var category = await _appContext.User_Types.FindAsync(id);

                if (category == null)
                {
                    return NotFound("Product category not found.");
                }

                return Ok(category);
            }
            catch (Exception)
            {
                return BadRequest("An Error occured, Please try again");
            }
        }

        [HttpGet("getCategoriesByType/{typeId}")]
        public async Task<IActionResult> GetCategoriesByType(int typeId)
        {
            try
            {
                var categories = await _appContext.Product_Categories
                    .Where(c => c.Product_Type_ID == typeId)
                    .ToListAsync();

                if (categories == null || categories.Count == 0)
                {
                    return NotFound("No categories found for the selected product type.");
                }

                return Ok(categories);
            }
            catch (Exception)
            {
                return BadRequest("An error occurred. Please try again.");
            }
        }

        // POST api/<ProductCategoryController>
        [HttpPost]
        [Route("addProdCategory")]
        public async Task<IActionResult> addProdCategory(ProductCategoryViewModel pcv)
        {
            // Ensure the product type exists before assigning it
            var productType = await _appContext.Product_Types.FindAsync(pcv.Product_Type_ID);
            if (productType == null)
            {
                return BadRequest("Invalid Product Type ID.");
            }

            var category = new Product_Category
            {
                Category_Name = pcv.Category_Name,
                Product_Type_ID = pcv.Product_Type_ID
            };

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail
            var audit = new Audit_Trail
            {
                Transaction_Type = "INSERT",
                Critical_Data = $"Product category created: Name '{category.Category_Name}', Product Type ID: '{category.Product_Type_ID}'",
                Changed_By = changedBy,
                Table_Name = nameof(Product_Category),
                Timestamp = DateTime.UtcNow
            };

            _appContext.Product_Categories.Add(category);
            await _appContext.SaveChangesAsync();

            _appContext.Audit_Trails.Add(audit);
            await _appContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProdCategoryById), new { id = category.Product_Category_ID }, category);
        }


        // PUT api/<ProductCategoryController>/5
        [HttpPut]
        [Route("updateProdCategory/{id}")]
        public async Task<IActionResult> UpdateProdCategory(int id, [FromBody] ProductCategoryViewModel pcv)
        {
            var category = await _appContext.Product_Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound("User_Type not found.");
            }

            // Ensure the product type exists before assigning it
            var productType = await _appContext.Product_Types.FindAsync(pcv.Product_Type_ID);
            if (productType == null)
            {
                return BadRequest("Invalid Product Type ID.");
            }

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail
            var audit = new Audit_Trail
            {
                Transaction_Type = "UPDATE",
                Critical_Data = $"Product category updated from '{category.Category_Name}' to '{pcv.Category_Name}', Product Type ID changed from '{category.Product_Type_ID}' to '{pcv.Product_Type_ID}'",
                Changed_By = changedBy,
                Table_Name = nameof(Product_Category),
                Timestamp = DateTime.UtcNow
            };

            category.Category_Name = pcv.Category_Name;
            category.Product_Type_ID = pcv.Product_Type_ID;

            _appContext.Entry(category).State = EntityState.Modified;
            _appContext.Audit_Trails.Add(audit);
            await _appContext.SaveChangesAsync();

            return NoContent();
        }

        //[HttpDelete("{id}")]
        [HttpDelete]
        [Route("deleteProdCategory/{id}")]
        public async Task<IActionResult> DeleteProdCategory(int id)
        {
            var category = await _appContext.Product_Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound("Product category not found.");
            }

            var product = await _appContext.Products.FirstOrDefaultAsync(category => category.Product_Category_ID == id);
            if (product != null)
            {
                return BadRequest("Cannot delete this category as there are inventory items with this category.");
            }

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail
            var audit = new Audit_Trail
            {
                Transaction_Type = "DELETE",
                Critical_Data = $"Product category deleted: ID '{category.Product_Category_ID}', Name '{category.Category_Name}'",
                Changed_By = changedBy,
                Table_Name = nameof(Product_Category),
                Timestamp = DateTime.UtcNow
            };

            _appContext.Product_Categories.Remove(category);
            _appContext.Audit_Trails.Add(audit);
            await _appContext.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductCategoryExists(int id)
        {
            return (_appContext.Product_Categories?.Any(e => e.Product_Category_ID == id)).GetValueOrDefault();
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
