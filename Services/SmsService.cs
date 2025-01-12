using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Vonage;
using Vonage.Request;
using Vonage.Messaging;

public class SmsService
{
    private readonly string _apiKey;
    private readonly string _apiSecret;

    public SmsService(IOptions<VonageOptions> options)
    {
        _apiKey = options.Value.ApiKey;
        _apiSecret = options.Value.ApiSecret;
    }

    public async Task SendSms(string to, string message)
    {
        var credentials = Credentials.FromApiKeyAndSecret(_apiKey, _apiSecret);
        var client = new VonageClient(credentials);

        var smsRequest = new SendSmsRequest
        {
            To = to,
            From = "AVS Fitness", // Adjust as necessary
            Text = message
        };

        try
        {
            var response = await client.SmsClient.SendAnSmsAsync(smsRequest);
            // You might want to log the response or handle it as needed
            Console.WriteLine($"SMS Status: {response.Messages[0].Status}");
        }
        catch (VonageHttpRequestException ex)
        {
            // Handle API errors
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

public class VonageOptions
{
    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }
}
