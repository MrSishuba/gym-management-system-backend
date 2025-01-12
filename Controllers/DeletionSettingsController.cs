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
    public class DeletionSettingsController : ControllerBase
    {
        private readonly AppDbContext _appContext;
        private readonly IOptionsMonitor<DeletionSettings> _deletionSettings;
        private readonly IConfigurationRoot _configurationRoot;

        public DeletionSettingsController(AppDbContext context, IOptionsMonitor<DeletionSettings> deletionSettings, IConfiguration configuration)
        {
            _appContext = context;
            _deletionSettings = deletionSettings;
            _configurationRoot = (IConfigurationRoot)configuration;
        }

        [HttpPost]
        [Route("UpdateDeletionTime")]
        public async Task<IActionResult> UpdateDeletionTime([FromBody] DeletionSettings settings)
        {
            if (settings.DeletionTimeValue < 0)
            {
                return BadRequest(new { message = "Deletion time value must be non-negative" });
            }

            var configurationFile = "appsettings.Development.json";
            var configurationPath = Path.Combine(Directory.GetCurrentDirectory(), configurationFile);

            var json = System.IO.File.ReadAllText(configurationPath);
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

            // Retrieve current settings before updating
            var currentDeletionTimeValue = jsonObj["DeletionSettings"]?["DeletionTimeValue"];
            var currentDeletionTimeUnit = jsonObj["DeletionSettings"]?["DeletionTimeUnit"];

            if (jsonObj["DeletionSettings"] == null)
            {
                jsonObj["DeletionSettings"] = new Newtonsoft.Json.Linq.JObject();
            }

            jsonObj["DeletionSettings"]["DeletionTimeValue"] = settings.DeletionTimeValue;
            jsonObj["DeletionSettings"]["DeletionTimeUnit"] = settings.DeletionTimeUnit;

            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(configurationPath, output);

            // Reload the configuration
            _configurationRoot.Reload();

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail for updating deletion time
            var auditTrail = new Audit_Trail
            {
                Transaction_Type = "UPDATE",
                Critical_Data = $"Deletion Settings updated From: DeletionTimeValue '{currentDeletionTimeValue}', DeletionTimeUnit '{currentDeletionTimeUnit}' To: DeletionTimeValue '{settings.DeletionTimeValue}', DeletionTimeUnit '{settings.DeletionTimeUnit}'",
                Changed_By = changedBy,
                Table_Name = "DeletionSettings",
                Timestamp = DateTime.UtcNow
            };

            _appContext.Audit_Trails.Add(auditTrail);
            _appContext.SaveChanges();  // Ensure the audit trail is saved

            return Ok(new { message = "Deletion time updated successfully" });
        }

        [HttpGet]
        [Route("GetDeletionSettings")]
        public IActionResult GetDeletionSettings()
        {
            var settings = _deletionSettings.CurrentValue;
            if (settings == null)
            {
                return NotFound(new { message = "Deletion settings not found" });
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
