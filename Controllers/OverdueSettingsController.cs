using av_motion_api.Data;
using av_motion_api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using NuGet.Configuration;
using System;
using System.Security.Claims;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OverdueSettingsController : ControllerBase
    {
        private readonly AppDbContext _appContext;
        private readonly IOptionsMonitor<OverdueSettings> _overdueSettings;
        private readonly IConfigurationRoot _configurationRoot;

        public OverdueSettingsController(AppDbContext context, IOptionsMonitor<OverdueSettings> overdueSettings, IConfiguration configuration)
        {
            _appContext = context;
            _overdueSettings = overdueSettings;
            _configurationRoot = (IConfigurationRoot)configuration;
        }

        [HttpPost]
        [Route("UpdateOverdueSettings")]
        public async Task <IActionResult> UpdateOverdueSettings([FromBody] OverdueSettings settings)
        {
            if (settings.OverdueTimeValue < 0)
            {
                return BadRequest("Overdue time value must be non-negative");
            }

            var configurationFile = "appsettings.Development.json";
            var configurationPath = Path.Combine(Directory.GetCurrentDirectory(), configurationFile);

            var json = System.IO.File.ReadAllText(configurationPath);
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

            // Retrieve current settings before updating
            var currentOverdueTimeValue = jsonObj["OverdueSettings"]?["OverdueTimeValue"];
            var currentOverdueTimeUnit = jsonObj["OverdueSettings"]?["OverdueTimeUnit"];

            if (jsonObj["OverdueSettings"] == null)
            {
                jsonObj["OverdueSettings"] = new Newtonsoft.Json.Linq.JObject();
            }

            jsonObj["OverdueSettings"]["OverdueTimeValue"] = settings.OverdueTimeValue;
            jsonObj["OverdueSettings"]["OverdueTimeUnit"] = settings.OverdueTimeUnit;

            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(configurationPath, output);


            _configurationRoot.Reload();

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail for updating overdue settings
            var auditTrail = new Audit_Trail
            {
                Transaction_Type = "UPDATE",
                Critical_Data = $"Overdue Settings updated From: DeletionTimeValue '{currentOverdueTimeValue}', DeletionTimeUnit '{currentOverdueTimeUnit}' To OverdueTimeValue '{settings.OverdueTimeValue}', OverdueTimeUnit '{settings.OverdueTimeUnit}'",
                Changed_By = changedBy,
                Table_Name = "OverdueSettings",
                Timestamp = DateTime.UtcNow
            };

            _appContext.Audit_Trails.Add(auditTrail);
            _appContext.SaveChanges();  // Ensure the audit trail is saved

            return Ok(settings);
        }

        [HttpGet]
        [Route("GetOverdueSettings")]
        public IActionResult GetOverdueSettings()
        {
            var settings = new OverdueSettings();
            _configurationRoot.Bind("OverdueSettings", settings);
            if (settings == null)
            {
                return NotFound("Overdue settings not found");
            }
            return Ok(settings);
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
