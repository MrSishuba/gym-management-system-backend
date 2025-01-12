using av_motion_api.Data;
using av_motion_api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class FreeTrialController : ControllerBase
{
    private readonly AppDbContext _appDbContext;
    private readonly IConfiguration _config;

    public FreeTrialController(AppDbContext context, IConfiguration config)
    {
        _appDbContext = context;
        _config = config;
    }

    public class TrialRequest
    {
        public string Email { get; set; }
    }

    [HttpPost("SendTrialCode")]
    public async Task<IActionResult> SendTrialCode([FromBody] TrialRequest request)
    {
        var trialCode = GenerateTrialCode();
        var emailSent = await SendEmail(request.Email, trialCode);

        if (emailSent)
        {
            return Ok(new { Message = "Free trial code sent to your email." });
        }

        return BadRequest(new { Error = "Failed to send trial code." });
    }


    private string GenerateTrialCode()
    {
        return Guid.NewGuid().ToString().Substring(0, 12); // 12 character trial code
    }

    private async Task<bool> SendEmail(string recipientEmail, string trialCode)
    {
        var apiKey = _config["SendGrid:ApiKey"];
        var senderEmail = _config["SendGrid:SenderEmail"];
        var senderName = _config["SendGrid:SenderName"];

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(senderEmail, senderName);
        var subject = "Your One Week Free Trial Code";
        var to = new EmailAddress(recipientEmail);

        var htmlContent = $@"
                        <strong>Hello There Guest</strong>,<br/><br/>
                        Welcome to AVS Fitness gym and wellness community where we train purpose beyond value.<br/>
                        Give us the chance to show you why we are the best in the business and what we can offer you.<br/>
                        We believe action speaks louder than words and in just one week we can show you what that truly means.<br/><br/>
        
                        Join us free for one week by coming to AVS Fitness gym located at:<br/>
                        <strong>5 Calliiandra St, Montana Park, Pretoria, 0182</strong><br/><br/>
        
                        Meet our Owner and Founder, Audrin Van Schoor, and be prepared for some of the most challenging, effective, and targeted workouts according to your specific fitness level, goals, and ability.<br/>
                        Our professional team is ready to help you while our community is ready to support you in your fitness journey.<br/><br/>
        
                        Test us out for yourself using your one-time free trial code: <strong>{trialCode}</strong><br/><br/>
                        Present your code at the gym along with your original ID card and once your code and creditials are processed by our staff you'll be ready to join us with your one week pass.<br/>
                        Note the code will be eligible only once activated on our system so don't worry about its expiration once you have this email.<br/><br/>
        
                        Come to the gym, and after experiencing the AV Motion community, consider joining the AV Army by becoming a member!<br/><br/>
        
                        <strong>Your AVS Fitness Team</strong><br/>
                        Contact us at: 012 454 5321<br/>
                    ";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, htmlContent, htmlContent);
        var response = await client.SendEmailAsync(msg);
        var responseBody = await response.Body.ReadAsStringAsync(); // Log detailed response

        return response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted;
    }

    [HttpPost("ActivateGuestFreeTrial")]
    public async Task<IActionResult> ActivateGuestFreeTrial([FromBody] GuestSignUpViewModel guestSignUp)
    {
        // Check if the ID_Number or Email matches any existing user in the User table
        var existingUser = await _appDbContext.Users
            .FirstOrDefaultAsync(u => u.ID_Number == guestSignUp.ID_Number || u.Email == guestSignUp.Email);

        if (existingUser != null)
        {
            // Check user type and return an appropriate error message
            switch (existingUser.User_Type_ID)
            {
                case 1: // Owner
                    return BadRequest(new { Error = "Already a registered owner, this feature is only for guests." });

                case 2: // Employee
                    return BadRequest(new { Error = "Already a registered employee, this feature is only for guests." });

                case 3: // Member
                    return BadRequest(new { Error = "Already a registered member, this feature is only for guests." });

                default:
                    return BadRequest(new { Error = "Already a registered user, this feature is only for guests." });
            }
        }

        // Check if there's already a Free_Trial_SignUp with the same Email, ID_Number, or FreeTrialCode
        var existingFreeTrial = await _appDbContext.Free_Trial_SignUps
            .FirstOrDefaultAsync(f => f.Email == guestSignUp.Email || f.ID_Number == guestSignUp.ID_Number || f.FreeTrialCode == guestSignUp.TrialCode);

        if (existingFreeTrial != null)
        {
            // Check if the FreeTrialCode was already used
            if (existingFreeTrial.FreeTrialCode == guestSignUp.TrialCode)
            {
                return BadRequest(new { Error = "This free trial code has already been used and cannot be used again." });
            }

            // If the guest with same email or ID_Number exists
            return BadRequest(new { Error = "A guest with this email or ID number has already activated a free trial." });
        }

        // If no match, proceed to activate guest free trial
        var newGuest = new Free_Trial_SignUp
        {
            Name = guestSignUp.Name,
            Surname = guestSignUp.Surname,
            Email = guestSignUp.Email,
            ID_Number = guestSignUp.ID_Number,
            FreeTrialCode = guestSignUp.TrialCode,
            DateActivated = DateTime.Now,
            DateExpired = DateTime.Now.AddDays(7)
        };

        _appDbContext.Free_Trial_SignUps.Add(newGuest);
        await _appDbContext.SaveChangesAsync();

        return Ok(new { Message = "Free trial activated successfully." });
    }


    public class GuestSignUpViewModel
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string ID_Number { get; set; }
        public string TrialCode { get; set; } // This would be received after email verification
    }

    [HttpGet("GetFreeTrialSignUps")]
    public async Task<IActionResult> GetFreeTrialSignUps()
    {
        var freeTrialSignUps = await _appDbContext.Free_Trial_SignUps.ToListAsync();

        if (freeTrialSignUps == null || freeTrialSignUps.Count == 0)
        {
            return NotFound(new { Message = "No free trial sign-ups found." });
        }

        return Ok(freeTrialSignUps);
    }


}
