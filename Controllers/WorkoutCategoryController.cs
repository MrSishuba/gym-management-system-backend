using av_motion_api.Data;
using av_motion_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace av_motion_api.Controllers
{
    [Route("api/workoutCategory/[controller]")]
    [ApiController]
    public class WorkoutCategoryController : ControllerBase
    {
        private readonly AppDbContext _appContext;
        public WorkoutCategoryController(AppDbContext _context)
        {

            _appContext = _context;
        }
        // GET: api/<WorkoutCategoryController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Workout_Category>>> GetWorkoutCategories()
        {
            var categories = await _appContext.Workout_Category.ToListAsync();

            return categories;
        }

        // GET api/<WorkoutCategoryController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Workout_Category>> GetWorkoutCategory(int id)
        {
            if (_appContext.Workout_Category == null)
            {
                return NotFound();
            }
            var category = await _appContext.Workout_Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return category;
        }

        // POST api/<WorkoutCategoryController>
        [HttpPost]
        public async Task<ActionResult<Workout_Category>> PostWorkoutCategory([FromBody] Workout_Category newWorkoutCategory)
        {
            if (_appContext.Workout_Category == null)
            {
                return Problem("Entity set 'AppDbContext.Brands'  is null.");
            }
            var WorkoutCategory = await _appContext.Workout_Category.FirstOrDefaultAsync(wo => wo.Workout_Category_Name == newWorkoutCategory.Workout_Category_Name);

            if (WorkoutCategory != null)
            {
                return BadRequest("A workout category with this name already exists");
            }
            else
            {
                try
                {
                    _appContext.Workout_Category.Add(newWorkoutCategory);
                    await _appContext.SaveChangesAsync();

                    // Audit Trail
                    var changedBy = await GetChangedByAsync();
                    var auditTrail = new Audit_Trail
                    {
                        Transaction_Type = "INSERT",
                        Critical_Data = $"Workout category created: ID '{newWorkoutCategory.Workout_Category_ID}', Name '{newWorkoutCategory.Workout_Category_Name}'",
                        Changed_By = changedBy,
                        Table_Name = nameof(Workout_Category),
                        Timestamp = DateTime.UtcNow
                    };

                    _appContext.Audit_Trails.Add(auditTrail);
                    await _appContext.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    return BadRequest("Failed to create Workout Category. Please Try again.");
                }
            }
                
            

            return Ok(newWorkoutCategory);
        }

        // PUT api/<WorkoutCategoryController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWorkoutCategory(int id, [FromBody] Workout_Category workoutCategory)
        {
            if (id != workoutCategory.Workout_Category_ID)
            {
                return BadRequest("Error. Please Try Again.");
            }

            // Retrieve the existing entity for auditing purposes
            var existingCategory = await _appContext.Workout_Category.FirstOrDefaultAsync(wo => wo.Workout_Category_ID == id);
            if (existingCategory == null)
            {
                return NotFound();
            }

            // Check if a category with the same name already exists
            var WorkoutCategory = await _appContext.Workout_Category
                .FirstOrDefaultAsync(wo => wo.Workout_Category_Name == workoutCategory.Workout_Category_Name);

            if (WorkoutCategory != null && workoutCategory.Workout_Category_ID != WorkoutCategory.Workout_Category_ID)
            {
                return BadRequest("A workout category with this name already exists.");
            }

            // Update properties of the existing category entity
            existingCategory.Workout_Category_Name = workoutCategory.Workout_Category_Name;
            existingCategory.Workout_Category_Description = workoutCategory.Workout_Category_Description;
            existingCategory.Workout_Category_ID = workoutCategory.Workout_Category_ID;

            // Save changes
            try
            {
                await _appContext.SaveChangesAsync();

                // Audit Trail - after successful save
                var changedBy = await GetChangedByAsync();
                var auditChanges = new List<string>();

                if (existingCategory.Workout_Category_Name != workoutCategory.Workout_Category_Name)
                {
                    auditChanges.Add($"Name changed from '{existingCategory.Workout_Category_Name}' to '{workoutCategory.Workout_Category_Name}'");
                }

                if (existingCategory.Workout_Category_Description != workoutCategory.Workout_Category_Description)
                {
                    auditChanges.Add($"Description changed from '{existingCategory.Workout_Category_Description}' to '{workoutCategory.Workout_Category_Description}'");
                }

                if (existingCategory.Workout_Category_ID != workoutCategory.Workout_Category_ID)
                {
                    auditChanges.Add($"Category changed from '{existingCategory.Workout_Category_Name}' to '{workoutCategory.Workout_Category_Name}'");
                }

                if (auditChanges.Any())
                {
                    var auditTrail = new Audit_Trail
                    {
                        Transaction_Type = "UPDATE",
                        Critical_Data = $"Workout category updated: ID '{id}', {string.Join(", ", auditChanges)}",
                        Changed_By = changedBy,
                        Table_Name = nameof(Workout_Category),
                        Timestamp = DateTime.UtcNow
                    };

                    _appContext.Audit_Trails.Add(auditTrail);
                    await _appContext.SaveChangesAsync();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WorkoutCategoryExists(id))
                {
                    return NotFound("Error. Please try again.");
                }
                else
                {
                    return BadRequest("Failed to update Workout Category. Please try again.");
                }
            }

            return NoContent();
        }

        // DELETE api/<WorkoutCategoryController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkoutCategory(int id)
        {
            if (_appContext.Workout_Category == null)
            {
                return NotFound();
            }
            var category = await _appContext.Workout_Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var workout = await _appContext.Workout.FirstOrDefaultAsync(slot => slot.Workout_Category_ID == id);
            if (workout != null)
            {
                return BadRequest("Cannot delete this workout category as it has been added to a workout/workout(s)");
            }

            try
            {
                _appContext.Workout_Category.Remove(category);
                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "DELETE",
                    Critical_Data = $"Workout category deleted: ID '{id}', Name '{category.Workout_Category_Name}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Workout_Category),
                    Timestamp = DateTime.UtcNow
                };



                _appContext.Audit_Trails.Add(auditTrail);
                await _appContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return BadRequest("Failed to delete Workout Category");
            }
           
         
            return NoContent();
        }

        private bool WorkoutCategoryExists(int id)
        {
            return (_appContext.Workout_Category?.Any(e => e.Workout_Category_ID == id)).GetValueOrDefault();
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
