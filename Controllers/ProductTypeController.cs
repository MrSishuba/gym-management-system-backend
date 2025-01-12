using av_motion_api.Data;
using av_motion_api.Models;
using av_motion_api.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductTypeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductTypeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ProductType/getAllProdTypes
        [HttpGet]
        [Route("getAllProdTypes")]
        public async Task<IActionResult> GetAllProdTypes()
        {
            try
            {
                var productTypes = await _context.Product_Types.ToListAsync();
                return Ok(productTypes);
            }
            catch (Exception)
            {
                return BadRequest("An error occurred, please try again.");
            }
        }

        // GET: api/ProductType/getProdTypeById/{id}
        [HttpGet]
        [Route("getProdTypeById/{id}")]
        public async Task<IActionResult> GetProdTypeById(int id)
        {
            try
            {
                var productType = await _context.Product_Types.FindAsync(id);

                if (productType == null)
                {
                    return NotFound("Product type not found.");
                }

                return Ok(productType);
            }
            catch (Exception)
            {
                return BadRequest("An error occurred, please try again.");
            }
        }


        // POST: api/ProductType
        [HttpPost]
        [Route("addProdType")]
        public async Task<IActionResult> AddProdType([FromBody] ProductTypeViewModel ptv)
        {
            var productType = new Product_Type
            {
                Type_Name = ptv.Type_Name
            };

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail
            var audit = new Audit_Trail
            {
                Transaction_Type = "INSERT",
                Critical_Data = $"Product type created: Name '{productType.Type_Name}'",
                Changed_By = changedBy,
                Table_Name = nameof(Product_Type),
                Timestamp = DateTime.UtcNow
            };

            _context.Product_Types.Add(productType);
            _context.Audit_Trails.Add(audit);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProdTypeById), new { id = productType.Product_Type_ID }, productType);
        }

        // PUT: api/ProductType/5
        [HttpPut]
        [Route("updateProdType/{id}")]
        public async Task<IActionResult> UpdateProdType(int id, [FromBody] ProductTypeViewModel ptv)
        {
            var productType = await _context.Product_Types.FindAsync(id);

            if (productType == null)
            {
                return NotFound("Product type not found.");
            }

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail
            var audit = new Audit_Trail
            {
                Transaction_Type = "UPDATE",
                Critical_Data = $"Product type updated from '{productType.Type_Name}' to '{ptv.Type_Name}'",
                Changed_By = changedBy,
                Table_Name = nameof(Product_Type),
                Timestamp = DateTime.UtcNow
            };

            productType.Type_Name = ptv.Type_Name;

            _context.Entry(productType).State = EntityState.Modified;
            _context.Audit_Trails.Add(audit);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/ProductType/5
        [HttpDelete]
        [Route("deleteProdType/{id}")]
        public async Task<IActionResult> DeleteProdType(int id)
        {
            var productType = await _context.Product_Types.FindAsync(id);

            if (productType == null)
            {
                return NotFound("Product type not found.");
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Product_Type_ID == id);
            if (product != null)
            {
                return BadRequest("Cannot delete this type as there are products assigned to it.");
            }

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail
            var audit = new Audit_Trail
            {
                Transaction_Type = "DELETE",
                Critical_Data = $"Product type deleted: ID '{productType.Product_Type_ID}', Name '{productType.Type_Name}'",
                Changed_By = changedBy,
                Table_Name = nameof(Product_Type),
                Timestamp = DateTime.UtcNow
            };

            _context.Product_Types.Remove(productType);
            _context.Audit_Trails.Add(audit);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<string> GetChangedByAsync()
        {
            // Same logic as ProductCategoryController for identifying the user who made the change
            var userId = User.FindFirstValue("userId");
            if (userId == null)
            {
                return "Unknown";
            }

            if (!int.TryParse(userId, out var parsedUserId))
            {
                return "Unknown";
            }

            var user = await _context.Users.FindAsync(parsedUserId);
            if (user == null)
            {
                return "Unknown";
            }

            var owner = await _context.Owners.FirstOrDefaultAsync(o => o.User_ID == user.User_ID);
            if (owner != null)
            {
                return $"{owner.User.Name} {owner.User.Surname} (Owner)";
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.User_ID == user.User_ID);
            if (employee != null)
            {
                return $"{employee.User.Name} {employee.User.Surname} (Employee)";
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == user.User_ID);
            if (member != null)
            {
                return $"{member.User.Name} {member.User.Surname} (Member)";
            }

            return "Unknown";
        }
    }
}
