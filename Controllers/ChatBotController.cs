using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatBotController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public ChatBotController()
        {
            _httpClient = new HttpClient();
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskBot([FromBody] string message)
        {
            // Send message to Rasa bot and get a response
            var content = new StringContent("{\"sender\": \"user\", \"message\": \"" + message + "\"}", Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("http://localhost:5005/webhooks/rest/webhook", content); // Assuming Rasa runs on localhost:5005
            var botResponse = await response.Content.ReadAsStringAsync();

            return Ok(botResponse);
        }
    }


}
