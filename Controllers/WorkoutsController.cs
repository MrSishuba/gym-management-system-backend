using av_motion_api.Data;
using av_motion_api.Interfaces;
using av_motion_api.Models;
using av_motion_api.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace av_motion_api.Controllers
{
    [Route("api/workouts/[controller]")]
    [ApiController]
    public class WorkoutsController : ControllerBase
    {
        private readonly AppDbContext _appContext;
        public readonly IRepository _repository;
        public WorkoutsController(AppDbContext _context, IRepository repository)
        {

            _appContext = _context;
            _repository = repository;
        }

        // GET: api/<WorkoutsController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkoutViewModel>>> GetWorkouts()
        {
            var workoutsDetails = await _repository.GetWorkoutCategories();
           
            return Ok(workoutsDetails);
        }


        [HttpGet("all/{id}")]
     
        public async Task<ActionResult<IEnumerable<Workout>>> GetWorkout(int id)
        {
            if (_appContext.Workout == null)
            {
                return NotFound();
            }
            var workoutDetails = await _appContext.Workout.FindAsync(id);
            if (workoutDetails == null)
            {
                return NotFound();
            }

            return Ok(workoutDetails);
        }


        // GET api/<WorkoutsController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<IEnumerable<WorkoutViewModel>>> ViewWorkout(int id)
        {
            if (_appContext.Workout == null)
            {
                return NotFound();
            }
            var workoutDetails = await _repository.GetWorkoutCategory(id);
            if (workoutDetails == null)
            {
                return NotFound();
            }

            return Ok(workoutDetails);
        }

        // POST api/<WorkoutsController>
        [HttpPost]
        public async Task<ActionResult<Workout>> PostWorkout([FromBody] WorkoutViewModel work_Out)
        {

           // var workoutCategory = await _appContext.Workout_Category.FirstOrDefaultAsync(wo => wo.Workout_Category_ID == work_Out.workoutCategoryId);

            var workoutEntity = new Workout()
                {
                    Workout_Name = work_Out.Workout_Name,
                    Workout_Description = work_Out.Workout_Description,
                    Sets = work_Out.Sets,
                    Reps = work_Out.Reps,
                    Workout_Category_ID = work_Out.Workout_Category_ID,
                   
                   
                };

                var workout = await _appContext.Workout.FirstOrDefaultAsync(wo => wo.Workout_Name == work_Out.Workout_Name);
            try
            {
                if (workout != null)
                {
                    return BadRequest("A workout with this name already exists");
                }
                else
                {
                    _appContext.Workout.Add(workoutEntity);
                    // Save Changes
                    await _appContext.SaveChangesAsync();

                    // Audit Trail
                    var changedBy = await GetChangedByAsync();
                    var auditTrail = new Audit_Trail
                    {
                        Transaction_Type = "INSERT",
                        Critical_Data = $"New Workout created: Name '{work_Out.Workout_Name}', Description '{work_Out.Workout_Description}', Sets '{work_Out.Sets}', Reps '{work_Out.Reps}'",
                        Changed_By = changedBy,
                        Table_Name = nameof(Workout),
                        Timestamp = DateTime.UtcNow
                    };

                    _appContext.Audit_Trails.Add(auditTrail);
                    await _appContext.SaveChangesAsync();
                }
            }catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest("Failed to create Workout. Please try again.");
            }

            if (_appContext.Workout == null)
            { return Problem("Entity set 'AppDbContext.Workouts'  is null."); }

         


            return Ok(workoutEntity);
        }

        // PUT api/<WorkoutsController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWorkout(int id, [FromBody] WorkoutViewModel work_Out)
        {

            var workoutEntity = await _appContext.Workout.FindAsync(id);

            if (workoutEntity == null)
            {
                return NotFound();
            }

            // Store the original values before making updates
            var originalName = workoutEntity.Workout_Name;
            var originalDescription = workoutEntity.Workout_Description;
            var originalSets = workoutEntity.Sets;
            var originalReps = workoutEntity.Reps;
            var originalCategoryId = workoutEntity.Workout_Category_ID;

            // Update the workout entity with new values
            workoutEntity.Workout_Name = work_Out.Workout_Name;
            workoutEntity.Workout_Description = work_Out.Workout_Description;
            workoutEntity.Sets = work_Out.Sets;
            workoutEntity.Reps = work_Out.Reps;
            workoutEntity.Workout_Category_ID = work_Out.Workout_Category_ID;
            // var workoutCategory = await _a(work_Out.workoutCategory);

            //if (workoutCategory == null)
            //{
            //    return NotFound("WorkoutCategory not found");
            //}
            var workout = await _appContext.Workout.FirstOrDefaultAsync(wo => wo.Workout_Name == work_Out.Workout_Name);

            if (workout != null && workoutEntity.Workout_ID != workout.Workout_ID)
            {
                return BadRequest("A workout with this name already exists");
            }
            else
            {
                try
                {
                    _appContext.Workout.Update(workoutEntity);
                    await _appContext.SaveChangesAsync();

                    // Audit Trail
                    var changedBy = await GetChangedByAsync();
                    var auditChanges = new List<string>();

                    // Compare and record changes
                    if (originalName != work_Out.Workout_Name)
                    {
                        auditChanges.Add($"Name changed from '{originalName}' to '{work_Out.Workout_Name}'");
                    }
                    if (originalDescription != work_Out.Workout_Description)
                    {
                        auditChanges.Add($"Description changed from '{originalDescription}' to '{work_Out.Workout_Description}'");
                    }
                    if (originalSets != work_Out.Sets)
                    {
                        auditChanges.Add($"Sets changed from '{originalSets}' to '{work_Out.Sets}'");
                    }
                    if (originalReps != work_Out.Reps)
                    {
                        auditChanges.Add($"Reps changed from '{originalReps}' to '{work_Out.Reps}'");
                    }
                    if (originalCategoryId != work_Out.Workout_Category_ID)
                    {
                        auditChanges.Add($"Workout Category ID changed from '{originalCategoryId}' to '{work_Out.Workout_Category_ID}'");
                    }

                    if (auditChanges.Any())
                    {
                        var auditTrail = new Audit_Trail
                        {
                            Transaction_Type = "UPDATE",
                            Critical_Data = $"Workout updated: ID '{id}', {string.Join(", ", auditChanges)}",
                            Changed_By = changedBy,
                            Table_Name = nameof(Workout),
                            Timestamp = DateTime.UtcNow
                        };

                        _appContext.Audit_Trails.Add(auditTrail);
                        await _appContext.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkoutExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        return BadRequest("Failed to update Workout. Please try again.");
                    }
                }
            }
           

            return Ok(workoutEntity);
        }

        // DELETE api/<WorkoutsController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkout(int id)
        {
            if (_appContext.Workout == null)
            {
                return NotFound();
            }
            var work_Out = await _appContext.Workout.FindAsync(id);
            if (work_Out == null)
            {
                return NotFound();
            }

            var lessonPlanWorkout = await _appContext.lesson_Plan_Workout.FirstOrDefaultAsync(slot => slot.Workout_ID == id);
            if (lessonPlanWorkout != null)
            {
                return BadRequest("Cannot delete this workout as it is part of lesson plans");
            }

            try
            {
                _appContext.Workout.Remove(work_Out);
                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "DELETE",
                    Critical_Data = $"Workout deleted: ID '{id}', Name '{work_Out.Workout_Name}', Description '{work_Out.Workout_Description}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Workout),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(auditTrail);
                await _appContext.SaveChangesAsync();
            }
            catch(DbUpdateConcurrencyException)
            {
                return BadRequest("Failed to delete Workout. Please try again.");
            }

          

            return NoContent();
        }

        private bool WorkoutExists(int id)
        {
            return (_appContext.Workout?.Any(e => e.Workout_ID == id)).GetValueOrDefault();
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
