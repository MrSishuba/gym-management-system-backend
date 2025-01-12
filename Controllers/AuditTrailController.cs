using av_motion_api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditTrailController : ControllerBase
    {
        private readonly AppDbContext _appContext;

        public AuditTrailController(AppDbContext context)
        {
            _appContext = context;
        }

        [HttpGet("getAuditTrails")]
        public IActionResult GetAuditTrails()
        {
            var auditTrails = _appContext.Audit_Trails.ToList();
            return Ok(auditTrails);
        }
    }
}
