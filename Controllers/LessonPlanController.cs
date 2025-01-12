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
    [Route("api/lessonPlan/[controller]")]
    [ApiController]
    public class LessonPlanController : ControllerBase
    {
        // GET: api/<LessonPlanController>
        private readonly AppDbContext _appContext;
        public readonly IRepository _repository;

        public LessonPlanController(AppDbContext _context, IRepository repository)
        {

            _appContext = _context;
            _repository = repository;
        }

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<Lesson_Plan>>> GetLessonPlans()
        {
            var lessonPlans = await _appContext.Lesson_Plans.ToListAsync();

            return Ok(lessonPlans);
        }

        [HttpGet("with-workouts")]
        public async Task<ActionResult<IEnumerable<LessonPlanWorkoutViewModel>>> GetLessonPlansWithWorkouts()
        {
            var lessonPlans = await _repository.GetLessonPlanWithWorkout();

            return Ok(lessonPlans);
        }

        [HttpGet("by-id/{id}")]
        public async Task<ActionResult<Lesson_Plan>> GetLessonPlan(int id)
        {
            if (_appContext.Lesson_Plans == null)
            {
                return NotFound();
            }

            var lessonPlans = await _appContext.Lesson_Plans.FindAsync(id);

            //   var lessonPlan = await _appContext.Lesson_Plans.FindAsync(id);
            if (Ok(lessonPlans) == null)
            {
                return NotFound();
            }

            return Ok(lessonPlans);
        }
        // GET api/<LessonPlanController>/5
        [HttpGet("with-workouts/{id}")]
        public async Task<ActionResult<Lesson_Plan>> GetLessonPlanWithWorkouts(int id)
        {
            if (_appContext.Lesson_Plans == null)
            {
                return NotFound();
            }

            var lessonPlans = await _repository.GetLessonPlanWithWorkouts(id);

            //   var lessonPlan = await _appContext.Lesson_Plans.FindAsync(id);
            if (Ok(lessonPlans) == null)
            {
                return NotFound();
            }

            return Ok(lessonPlans);
        }

        // POST api/<LessonPlanController>


        //this post method adds a lesosn plan directly to the lesson plan table
        [HttpPost("lessonplan")]
        public async Task<ActionResult<Lesson_Plan>> PostLessonPlan([FromBody] Lesson_Plan newLesson_Plan)
        {

            try
            {
                var lessonPlan = await _appContext.Lesson_Plans.FirstOrDefaultAsync(wo => wo.Program_Name == newLesson_Plan.Program_Name);

                if (lessonPlan != null)
                {
                    return BadRequest("A lesson plan with this name already exists");
                }
                else
                {
                    _appContext.Lesson_Plans.Add(newLesson_Plan);
                    await _appContext.SaveChangesAsync();

                    // Audit Trail
                    var changedBy = await GetChangedByAsync();
                    var auditTrail = new Audit_Trail
                    {
                        Transaction_Type = "INSERT",
                        Critical_Data = $"Lesson plan created: ID '{newLesson_Plan.Lesson_Plan_ID}', Name '{newLesson_Plan.Program_Name}', Description '{newLesson_Plan.Program_Description}'",
                        Changed_By = changedBy,
                        Table_Name = nameof(Lesson_Plan),
                        Timestamp = DateTime.UtcNow
                    };

                    _appContext.Audit_Trails.Add(auditTrail);
                    await _appContext.SaveChangesAsync();
                }
            }
            catch(Exception ex)
            {
                return BadRequest("Failed to create LessonPlan. Please try again");
            }
          
            return Ok(newLesson_Plan);
        }


        //this post method first searched for the lesson plan with that id, it then maps the correspoding workouts to the correct lesson plan in the Lesson_Plan_Workouts table
        //due to the constratints of the parameter list, i.e there cannot be more than one object parsed at at time, this post method was separted from the initial lesson plan post method
        [HttpPost]
        public async Task<ActionResult<Lesson_Plan>> PostLessonPlanWorkout([FromBody] LessonPlanWorkoutViewModel lesson_Plan)
        {

            try
            {
                // Validate the incoming model
                if (lesson_Plan == null || lesson_Plan.workout_ID == null || !lesson_Plan.workout_ID.Any())
                {
                    return BadRequest("Invalid lesson plan workout data.");
                }

                // Check if the Lesson_Plan exists
                var lessonPlan = await _appContext.Lesson_Plans.FindAsync(lesson_Plan.lessonPlanID);
                if (lessonPlan == null)
                {
                    return NotFound("Lesson plan not found.");
                }

                // Create LessonPlanWorkout entities
                foreach (var workoutId in lesson_Plan.workout_ID)
                {
                    var workout = await _appContext.Workout.FindAsync(workoutId);
                    if (workout == null)
                    {
                        return NotFound($"Workout with ID {workoutId} not found.");
                    }

                    var lessonPlanWorkout = new Lesson_Plan_Workout
                    {
                        Lesson_Plan_ID = lesson_Plan.lessonPlanID,
                        Workout_ID = workoutId
                    };
                    _appContext.lesson_Plan_Workout.Add(lessonPlanWorkout);
                }

                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"Lesson plan workouts added: Lesson Plan ID '{lesson_Plan.lessonPlanID}', Workout IDs '{string.Join(", ", lesson_Plan.workout_ID)}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Lesson_Plan_Workout),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(auditTrail);
                await _appContext.SaveChangesAsync();
            }
            catch(Exception ex) {
                return BadRequest("Failed to create lesson plan. Please try again");
            }

            

            return Ok(lesson_Plan);
        }

        // PUT api/<LessonPlanController>/5
        [HttpPut("lessonplan/{id}")]
        public async Task<IActionResult> PutLessonPlan(int id, [FromBody] Lesson_Plan lesson_Plan)
        {

            if (id != lesson_Plan.Lesson_Plan_ID)
            {
                return BadRequest();
            }

            var existingLessonPlan = await _appContext.Lesson_Plans.FindAsync(id);
            if (existingLessonPlan == null)
            {
                return NotFound();
            }

            var originalName = existingLessonPlan.Program_Name;
            var originalDescription = existingLessonPlan.Program_Description;
           

            // Update the properties of the existing entity with the values from the input entity
            existingLessonPlan.Program_Name = lesson_Plan.Program_Name;
            existingLessonPlan.Program_Description = lesson_Plan.Program_Description;


            var oldLessonPlan = await _appContext.Lesson_Plans.FirstOrDefaultAsync(wo => wo.Program_Name == lesson_Plan.Program_Name && wo.Lesson_Plan_ID != lesson_Plan.Lesson_Plan_ID);

            if (oldLessonPlan != null && lesson_Plan.Lesson_Plan_ID != oldLessonPlan.Lesson_Plan_ID)
            {
                return BadRequest("A lesson plan with this name already exists");
            }



            try
            {
                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditChanges = new List<string>();

                if (originalName != lesson_Plan.Program_Name)
                {
                    auditChanges.Add($"Program Name changed from '{originalName}' to '{lesson_Plan.Program_Name}'");
                }
                if (originalDescription != lesson_Plan.Program_Description)
                {
                    auditChanges.Add($"Description changed from '{originalDescription}' to '{lesson_Plan.Program_Description}'");
                }

                if (auditChanges.Any())
                {
                    var auditTrail = new Audit_Trail
                    {
                        Transaction_Type = "UPDATE",
                        Critical_Data = $"Lesson plan updated: ID '{id}', {string.Join(", ", auditChanges)}",
                        Changed_By = changedBy,
                        Table_Name = nameof(Lesson_Plan),
                        Timestamp = DateTime.UtcNow
                    };

                    _appContext.Audit_Trails.Add(auditTrail);
                    await _appContext.SaveChangesAsync();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LessonPlanExists(id))
                {
                    return NotFound();
                }
                else
                {
                    return BadRequest("Failed to update lesson plan. Please try again.");
                }
            }

            return NoContent();
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> PutLessonPlanWorkout(int id, [FromBody] LessonPlanWorkoutViewModel lessonPlanWorkout)
        {
            using (var transaction = await _appContext.Database.BeginTransactionAsync())
            {
                try
                {
                    // Retrieve existing workout mappings for the specified lesson plan
                    var existingWorkouts = await _appContext.lesson_Plan_Workout
                        .Where(lp => lp.Lesson_Plan_ID == id)
                        .ToListAsync();

                    // Extract existing workout IDs
                    var existingWorkoutIds = existingWorkouts.Select(w => w.Workout_ID).ToList();

                    // Determine workouts to add
                    var workoutsToAdd = lessonPlanWorkout.workout_ID.Except(existingWorkoutIds).ToList();

                    // Determine workouts to remove
                    var workoutsToRemove = existingWorkoutIds.Except(lessonPlanWorkout.workout_ID).ToList();

                    // Remove workouts
                    foreach (var workoutIdToRemove in workoutsToRemove)
                    {
                        var workoutToRemove = existingWorkouts.FirstOrDefault(w => w.Workout_ID == workoutIdToRemove);
                        if (workoutToRemove != null)
                        {
                            _appContext.lesson_Plan_Workout.Remove(workoutToRemove);
                            await _appContext.SaveChangesAsync();
                        }
                    }
                                       
                    // Add new workouts
                    foreach (var workoutIdToAdd in workoutsToAdd)
                    {
                        var workout = await _appContext.Workout.FindAsync(workoutIdToAdd);
                        if (workout == null)
                        {
                            return NotFound($"Workout with ID {workoutIdToAdd} not found.");
                        }

                        var lessonPlanWorkouts = new Lesson_Plan_Workout
                        {
                            Lesson_Plan_ID = id,
                            Workout_ID = workoutIdToAdd
                        };
                        _appContext.lesson_Plan_Workout.Add(lessonPlanWorkouts);
                    }

                    await _appContext.SaveChangesAsync();



                    // Audit Trail
                    var changedBy = await GetChangedByAsync();
                    var auditChanges = new List<string>();

                    if (workoutsToAdd.Any())
                    {
                        auditChanges.Add($"Workouts added: {string.Join(", ", workoutsToAdd)}");
                    }
                    if (workoutsToRemove.Any())
                    {
                        auditChanges.Add($"Workouts removed: {string.Join(", ", workoutsToRemove)}");
                    }

                    if (auditChanges.Any())
                    {
                        var auditTrail = new Audit_Trail
                        {
                            Transaction_Type = "UPDATE",
                            Critical_Data = $"Lesson plan workouts updated: Lesson Plan ID '{id}', {string.Join(", ", auditChanges)}",
                            Changed_By = changedBy,
                            Table_Name = nameof(Lesson_Plan_Workout),
                            Timestamp = DateTime.UtcNow
                        };

                        _appContext.Audit_Trails.Add(auditTrail);
                        await _appContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }

                    return NoContent();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Handle concurrency exception
                    return BadRequest("Failed to update lesson plan. Please try again.");
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    return BadRequest("Failed to update lesson plan. Please try again.");
                }

            }
        }



        // DELETE api/<LessonPlanController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLessonPlan(int id)
        {
            var lessonPlan = await _appContext.Lesson_Plans.FindAsync(id);
            if (lessonPlan == null)
            {
                return NotFound();
            }

            var lessonPlanSLot = await _appContext.Time_Slots.FirstOrDefaultAsync(slot => slot.Lesson_Plan_ID == id);
            if (lessonPlanSLot != null)
            {
              return BadRequest("This lesson plan has been assigned to a slot");
            }

            var lessonPlanWorkouts = await _appContext.lesson_Plan_Workout
                .Where(slot => slot.Lesson_Plan_ID == id)
                .ToListAsync();

            try
            {
                _appContext.lesson_Plan_Workout.RemoveRange(lessonPlanWorkouts);
                await _appContext.SaveChangesAsync();

                _appContext.Lesson_Plans.Remove(lessonPlan);
                await _appContext.SaveChangesAsync();

                // Audit Trail
                var changedBy = await GetChangedByAsync();
                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "DELETE",
                    Critical_Data = $"Lesson plan deleted: ID '{id}', Name '{lessonPlan.Program_Name}', Description '{lessonPlan.Program_Description}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Lesson_Plan),
                    Timestamp = DateTime.UtcNow
                };

                _appContext.Audit_Trails.Add(auditTrail);
                await _appContext.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                return BadRequest("Failed to delete lesson plan. Please try again");
            }
          

            return NoContent();
        }

        private bool LessonPlanExists(int id)
        {
            return (_appContext.Lesson_Plans?.Any(e => e.Lesson_Plan_ID == id)).GetValueOrDefault();
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
