using av_motion_api.Data;
using av_motion_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace av_motion_api.Controllers
{
    [Route("api/inspectionType/[controller]")]
    [ApiController]
    public class InspectionTypeController : ControllerBase
    {
        // GET: api/<InspectionTypeController>
        private readonly AppDbContext _appContext;
        public InspectionTypeController(AppDbContext _context)
        {

            _appContext = _context;
        }
        // GET: api/<InspectionTypeController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Inspection_Type>>> GetInspectionTypes()
        {
            var inspectionTypes = await _appContext.Inspection_Type.ToListAsync();

            return inspectionTypes;
        }

        // GET api/<InspectionTypeController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Inspection_Type>> GetInspectionInspection(int id)
        {
            if (_appContext.Inspection_Type == null)
            {
                return NotFound();
            }
            var insoectionType = await _appContext.Inspection_Type.FindAsync(id);
            if (insoectionType == null)
            {
                return NotFound();
            }

            return insoectionType;
        }

        // POST api/<InspectionTypeController>
        [HttpPost]
        public async Task<ActionResult<Inspection_Type>> PostInspectionType([FromBody] Inspection_Type insoectionType)
        {
            if (_appContext.Inspection_Type == null)
            {
                return Problem("Entity set 'AppDbContext.Brands'  is null.");
            }
            _appContext.Inspection_Type.Add(insoectionType);
            await _appContext.SaveChangesAsync();

            return Ok(insoectionType);
        }

        // PUT api/<InspectionTypeController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutInspectionType(int id, [FromBody] Inspection_Type insepectionType)
        {
            if (id != insepectionType.Inspection_Type_ID)
            {
                return BadRequest();
            }

            _appContext.Entry(insepectionType).State = EntityState.Modified;

            try
            {
                await _appContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InspectionTypeExists(id))
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

        // DELETE api/<InspectionTypeController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInspectionType(int id)
        {
            if (_appContext.Inspection_Type == null)
            {
                return NotFound();
            }
            var inspectionType = await _appContext.Inspection_Type.FindAsync(id);
            if (inspectionType == null)
            {
                return NotFound();
            }

            var inspection = await _appContext.Inspection.FirstOrDefaultAsync(slot => slot.Inspection_Type_ID == id);
            if (inspection != null)
            {
                return BadRequest("Cannot delete this inspection type as there are inspections with this type.");
            }

            _appContext.Inspection_Type.Remove(inspectionType);
            await _appContext.SaveChangesAsync();

            return NoContent();
        }

        private bool InspectionTypeExists(int id)
        {
            return (_appContext.Inspection_Type?.Any(e => e.Inspection_Type_ID == id)).GetValueOrDefault();
        }
    }
}
