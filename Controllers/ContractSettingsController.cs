using av_motion_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractSettingsController : ControllerBase
    {
        private readonly IOptionsMonitor<ContractDeletionSettings> _deletionSettings;
        private readonly ILogger<ContractSettingsController> _logger;

        public ContractSettingsController(IOptionsMonitor<ContractDeletionSettings> deletionSettings, ILogger<ContractSettingsController> logger)
        {
            _deletionSettings = deletionSettings;
            _logger = logger;
        }

        [HttpGet("GetContractSettings")]
        public async Task<ActionResult<ContractDeletionSettings>> GetContractSettings()
        {
            _logger.LogInformation("Fetching contract settings.");

            var appSettingsFile = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json");

            if (!System.IO.File.Exists(appSettingsFile))
            {
                _logger.LogError("App settings file not found at {Path}", appSettingsFile);
                return StatusCode(500, "App settings file not found.");
            }

            try
            {
                var json = await System.IO.File.ReadAllTextAsync(appSettingsFile);
                var jsonObj = JObject.Parse(json);

                var deletionSettingsSection = jsonObj["ContractDeletionSettings"];
                if (deletionSettingsSection == null)
                {
                    _logger.LogWarning("ContractDeletionSettings section not found in the configuration file.");
                    return NotFound(new { message = "ContractDeletionSettings not found in configuration." });
                }

                var settings = new ContractDeletionSettings
                {
                    DeletionTimeValue = (int)deletionSettingsSection["DeletionTimeValue"],
                    DeletionTimeUnit = (string)deletionSettingsSection["DeletionTimeUnit"]
                };

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading contract settings from configuration file.");
                return StatusCode(500, "Error reading contract settings.");
            }
        }

        [HttpPut("UpdateContractDeletionTime")]
        public async Task<IActionResult> UpdateContractDeletionTime([FromBody] ContractDeletionSettings newSettings)
        {
            if (newSettings == null)
            {
                return BadRequest("Invalid settings.");
            }

            _logger.LogInformation("Updating contract deletion time to {Value} {Unit}", newSettings.DeletionTimeValue, newSettings.DeletionTimeUnit);

            // Update the in-memory settings
            // Update the appsettings.Development.json file
            var appSettingsFile = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json");

            if (!System.IO.File.Exists(appSettingsFile))
            {
                _logger.LogError("App settings file not found at {Path}", appSettingsFile);
                return StatusCode(500, "App settings file not found.");
            }

            try
            {
                var json = await System.IO.File.ReadAllTextAsync(appSettingsFile);
                var jsonObj = JObject.Parse(json);

                var deletionSettingsSection = jsonObj["ContractDeletionSettings"];
                deletionSettingsSection["DeletionTimeValue"] = newSettings.DeletionTimeValue;
                deletionSettingsSection["DeletionTimeUnit"] = newSettings.DeletionTimeUnit;

                await System.IO.File.WriteAllTextAsync(appSettingsFile, jsonObj.ToString());

                _logger.LogInformation("Contract deletion time updated successfully.");
                return Ok(newSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contract settings.");
                return StatusCode(500, "Error updating contract settings.");
            }
        }
    }
}

//using av_motion_api.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;
//using Newtonsoft.Json.Linq;
//using System.IO;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;

//namespace av_motion_api.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class ContractSettingsController : ControllerBase
//    {
//        private readonly IOptionsMonitor<ContractDeletionSettings> _deletionSettings;
//        private readonly ILogger<ContractSettingsController> _logger;

//        public ContractSettingsController(IOptionsMonitor<ContractDeletionSettings> deletionSettings, ILogger<ContractSettingsController> logger)
//        {
//            _deletionSettings = deletionSettings;
//            _logger = logger;
//        }

//        [HttpGet("GetContractSettings")]
//        public ActionResult<ContractDeletionSettings> GetContractSettings()
//        {
//            _logger.LogInformation("Fetching contract settings.");
//            return Ok(_deletionSettings.CurrentValue);
//        }

//        [HttpPut("UpdateContractDeletionTime")]
//        public async Task<IActionResult> UpdateContractDeletionTime([FromBody] ContractDeletionSettings newSettings)
//        {
//            if (newSettings == null)
//            {
//                return BadRequest("Invalid settings.");
//            }

//            _logger.LogInformation("Updating contract deletion time to {Value} {Unit}", newSettings.DeletionTimeValue, newSettings.DeletionTimeUnit);

//            // Update the in-memory settings
//            // Update the appsettings.Development.json file
//            var appSettingsFile = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json");

//            if (!System.IO.File.Exists(appSettingsFile))
//            {
//                _logger.LogError("App settings file not found at {Path}", appSettingsFile);
//                return StatusCode(500, "App settings file not found.");
//            }

//            try
//            {
//                var json = await System.IO.File.ReadAllTextAsync(appSettingsFile);
//                var jsonObj = JObject.Parse(json);

//                var deletionSettingsSection = jsonObj["ContractDeletionSettings"];
//                deletionSettingsSection["DeletionTimeValue"] = newSettings.DeletionTimeValue;
//                deletionSettingsSection["DeletionTimeUnit"] = newSettings.DeletionTimeUnit;

//                await System.IO.File.WriteAllTextAsync(appSettingsFile, jsonObj.ToString());

//                _logger.LogInformation("Contract deletion time updated successfully.");
//                return Ok(newSettings);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error updating contract settings.");
//                return StatusCode(500, "Error updating contract settings.");
//            }
//        }
//    }
//}
