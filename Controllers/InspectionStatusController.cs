using av_motion_api.Data;
using av_motion_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace av_motion_api.Controllers
{
    [Route("api/inspectionStatus/[controller]")]
    [ApiController]
    public class InspectionStatusController : ControllerBase
    {
        // GET: api/<InspectionStatusController>
        private readonly AppDbContext _appContext;
        public InspectionStatusController(AppDbContext _context)
        {

            _appContext = _context;
        }
        // GET: api/<InspectionStatusController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Inspection_Status>>> GetInspectionStatuses()
        {
            var inspectionStatuses = await _appContext.Inspection_Status.ToListAsync();

            return inspectionStatuses;
        }

        // GET api/<InspectionStatusController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Inspection_Status>> GetInspectionStatus(int id)
        {
            if (_appContext.Inspection_Status == null)
            {
                return NotFound();
            }
            var inspectionStatus = await _appContext.Inspection_Status.FindAsync(id);
            if (inspectionStatus == null)
            {
                return NotFound();
            }

            return inspectionStatus;
        }

        // POST api/<InspectionStatusController>
        [HttpPost]
        public async Task<ActionResult<Inspection_Status>> PostInspectionStatus([FromBody] Inspection_Status inspectionStatus)
        {
            if (_appContext.Inspection_Status == null)
            {
                return Problem("Entity set 'AppDbContext.Inspection_Status' is null.");
            }
            _appContext.Inspection_Status.Add(inspectionStatus);
            await _appContext.SaveChangesAsync();

            return Ok(inspectionStatus);
        }

        // PUT api/<InspectionStatusController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutInspectionStatus(int id, [FromBody] Inspection_Status insepectionStatus)
        {
            if (id != insepectionStatus.Inspection_Status_ID)
            {
                return BadRequest();
            }

            _appContext.Entry(insepectionStatus).State = EntityState.Modified;

            try
            {
                await _appContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InspectionStatusExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE api/<InspectionStatusController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInspectionStatus(int id)
        {
            if (_appContext.Inspection_Status == null)
            {
                return NotFound();
            }
            var inspectionStatus = await _appContext.Inspection_Status.FindAsync(id);
            if (inspectionStatus == null)
            {
                return NotFound();
            }

            var inspection = await _appContext.Inspection.FirstOrDefaultAsync(slot => slot.Inspection_Status_ID == id);
            if (inspection != null)
            {
                return BadRequest("Cannot delete this inspection status as there are inspections with this status.");
            }

            _appContext.Inspection_Status.Remove(inspectionStatus);
            await _appContext.SaveChangesAsync();

            return NoContent();
        }

        private bool InspectionStatusExists(int id)
        {
            return (_appContext.Inspection_Status?.Any(e => e.Inspection_Status_ID == id)).GetValueOrDefault();
        }
    }
}
