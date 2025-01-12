using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using av_motion_api.Data;
using av_motion_api.Models;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SurveyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SurveyController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitSurvey([FromBody] SurveyResponse surveyResponse)
        {
            if (ModelState.IsValid)
            {
                _context.SurveyResponses.Add(surveyResponse);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Survey submitted successfully!" });
            }
            return BadRequest(ModelState);
        }

        // Optionally, you can add a method to fetch the survey responses
        [HttpGet("responses")]
        public IActionResult GetSurveyResponses()
        {
            var responses = _context.SurveyResponses.ToList();
            return Ok(responses);
        }

        [HttpGet("export")]
        public IActionResult ExportSurveyResponses()
        {
            var responses = _context.SurveyResponses.ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("SurveyResponses");

                // Set the main title color and font
                var titleCell = worksheet.Cells["A1:G1"];
                titleCell.Merge = true;
                titleCell.Value = "AV Motion Survey Responses";
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.Size = 16;
                titleCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                titleCell.Style.Font.Color.SetColor(Color.White); // Title font color
                titleCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                titleCell.Style.Fill.BackgroundColor.SetColor(Color.Black); // Title background color

                // Add logo
                var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "images", "AVSFitness_Logo.jpeg");
                if (System.IO.File.Exists(logoPath))
                {
                    var logoImage = new FileInfo(logoPath);
                    var logoPicture = worksheet.Drawings.AddPicture("Logo", logoImage);

                    // Adjust the position and size of the logo
                    logoPicture.SetPosition(0, 0, 0, 0); // Position the logo at the top left of the worksheet
                    logoPicture.SetSize(146, 80); // Set a new size (adjust as necessary)

                    // Optionally, you can adjust the row height of the first row to fit the logo better
                    worksheet.Row(1).Height = 80; // Increase height of the first row for the title
                }

                // Headers
                string[] headers = { "Name", "Email", "Membership Status", "Age Group", "Booking Satisfaction", "Product Satisfaction", "Recommend Gym" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var headerCell = worksheet.Cells[2, i + 1];
                    headerCell.Value = headers[i];
                    headerCell.Style.Font.Bold = true;
                    headerCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerCell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#B08F26")); // Header background color
                    headerCell.Style.Font.Color.SetColor(Color.White); // Header font color
                }

                // Data Rows
                for (int i = 0; i < responses.Count; i++)
                {
                    var row = responses[i];
                    worksheet.Cells[i + 3, 1].Value = row.Name;
                    worksheet.Cells[i + 3, 2].Value = row.Email;
                    worksheet.Cells[i + 3, 3].Value = row.MembershipStatus;
                    worksheet.Cells[i + 3, 4].Value = row.AgeGroup;
                    worksheet.Cells[i + 3, 5].Value = row.BookingSatisfaction;
                    worksheet.Cells[i + 3, 6].Value = row.ProductSatisfaction;
                    worksheet.Cells[i + 3, 7].Value = row.RecommendGym;

                    // Optional: Add shading for the rows
                    var rowCell = worksheet.Cells[i + 3, 1, i + 3, 7];
                    rowCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    rowCell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#ccc")); // Row shading color
                }

                // AutoFit columns
                worksheet.Cells.AutoFitColumns();

                // Return the Excel file
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = "SurveyResponses.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}

