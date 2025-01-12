using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;


namespace av_motion_api.Controllers
    {
    [ApiController]
    [Route("api/[controller]")]
    public class WeeklyNewsController : ControllerBase
    {
        // Path to the images directory
        private readonly string imagesDirectory = "C:\\Users\\andas\\source\\repos\\inf370systems-team43\\WeeklyNewsImages";

        [HttpGet("image")]
        public IActionResult GetWeeklyNewsImage()
        {
            // Get all images in the directory that match the specified formats
            var images = Directory.GetFiles(imagesDirectory, "*.*")
                .Where(file => file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                               file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                               file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .ToArray(); // Collect the matching image files into an array

            // Check if there are no images found
            if (images.Length == 0)
                return NotFound();

            // Assuming you want the latest image; you can modify this logic as needed
            var latestImage = images[0];

            // Serve the image URL from the API, assuming a route that serves static files
            return Ok(new { imageUrl = $"https://localhost:7185/WeeklyNewsImages/{Path.GetFileName(latestImage)}" });
        }
    }
}
