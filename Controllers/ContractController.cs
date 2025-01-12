using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using av_motion_api.Models;
using av_motion_api.Data;
using av_motion_api.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Web;
using Microsoft.IdentityModel.Tokens;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly SmsService _smsService;

        public ContractController(AppDbContext context, UserManager<User> userManager, IConfiguration configuration, SmsService smsService)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _smsService = smsService;

        }

        [HttpPost]
        [Route("UploadSignedContract")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadSignedContract([FromForm] ContractSubmissionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Ensure the Member_Name is in the "Name Surname" format
            var nameParts = model.Member_Name?.Split(' ');
            if (nameParts == null || nameParts.Length != 2)
            {
                return BadRequest("Member_Name must be in the 'Name Surname' format.");
            }

            string memberFirstName = nameParts[0];
            string memberLastName = nameParts[1];

            // Find the member based on the provided name and surname
            var member = await _context.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.User.Name == memberFirstName && m.User.Surname == memberLastName);

            if (member == null)
            {
                return NotFound("Member not found.");
            }

            // Check if the member's status is unsubscribed (Membership_Status_ID == 2)
            if (member.Membership_Status_ID != 2)
            {
                return BadRequest("The member is not unsubscribed.");
            }

            // Handling Contract File
            if (model.File != null)
            {
                var fileExtension = Path.GetExtension(model.File.FileName).ToLower();
                if (fileExtension != ".pdf")
                {
                    return BadRequest("Only PDF files are allowed.");
                }

                var signedContractsDir = Path.Combine(Directory.GetCurrentDirectory(), "SignedContracts");
                if (!Directory.Exists(signedContractsDir))
                {
                    Directory.CreateDirectory(signedContractsDir);
                }

                var existingFile = Directory.GetFiles(signedContractsDir, $"{memberFirstName}_{memberLastName}_{member.Member_ID}_*{fileExtension}")
                    .FirstOrDefault();

                if (existingFile != null)
                {
                    return BadRequest("A signed contract for this member already exists.");
                }

                var uniqueFileName = $"{memberFirstName}_{memberLastName}_{member.Member_ID}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(signedContractsDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail for uploading a signed contract
                var audit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"Uploaded signed contract for Member ID '{member.Member_ID}', File Name '{uniqueFileName}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Contract),
                    Timestamp = DateTime.UtcNow
                };

                _context.Audit_Trails.Add(audit);
                await _context.SaveChangesAsync();
            }
            else
            {
                return BadRequest("Contract file is required.");
            }

            // Handling Consent Form File
            if (model.ConsentFormFile != null)
            {
                var fileExtension = Path.GetExtension(model.ConsentFormFile.FileName).ToLower();
                if (fileExtension != ".pdf")
                {
                    return BadRequest("Only PDF files are allowed.");
                }

                var consentFormsDir = Path.Combine(Directory.GetCurrentDirectory(), "ConsentForms");
                if (!Directory.Exists(consentFormsDir))
                {
                    Directory.CreateDirectory(consentFormsDir);
                }

                var consentFormFileName = $"{memberFirstName}_{memberLastName}_{member.Member_ID}_ConsentForm_{Guid.NewGuid()}{fileExtension}";
                var consentFormFilePath = Path.Combine(consentFormsDir, consentFormFileName);

                using (var stream = new FileStream(consentFormFilePath, FileMode.Create))
                {
                    await model.ConsentFormFile.CopyToAsync(stream);
                } 

                // Save consent form record to the database
                var consentForm = new ConsentForm
                {
                    Member_ID = member.Member_ID,
                    Member_Name = $"{memberFirstName} {memberLastName}",
                    FileName = consentFormFileName,
                };

                _context.ConsentForms.Add(consentForm);
                await _context.SaveChangesAsync();
            }
            else
            {
                return BadRequest("Consent form file is required.");
            }

            return Ok(new { message = "Signed contract and consent form uploaded successfully." });
        }




        //[HttpPost]
        //[Route("UploadSignedContract")]
        //[DisableRequestSizeLimit]
        //public async Task<IActionResult> UploadSignedContract([FromForm] ContractSubmissionViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    // Ensure the Member_Name is in the "Name Surname" format
        //    var nameParts = model.Member_Name?.Split(' ');
        //    if (nameParts == null || nameParts.Length != 2)
        //    {
        //        return BadRequest("Member_Name must be in the 'Name Surname' format.");
        //    }

        //    string memberFirstName = nameParts[0];
        //    string memberLastName = nameParts[1];

        //    // Find the member based on the provided name and surname
        //    var member = await _context.Members
        //        .Include(m => m.User)
        //        .FirstOrDefaultAsync(m => m.User.Name == memberFirstName && m.User.Surname == memberLastName);

        //    if (member == null)
        //    {
        //        return NotFound("Member not found.");
        //    }

        //    // Check if the member's status is unsubscribed (Membership_Status_ID == 2)
        //    if (member.Membership_Status_ID != 2)
        //    {
        //        return BadRequest("The member is not unsubscribed.");
        //    }

        //    if (model.File != null)
        //    {
        //        var fileExtension = Path.GetExtension(model.File.FileName).ToLower();
        //        if (fileExtension != ".pdf")
        //        {
        //            return BadRequest("Only PDF files are allowed.");
        //        }

        //        var signedContractsDir = Path.Combine(Directory.GetCurrentDirectory(), "SignedContracts");
        //        if (!Directory.Exists(signedContractsDir))
        //        {
        //            Directory.CreateDirectory(signedContractsDir);
        //        }

        //        // Check for existing file by combining Member_ID with the name and surname
        //        var existingFile = Directory.GetFiles(signedContractsDir, $"{memberFirstName}_{memberLastName}_{member.Member_ID}_*{fileExtension}")
        //            .FirstOrDefault();

        //        if (existingFile != null)
        //        {
        //            return BadRequest("A signed contract for this member already exists.");
        //        }

        //        var uniqueFileName = $"{memberFirstName}_{memberLastName}_{member.Member_ID}_{Guid.NewGuid()}{fileExtension}";
        //        var filePath = Path.Combine(signedContractsDir, uniqueFileName);

        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            await model.File.CopyToAsync(stream);
        //        }

        //        // Retrieve the changed by information
        //        var changedBy = await GetChangedByAsync();

        //        // Log audit trail for uploading a signed contract
        //        var audit = new Audit_Trail
        //        {
        //            Transaction_Type = "INSERT",
        //            Critical_Data = $"Uploaded signed contract for Member ID '{member.Member_ID}', File Name '{uniqueFileName}'",
        //            Changed_By = changedBy,
        //            Table_Name = nameof(Contract),
        //            Timestamp = DateTime.UtcNow
        //        };

        //        _context.Audit_Trails.Add(audit);
        //        await _context.SaveChangesAsync();

        //        return Ok(new { message = "Signed contract uploaded successfully.", filePath });
        //    }
        //    else
        //    {
        //        return BadRequest("File is required.");
        //    }
        //}

        [HttpPost]
        [Route("RemoveContractFile")]
        public async Task<IActionResult> RemoveContractFile([FromForm] RemoveContractViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Member_Name) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest("Member_Name and Password are required.");
            }

            var nameParts = model.Member_Name.Split(' ');
            if (nameParts.Length != 2)
            {
                return BadRequest("Member_Name must be in the 'Name Surname' format.");
            }

            string memberFirstName = nameParts[0];
            string memberLastName = nameParts[1];

            // Validate the password using the proper password validation method
            if (!await ValidatePasswordAsync(model.Password))
            {
                return Unauthorized("Invalid password.");
            }

            // Define the directories for SignedContracts and ConsentForms
            var signedContractsDir = Path.Combine(Directory.GetCurrentDirectory(), "SignedContracts");
            var consentFormsDir = Path.Combine(Directory.GetCurrentDirectory(), "ConsentForms");

            // Check if SignedContracts directory exists
            if (!Directory.Exists(signedContractsDir))
            {
                return NotFound("SignedContracts directory not found.");
            }

            // Remove contract file from SignedContracts
            var contractFileToRemove = Directory.GetFiles(signedContractsDir, $"{memberFirstName}_{memberLastName}_*.pdf")
                .FirstOrDefault();

            if (contractFileToRemove == null)
            {
                return NotFound("No contract file found for the given Member_Name.");
            }

            System.IO.File.Delete(contractFileToRemove);

            // Check if ConsentForms directory exists
            if (!Directory.Exists(consentFormsDir))
            {
                return NotFound("ConsentForms directory not found.");
            }

            // Remove consent form file from ConsentForms
            var consentFormFileToRemove = Directory.GetFiles(consentFormsDir, $"{memberFirstName}_{memberLastName}_*.pdf")
                .FirstOrDefault();

            if (consentFormFileToRemove != null)
            {
                System.IO.File.Delete(consentFormFileToRemove);
            }

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail for removing both files
            var audit = new Audit_Trail
            {
                Transaction_Type = "DELETE",
                Critical_Data = $"Removed contract file: {contractFileToRemove} and consent form file: {consentFormFileToRemove ?? "No Consent Form Found"} for Member Name: {model.Member_Name}",
                Changed_By = changedBy,
                Table_Name = nameof(Contract),
                Timestamp = DateTime.UtcNow
            };

            _context.Audit_Trails.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Contract and consent form files removed successfully." });
        }


       
        // Password validation method
        private async Task<bool> ValidatePasswordAsync(string password)
        {
            // Retrieve the current hashed password from the database
            var passwordRecord = await _context.Contract_Securities.FirstOrDefaultAsync();

            if (passwordRecord == null)
            {
                return false;
            }

            // Hash the provided password
            string hashedInputPassword = HashPassword(password);

            // Compare the hashed password with the one stored in the database
            return hashedInputPassword == passwordRecord.HashedPassword;
        }


        [HttpPost]
        [Route("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Retrieve the current hashed password from the database
            var passwordRecord = await _context.Contract_Securities.FirstOrDefaultAsync();

            if (passwordRecord == null)
            {
                // No existing record found, create a new one
                var newPasswordRecord = new Contract_Security
                {
                    HashedPassword = HashPassword(model.NewPassword),
                    LastUpdated = DateTime.UtcNow
                };

                _context.Contract_Securities.Add(newPasswordRecord);
                //await _context.SaveChangesAsync();

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail for creating a new password
                var audit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"New contract security password set",
                    Changed_By = changedBy,
                    Table_Name = nameof(Contract),
                    Timestamp = DateTime.UtcNow
                };

                _context.Audit_Trails.Add(audit);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Password set successfully." });
            }

            // Hash the provided current password
            string hashedCurrentPassword = HashPassword(model.CurrentPassword);

            // Validate the current password
            if (hashedCurrentPassword != passwordRecord.HashedPassword)
            {
                return Unauthorized("Current password is incorrect.");
            }

            // Hash the new password
            string hashedNewPassword = HashPassword(model.NewPassword);

            // Update the hashed password in the database
            passwordRecord.HashedPassword = hashedNewPassword;
            _context.Contract_Securities.Update(passwordRecord);
            //await _context.SaveChangesAsync();

            // Retrieve the changed by information
            var changedByUpdate = await GetChangedByAsync();

            // Log audit trail for updating the password
            var auditUpdate = new Audit_Trail
            {
                Transaction_Type = "UPDATE",
                Critical_Data = $"Contract security password updated",
                Changed_By = changedByUpdate,
                Table_Name = nameof(Contract),
                Timestamp = DateTime.UtcNow
            };

            _context.Audit_Trails.Add(auditUpdate);
            await _context.SaveChangesAsync();

            // After password change logic
            var employees = await _context.Employees.Include(e => e.User).ToListAsync();
            foreach (var employee in employees)
            {
                var phoneNumber = $"+27{employee.User.PhoneNumber.Substring(1)}"; // Format to international
                var smsMessage = $"Dear Employees, note the new password for member form removal is: {model.NewPassword}. Please use this from now on.";

                try
                {
                    await _smsService.SendSms(phoneNumber, smsMessage);
                }
                catch (Exception ex)
                {
                    // Handle error (e.g., log it)
                    Console.WriteLine($"Failed to send SMS to {phoneNumber}: {ex.Message}");
                }
            }


            return Ok(new { message = "Password updated successfully." });
        }

        // Method to hash the password
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        [HttpGet]
        [Route("RetrieveSignedContract")]
        public async Task<IActionResult> RetrieveSignedContract(int memberId)
        {
            // Validate the member ID
            if (memberId <= 0)
            {
                return BadRequest("Invalid member ID.");
            }

            // Check if there are any active contracts for the member
            bool hasActiveContract = await _context.Contracts
                .AnyAsync(c => c.Member_ID == memberId && c.Approval_Status);

            // Check if there are any historical contracts for the member
            bool hasHistoricalContract = await _context.Contract_History
                .AnyAsync(ch => ch.Member_ID == memberId);

            // Ensure that there are no active or historical contracts for the member
            if (hasActiveContract || hasHistoricalContract)
            {
                return NotFound("Member has an active or historical contract, cannot retrieve signed contract.");
            }

            // Define the directory where signed contracts are stored
            var signedContractsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "SignedContracts");

            // Check if the directory exists
            if (!Directory.Exists(signedContractsDirectory))
            {
                return NotFound("Signed contracts directory does not exist.");
            }

            // Search for the contract file that contains the member ID in the specific position
            var contractFilePath = Directory.GetFiles(signedContractsDirectory)
                                             .FirstOrDefault(file =>
                                             {
                                                 var fileName = Path.GetFileNameWithoutExtension(file);
                                                 var parts = fileName.Split('_');
                                                 return parts.Length >= 3 &&
                                                        parts[2].Equals(memberId.ToString(), StringComparison.OrdinalIgnoreCase);
                                             });

            if (contractFilePath == null)
            {
                return NotFound("No signed contract found for the provided member ID.");
            }

            // Return the file path as a string
            return Ok(new { filePath = contractFilePath });
        }

        [HttpPost]
        [Route("CreateContract")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> CreateContract([FromForm] ContractViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify Member_ID exists
            var member = await _context.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.Member_ID == model.Member_ID);
            if (member == null || member.User == null)
            {
                return NotFound("Member or associated user not found.");
            }

            // Verify Employee_ID exists
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Employee_ID == model.Employee_ID);
            if (employee == null)
            {
                return NotFound("Employee not found.");
            }

            // Validate Approval_By format and match with Employee or Owner
            var nameParts = model.Approval_By?.Split(' ');
            if (nameParts == null || nameParts.Length != 2)
            {
                return BadRequest("Approval_By must be in the 'Name Surname' format.");
            }

            string approverFirstName = nameParts[0];
            string approverLastName = nameParts[1];

            var approverValid = employee.User.Name == approverFirstName && employee.User.Surname == approverLastName;

            // Validate Contract_Type_ID is 1, 2, or 3
            if (model.Contract_Type_ID < 1 || model.Contract_Type_ID > 3)
            {
                return BadRequest("Contract_Type_ID must be 1, 2, or 3.");
            }

            // Ensure Subscription_Date is today or in the future
            if (model.Subscription_Date.Date < DateTime.UtcNow.Date)
            {
                return BadRequest("Subscription_Date cannot be before today's date.");
            }

            // Set Approval_Date to today's date and ensure Subscription_Date matches Approval_Date
            var approvalDate = DateTime.UtcNow.Date;
            if (model.Subscription_Date.Date != approvalDate)
            {
                return BadRequest("Subscription_Date must be the same as today's date.");
            }

            // Validate Contract_Type_ID to determine contract length
            var contractLengthMonths = model.Contract_Type_ID switch
            {
                1 => 3,
                2 => 6,
                3 => 12,
                _ => 0
            };

            // Calculate expected expiry date based on contract length
            var expectedEndDate = model.Subscription_Date.AddMonths(contractLengthMonths);
            if (model.Expiry_Date != expectedEndDate)
            {
                return BadRequest($"Subscription_Date and Expiry_Date do not match the contract type length of {contractLengthMonths} months.");
            }

            // Check if the member already has an active contract
            var activeContract = await _context.Contracts
                .Where(c => c.Member_ID == model.Member_ID && c.Expiry_Date > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (activeContract != null)
            {
                return BadRequest("Member already has an active contract.");
            }

            // Ensure Filepath is not null or empty
            if (string.IsNullOrEmpty(model.Filepath))
            {
                return BadRequest("Filepath must be provided.");
            }

            // Create and save the contract
            var contract = new Contract
            {
                Member_ID = model.Member_ID,
                Subscription_Date = model.Subscription_Date,
                Expiry_Date = model.Expiry_Date,
                Approval_Date = approvalDate,
                Terms_Of_Agreement = model.Terms_Of_Agreement,
                Approval_Status = model.Approval_Status,
                Approval_By = model.Approval_By,
                Contract_Type_ID = model.Contract_Type_ID,
                Payment_Type_ID = model.Payment_Type_ID,
                Employee_ID = model.Employee_ID,
                Owner_ID = model.Owner_ID,
                Filepath = model.Filepath
            };

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();

            // Get the user who made the change
            var changedBy = await GetChangedByAsync();

            // Audit trail entry
            var auditTrail = new Audit_Trail
            {
                Transaction_Type = "INSERT",
                Critical_Data = $"Contract created: ID '{contract.Contract_ID}', Member ID '{contract.Member_ID}'",
                Changed_By = changedBy,
                Table_Name = nameof(Contract),
                Timestamp = DateTime.UtcNow
            };
            _context.Audit_Trails.Add(auditTrail);
            await _context.SaveChangesAsync();

            // Now trigger the ApproveContract method
            var approveModel = new ApproveContractViewModel
            {
                Contract_ID = contract.Contract_ID,
                Member_ID = contract.Member_ID
            };

            var result = await ApproveContract(approveModel);
            if (result is ObjectResult objResult && objResult.StatusCode == 200)
            {
                return Ok(new { message = "Contract created and approved successfully.", filePath = model.Filepath });
            }
            else
            {
                return StatusCode(500, "Contract created, but failed to approve the contract.");
            }



        }


        [HttpPost]
        [Route("ApproveContract")]
        public async Task<IActionResult> ApproveContract([FromBody] ApproveContractViewModel model)
        {
            // Include the User navigation property when querying for the member
            var member = await _context.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Member_ID == model.Member_ID);

            if (member == null)
            {
                return NotFound("Member not found.");
            }

            var contract = await _context.Contracts.FindAsync(model.Contract_ID);
            if (contract == null)
            {
                return NotFound("Contract not found.");
            }

            // Ensure User is not null before accessing its properties
            if (member.User == null)
            {
                return NotFound("Associated user not found.");
            }

            // Set both User_Status_ID and Membership_Status_ID to 1
            member.User.User_Status_ID = 1; // Activated
            member.Membership_Status_ID = 1; // Subscribed
            contract.Approval_Status = true;
            contract.Terms_Of_Agreement = true;


            _context.Contracts.Update(contract);
            _context.Members.Update(member);
            await _context.SaveChangesAsync();

            // Get the user who made the change
            var changedBy = await GetChangedByAsync();

            // Audit trail entry
            var auditTrail = new Audit_Trail
            {
                Transaction_Type = "UPDATE",
                Critical_Data = $"Contract approved: ID '{contract.Contract_ID}', Member ID '{contract.Member_ID}'",
                Changed_By = changedBy,
                Table_Name = nameof(Contract),
                Timestamp = DateTime.UtcNow
            };
            _context.Audit_Trails.Add(auditTrail);
            await _context.SaveChangesAsync();

            // Send approval email
            var approvalEmailModel = new ApprovalEmailRequestViewModel
            {
                Email = member.User.Email,
                SubscriptionDate = contract.Subscription_Date
            };

            var emailResponse = await SendApprovalEmail(approvalEmailModel);

            // Force a return Ok immediately to ensure that it captures the 200 status code
            if (emailResponse is ObjectResult objectResult && objectResult.StatusCode == 200)
            {
                return Ok("Email was successfully sent.");
            }

            // Now recheck and perform the logic based on the captured status code
            if (emailResponse is ObjectResult objResult && objResult.StatusCode == 200)
            {
                return Ok(new { message = "Contract approved, member activated, and approval email sent." });
            }
            else
            {
                return StatusCode(500, "Contract approved, but failed to send approval email.");
            }
        }

        [HttpGet]
        [Route("GetSignedContract/{memberId}")]
        public async Task<IActionResult> GetSignedContract(int memberId)
        {
            var contract = await _context.Contracts
                .Include(c => c.Contract_Type)
                .Include(c => c.Member)
                .ThenInclude(m => m.User)
                .Include(c => c.Employee)
                .ThenInclude(e => e.User)
                .Where(c => c.Member_ID == memberId && c.Approval_Status && !c.IsTerminated)
                .Select(c => new
                {
                    c.Contract_ID,
                    MemberID = c.Member.Member_ID,
                    MemberName = c.Member.User.Name,
                    MemberSurname = c.Member.User.Surname,
                    MemberPhone = c.Member.User.PhoneNumber,
                    MemberIDNumber = c.Member.User.ID_Number,
                    SubscriptionDate = c.Subscription_Date.ToString("yyyy-MM-dd"),
                    ExpiryDate = c.Expiry_Date.ToString("yyyy-MM-dd"),
                    EmployeeID = c.Employee.Employee_ID,
                    EmployeeName = c.Employee.User.Name,
                    EmployeeSurname = c.Employee.User.Surname,
                    ApprovalDate = c.Approval_Date.HasValue ? c.Approval_Date.Value.ToString("yyyy-MM-dd") : null,
                    ExpectedPaymentDate = c.Subscription_Date.AddDays(30).ToString("yyyy-MM-dd"),
                    FinalDuePaymentDate = c.Expiry_Date.AddDays(30).ToString("yyyy-MM-dd"),
                    ContractTypeName = c.Contract_Type.Contract_Type_Name
                })
                .FirstOrDefaultAsync();

            if (contract == null)
            {
                return NotFound("Signed contract not found.");
            }

            return Ok(contract);
        }

        [HttpGet]
        [Route("GetUnapprovedContracts")]
        public async Task<IActionResult> GetUnapprovedContracts()
        {
            var unapprovedContracts = await _context.Contracts
                .Where(c => c.Approval_Status == false && c.IsTerminated == false && c.Member.Membership_Status_ID == 2)
                .Include(c => c.Member)
                .ThenInclude(m => m.User)
                .Select(c => new
                {
                    c.Contract_ID,
                    c.Member_ID,
                    MemberName = c.Member.User.Name,
                    MemberSurname = c.Member.User.Surname,
                    Username = c.Member.User.UserName,
                    IDNumber = c.Member.User.ID_Number
                })
                .ToListAsync();

            if (unapprovedContracts.Count == 0)
            {
                return NotFound("No unapproved contracts found.");
            }

            return Ok(unapprovedContracts);
        }

        [HttpGet]
        [Route("GetMembersWithUploadedContractsButNoContractRecord")]
        public async Task<IActionResult> GetMembersWithUploadedContractsButNoContractRecord()
        {
            var signedContractsPath = Path.Combine("SignedContracts"); // Adjust this path as needed
            var signedContractFiles = Directory.GetFiles(signedContractsPath);

            var membersWithNoContractRecord = new List<object>();

            foreach (var filePath in signedContractFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var parts = fileName.Split('_');

                if (parts.Length == 4) // Expecting "Name_Surname_MemberID_Guid"
                {
                    var name = parts[0];
                    var surname = parts[1];
                    if (int.TryParse(parts[2], out int memberId))
                    {
                        // Check if the member exists and has the correct Member_ID
                        var member = await _context.Members
                            .Include(m => m.User)
                            .Where(m => m.Member_ID == memberId && m.User.Name == name && m.User.Surname == surname)
                            .Select(m => new
                            {
                                m.Member_ID,
                                MemberName = m.User.Name,
                                MemberSurname = m.User.Surname
                            })
                            .FirstOrDefaultAsync();

                        if (member != null)
                        {
                            // Check if the member has any associated contract in the Contracts table
                            var hasContract = await _context.Contracts.AnyAsync(c => c.Member_ID == memberId);
                            if (!hasContract)
                            {
                                membersWithNoContractRecord.Add(member);
                            }
                        }
                    }
                }
            }

            if (membersWithNoContractRecord.Count == 0)
            {
                return NotFound("No members with uploaded contracts but no contract record found.");
            }

            return Ok(membersWithNoContractRecord);
        }



        //[HttpGet]
        //[Route("GetUnapprovedContracts")]
        //public async Task<IActionResult> GetUnapprovedContracts()
        //{
        //    var unapprovedContracts = await _context.Contracts
        //        .Where(c => c.Approval_Status == false)
        //        .Include(c => c.Member)
        //        .ThenInclude(m => m.User)
        //        .Select(c => new
        //        {
        //            c.Contract_ID,
        //            c.Member_ID,
        //            MemberName = c.Member.User.Name,
        //            MemberSurname = c.Member.User.Surname,
        //            Username = c.Member.User.UserName,
        //            IDNumber = c.Member.User.ID_Number
        //        })
        //        .ToListAsync();

        //    if (unapprovedContracts.Count == 0)
        //    {
        //        return NotFound("No unapproved contracts found.");
        //    }

        //    return Ok(unapprovedContracts);
        //}

        [HttpDelete]
        [Route("DiscardUnapprovedContract")]
        public async Task<IActionResult> DiscardUnapprovedContract([FromBody] DiscardContractViewModel model)
        {
            // Validate that Contract_ID and Member_ID are provided
            if (model.Contract_ID <= 0 || model.Member_ID <= 0)
            {
                return BadRequest("Contract_ID and Member_ID are required.");
            }

            // Find the contract and related member
            var contract = await _context.Contracts
                .Include(c => c.Member)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.Contract_ID == model.Contract_ID && c.Member_ID == model.Member_ID && c.Approval_Status == false);

            if (contract == null)
            {
                return NotFound("Unapproved contract not found.");
            }

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail
            var audit = new Audit_Trail
            {
                Transaction_Type = "DELETE",
                Critical_Data = $"Contract Discarded: ID '{contract.Contract_ID}', Member '{contract.Member.User.Name} {contract.Member.User.Surname}', Approval Status '{contract.Approval_Status}'",
                Changed_By = changedBy,
                Table_Name = nameof(Contract),
                Timestamp = DateTime.UtcNow
            };

            // Construct the expected file name
            var signedContractsDir = Path.Combine(Directory.GetCurrentDirectory(), "SignedContracts");
            var expectedFileName = $"{contract.Member.User.Name.Trim()}_{contract.Member.User.Surname.Trim()}";
            var matchingFiles = Directory.GetFiles(signedContractsDir, $"{expectedFileName}_*.pdf");

            // Remove the contract file if it exists
            if (matchingFiles.Length > 0)
            {
                System.IO.File.Delete(matchingFiles[0]);
            }

            // Remove the contract record from the database
            _context.Contracts.Remove(contract);

            // Add audit trail entry
            _context.Audit_Trails.Add(audit);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Unapproved contract discarded successfully." });
        }

        [HttpGet]
        [Route("GetAllSignedContracts")]
        public async Task<IActionResult> GetAllSignedContracts()
        {
            // Get all contracts that are approved and not terminated
            var contracts = await _context.Contracts
                                          .Where(c => c.Approval_Status && !c.IsTerminated)
                                          .ToListAsync();

            if (contracts == null || contracts.Count == 0)
            {
                return NotFound("No signed contracts found.");
            }

            return Ok(contracts);
        }


        //[HttpGet]
        //[Route("GetAllSignedContracts")]
        //public async Task<IActionResult> GetAllSignedContracts()
        //{
        //    // Get all contracts that are approved
        //    var contracts = await _context.Contracts
        //                                  .Where(c => c.Approval_Status)
        //                                  .ToListAsync();
        //    if (contracts == null || contracts.Count == 0)
        //    {
        //        return NotFound("No signed contracts found.");
        //    }

        //    return Ok(contracts);
        //}

        [HttpPost]
        [Route("TerminateContract")]
        public async Task<IActionResult> TerminateContract([FromBody] TerminateContractViewModel model)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Fetch the contract
                    var contract = await _context.Contracts
                        .Include(c => c.Member)
                        .FirstOrDefaultAsync(c => c.Contract_ID == model.Contract_ID && c.Member_ID == model.Member_ID);

                    if (contract == null)
                    {
                        return NotFound("Contract not found.");
                    }

                    // Move contract to history
                    var contractHistory = new Contract_History
                    {
                        Contract_ID = contract.Contract_ID,
                        Member_ID = contract.Member_ID,
                        Subscription_Date = contract.Subscription_Date,
                        Expiry_Date = contract.Expiry_Date,
                        Approval_Date = contract.Approval_Date,
                        Terms_Of_Agreement = true,
                        Approval_Status = true,
                        Approval_By = contract.Approval_By,
                        Contract_Type_ID = contract.Contract_Type_ID,
                        Payment_Type_ID = contract.Payment_Type_ID,
                        Employee_ID = contract.Employee_ID,
                        Owner_ID = contract.Owner_ID,
                        Filepath = contract.Filepath,
                        IsTerminated = true, // Set IsTerminated to true
                        Termination_Reason = model.Reason,
                        Termination_Reason_Type = model.Termination_Reason_Type.ToString(),
                        Termination_Date = DateTime.UtcNow
                    };

                    _context.Contract_History.Add(contractHistory);
                    await _context.SaveChangesAsync();  // Save changes immediately after adding to history

                    // Retrieve the changed by information
                    var changedBy = await GetChangedByAsync();

                    // Log audit trail for contract history addition
                    var auditHistory = new Audit_Trail
                    {
                        Transaction_Type = "INSERT",
                        Critical_Data = $"Contract History Added: ID '{contractHistory.Contract_ID}', Member ID '{contract.Member_ID}', Termination Reason '{contractHistory.Termination_Reason}'",
                        Changed_By = changedBy,
                        Table_Name = nameof(Contract_History),
                        Timestamp = DateTime.UtcNow
                    };
                    _context.Audit_Trails.Add(auditHistory);

                    // Mark contract as terminated instead of removing it
                    contract.IsTerminated = true;
                    _context.Contracts.Update(contract);

                    // Update member status
                    var member = await _context.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.Member_ID == model.Member_ID);
                    if (member != null)
                    {
                        member.User.User_Status_ID = 2; // Deactivated
                        member.Membership_Status_ID = 2; // Unsubscribed
                        _context.Members.Update(member);

                        // Log audit trail for member update
                        var auditMember = new Audit_Trail
                        {
                            Transaction_Type = "UPDATE",
                            Critical_Data = $"Member Updated: ID '{member.Member_ID}', New Status 'Deactivated', Membership Status 'Unsubscribed'",
                            Changed_By = changedBy,
                            Table_Name = nameof(Member),
                            Timestamp = DateTime.UtcNow
                        };
                        _context.Audit_Trails.Add(auditMember);
                    }

                    await _context.SaveChangesAsync();  // Save changes again after updating member status

                    await transaction.CommitAsync();

                    // Send termination email
                    var terminationEmailModel = new TerminationEmailRequestViewModel
                    {
                        Email = member.User.Email,
                        TerminationReasonType = model.Termination_Reason_Type.ToString(),
                        TerminationReason = model.Reason,
                        TerminationDate = DateTime.UtcNow
                    };

                    var emailResponse = await SendTerminationEmail(terminationEmailModel);
                    if (!(emailResponse is OkObjectResult))
                    {
                        return StatusCode(500, "Contract terminated and moved to history, but failed to send termination email.");
                    }

                    return Ok(new { message = "Contract terminated, moved to history, and termination email sent." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }
        }

        [HttpPost]
        [Route("ApproveRequestedTermination")]
        public async Task<IActionResult> ApproveRequestedTermination([FromBody] TerminateContractRequestViewModel model)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Fetch the contract
                    var contract = await _context.Contracts
                        .Include(c => c.Member)
                        .FirstOrDefaultAsync(c => c.Contract_ID == model.Contract_ID && c.Member_ID == model.Member_ID);

                    if (contract == null)
                    {
                        return NotFound("Contract not found.");
                    }

                    // Move contract to history
                    var contractHistory = new Contract_History
                    {
                        Contract_ID = contract.Contract_ID,
                        Member_ID = contract.Member_ID,
                        Subscription_Date = contract.Subscription_Date,
                        Expiry_Date = contract.Expiry_Date,
                        Approval_Date = contract.Approval_Date,
                        Terms_Of_Agreement = true,
                        Approval_Status = true,
                        Approval_By = contract.Approval_By,
                        Contract_Type_ID = contract.Contract_Type_ID,
                        Payment_Type_ID = contract.Payment_Type_ID,
                        Employee_ID = contract.Employee_ID,
                        Owner_ID = contract.Owner_ID,
                        Filepath = contract.Filepath,
                        IsTerminated = true, // Set IsTerminated to true
                        Termination_Reason = model.CustomReason, // Use CustomReason from the view model
                        Termination_Reason_Type = model.Requested_Termination_Reason_Type.ToString(), // Use Requested_Termination_Reason_Type from the view model
                        Termination_Date = DateTime.UtcNow
                    };

                    _context.Contract_History.Add(contractHistory);
                    await _context.SaveChangesAsync();  // Save changes immediately after adding to history

                    // Retrieve the changed by information
                    var changedBy = await GetChangedByAsync();

                    // Log audit trail for contract history addition
                    var auditHistory = new Audit_Trail
                    {
                        Transaction_Type = "INSERT",
                        Critical_Data = $"Contract History Added: ID '{contractHistory.Contract_ID}', Member ID '{contract.Member_ID}', Termination Reason '{contractHistory.Termination_Reason}'",
                        Changed_By = changedBy,
                        Table_Name = nameof(Contract_History),
                        Timestamp = DateTime.UtcNow
                    };
                    _context.Audit_Trails.Add(auditHistory);

                    // Mark contract as terminated instead of removing it
                    contract.IsTerminated = true;
                    _context.Contracts.Update(contract);

                    // Update member status
                    var member = await _context.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.Member_ID == model.Member_ID);
                    if (member != null)
                    {
                        member.User.User_Status_ID = 2; // Deactivated
                        member.Membership_Status_ID = 2; // Unsubscribed
                        _context.Members.Update(member);

                        // Log audit trail for member update
                        var auditMember = new Audit_Trail
                        {
                            Transaction_Type = "UPDATE",
                            Critical_Data = $"Member Updated: ID '{member.Member_ID}', New Status 'Deactivated', Membership Status 'Unsubscribed'",
                            Changed_By = changedBy,
                            Table_Name = nameof(Member),
                            Timestamp = DateTime.UtcNow
                        };
                        _context.Audit_Trails.Add(auditMember);
                    }

                    await _context.SaveChangesAsync();  // Save changes again after updating member status

                    await transaction.CommitAsync();

                    // Send termination email
                    var terminationEmailModel = new TerminationEmailRequestViewModel
                    {
                        Email = member.User.Email,
                        TerminationReasonType = model.Requested_Termination_Reason_Type.ToString(),
                        TerminationReason = model.CustomReason,
                        TerminationDate = DateTime.UtcNow
                    };

                    var emailResponse = await SendRequestedTerminationEmail(terminationEmailModel);
                    if (!(emailResponse is OkObjectResult))
                    {
                        return StatusCode(500, "Contract terminated and moved to history, but failed to send termination email.");
                    }

                    return Ok(new { message = "Contract terminated, moved to history, and termination email sent." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }
        }

        [HttpPost]
        [Route("SendRequestedTerminationEmail")]
        public async Task<IActionResult> SendRequestedTerminationEmail([FromBody] TerminationEmailRequestViewModel model)
        {
            var client = new SendGridClient(_configuration["SendGrid:ApiKey"]);
            var from = new EmailAddress(_configuration["SendGrid:SenderEmail"], _configuration["SendGrid:SenderName"]);
            var subject = "We Are Sad To See You Go"; // Updated subject line
            var to = new EmailAddress(model.Email);

            var htmlContent = $"<strong>Notice of contract termination: {model.TerminationReasonType}</strong><br><br>" +
                              $"Your contract has been terminated effective as of <strong>{model.TerminationDate:yyyy/MM/dd}</strong>, due to: <strong>{model.TerminationReason}</strong>.<br><br>" +
                              $"Please note all terminations of contracts are final and cannot be appealed. No further communication will be responded to on this matter.<br><br>" +
                              $"For more information about AVS Fitness rules and regulations, please see our <a href='[Community Guidelines Link]'>community guidelines</a>.";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, htmlContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            // Log the response from SendGrid
            var responseBody = await response.Body.ReadAsStringAsync();
            Console.WriteLine($"SendGrid Response: {responseBody}");

            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                return Ok("Termination email sent.");
            }
            else
            {
                return StatusCode((int)response.StatusCode, $"Error sending termination email. Response: {responseBody}");
            }
        }





        [HttpGet("GetMemberAndContractIds/{userId}")]
        public IActionResult GetMemberAndContractIds(int userId)
        {
            var member = _context.Members.FirstOrDefault(m => m.User_ID == userId);

            if (member == null)
            {
                return NotFound("Member not found.");
            }

            var contract = _context.Contracts.FirstOrDefault(c =>
                c.Member_ID == member.Member_ID &&
                c.Approval_Status == true &&
                c.Terms_Of_Agreement == true &&
                c.IsTerminated == false);

            if (contract == null)
            {
                return NotFound("Active contract not found for this member.");
            }

            var response = new MemberContractResponse
            {
                Member_ID = member.Member_ID,
                Contract_ID = contract.Contract_ID
            };

            return Ok(response);
        }


        [HttpGet("GetMemberAndContractIdsByIdNumber/{idNumber}")]
        public IActionResult GetMemberAndContractIdsByIdNumber(string idNumber)
        {
            // Find the user based on the provided ID_Number
            var user = _context.Users.FirstOrDefault(u => u.ID_Number == idNumber);

            if (user == null)
            {
                return NotFound("User with the provided ID Number not found.");
            }

            // Find the member record associated with this user
            var member = _context.Members.FirstOrDefault(m => m.User_ID == user.User_ID);

            if (member == null)
            {
                return NotFound("Member record not found for the provided User ID.");
            }

            // Find an active contract for this member
            var contract = _context.Contracts.FirstOrDefault(c =>
                c.Member_ID == member.Member_ID &&
                c.Approval_Status == true &&
                c.Terms_Of_Agreement == true &&
                c.IsTerminated == false);

            if (contract == null)
            {
                return NotFound("Active contract not found for this member.");
            }

            var response = new MemberContractResponse
            {
                Member_ID = member.Member_ID,
                Contract_ID = contract.Contract_ID
            };

            return Ok(response);
        }

        [HttpGet]
        [Route("GetTerminationRequests")]
        public async Task<IActionResult> GetTerminationRequests()
        {
            var terminationRequests = await _context.TerminationRequests
                .Include(tr => tr.Contract)  // Include contract details
                .Include(tr => tr.Member)    // Include member details
                .ThenInclude(m => m.User)    // Include user details within member
                .Where(tr => tr.Contract.Approval_Status == true &&  // Filter by approval status
                            tr.Contract.Terms_Of_Agreement == true && // Filter by terms of agreement
                            tr.Contract.IsTerminated == false) // Filter by termination status
                .Select(tr => new TerminationRequestListViewModel
                {
                    Contract_ID = tr.Contract_ID,
                    Member_ID = tr.Member_ID,
                    ContractTypeDescription = tr.Contract.Contract_Type.Contract_Type_Name, // Adjust property name if necessary
                    MemberName = $"{tr.Member.User.Name} {tr.Member.User.Surname}",
                    RequestedTerminationReasonType = tr.Requested_Termination_Reason_Type.ToString(),
                    CustomReason = tr.CustomReason
                })
                .ToListAsync();

            return Ok(terminationRequests);
        }

        public class TerminationRequestListViewModel
        {
            public int Contract_ID { get; set; }
            public int Member_ID { get; set; }
            public string ContractTypeDescription { get; set; }
            public string MemberName { get; set; }
            public string RequestedTerminationReasonType { get; set; }
            public string CustomReason { get; set; }
        }

        [HttpPost]
        [Route("TerminateContractRequest")]
        public async Task<IActionResult> TerminateContractRequest([FromBody] TerminateContractRequestViewModel model)
        {
            // Validate the custom reason if the termination reason type is 'Custom'
            if (model.Requested_Termination_Reason_Type == RequestedTerminationReasonType.Custom && string.IsNullOrWhiteSpace(model.CustomReason))
            {
                return BadRequest("Custom reason must be provided if 'Custom' is selected as the termination reason.");
            }

            // Ensure the member has a valid and active contract
            var contract = await _context.Contracts
                .Include(c => c.Member)
                .ThenInclude(m => m.User) // Ensure the User is included for email details
                .FirstOrDefaultAsync(c => c.Contract_ID == model.Contract_ID && c.Member_ID == model.Member_ID && !c.IsTerminated);

            if (contract == null)
            {
                return NotFound("No active contract found for this member.");
            }

            if (contract.Member.Membership_Status_ID != 1) // Assuming 1 means 'Subscribed'
            {
                return BadRequest("Member is not currently subscribed.");
            }

            // Check if a termination request already exists for the member and contract
            var existingRequest = await _context.TerminationRequests
                .FirstOrDefaultAsync(tr => tr.Contract_ID == model.Contract_ID && tr.Member_ID == model.Member_ID);

            if (existingRequest != null)
            {
                return BadRequest("A termination request already exists for this member and contract.");
            }

            // Map the predefined reasons based on the termination reason type
            var predefinedReasons = new Dictionary<RequestedTerminationReasonType, string>
            {
                { RequestedTerminationReasonType.FeesTooExpensive, "Fees are too expensive" },
                { RequestedTerminationReasonType.DifferentChallengeAtAnotherGym, "I want a different challenge at a different gym" },
                { RequestedTerminationReasonType.UnhappyAtAVSFitness, "I'm unhappy at AVS Fitness" },
                { RequestedTerminationReasonType.Relocating, "Relocating to a different city or country" },
                { RequestedTerminationReasonType.HealthIssues, "Health issues or injury preventing regular attendance" },
                { RequestedTerminationReasonType.PersonalOrFinancialCircumstances, "Personal or financial circumstances" },
                { RequestedTerminationReasonType.LackOfTime, "Lack of time due to increased work or family commitments" },
                { RequestedTerminationReasonType.UnsatisfactoryCustomerService, "Unsatisfactory customer service or support experience" }
            };

            // Create a new termination request
            var terminationRequest = new TerminationRequest
            {
                Contract_ID = model.Contract_ID,
                Member_ID = model.Member_ID,
                Requested_Termination_Reason_Type = model.Requested_Termination_Reason_Type,
                CustomReason = model.Requested_Termination_Reason_Type == RequestedTerminationReasonType.Custom
                    ? model.CustomReason
                    : predefinedReasons.GetValueOrDefault(model.Requested_Termination_Reason_Type)
            };

            _context.TerminationRequests.Add(terminationRequest);
            await _context.SaveChangesAsync();

            // Prepare for termination confirmation email
            var emailModel = new TerminationAcknowledgmentViewModel
            {
                MemberName = contract.Member.User.Name,
                MemberSurname = contract.Member.User.Surname,
                MemberEmail = contract.Member.User.Email // Ensure Email is part of the User model
            };

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Call email sending method
            var emailResponse = await SendTerminationRequestEmail(emailModel);
            if (emailResponse is OkObjectResult)
            {
                var audit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"Termination request created and email sent: Contract ID '{model.Contract_ID}', Member ID '{model.Member_ID}', Reason '{terminationRequest.CustomReason}'.",
                    Changed_By = changedBy,
                    Table_Name = nameof(Member),
                    Timestamp = DateTime.UtcNow
                };
                _context.Audit_Trails.Add(audit);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    contract.Contract_ID,
                    contract.Member_ID,
                    model.Requested_Termination_Reason_Type,
                    terminationRequest.CustomReason
                });
            }
            else
            {
                return StatusCode((int)((ObjectResult)emailResponse).StatusCode, "Failed to send termination email.");
            }
        }





        [HttpPost]
        [Route("SendRequestTerminationEmail")]
        public async Task<IActionResult> SendTerminationRequestEmail([FromBody] TerminationAcknowledgmentViewModel model)
        {
            var client = new SendGridClient(_configuration["SendGrid:ApiKey"]);
            var from = new EmailAddress(_configuration["SendGrid:SenderEmail"], _configuration["SendGrid:SenderName"]);
            var subject = "Contract Termination Request Received";
            var to = new EmailAddress(model.MemberEmail, $"{model.MemberName} {model.MemberSurname}");

            var htmlContent = $"<p>Dear {model.MemberName} {model.MemberSurname},</p>" +
                              $"<p>Your request to cancel your current contract has been sent.</p>" +
                              $"<p>Kind Regards,<br>Your AVS Fitness Team</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, htmlContent, htmlContent); // Remove plainTextContent

            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                return Ok("Termination email sent.");
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"Error sending termination email: {responseBody}");
            }
        }

        public class TerminationAcknowledgmentViewModel
        {
            public string MemberName { get; set; }
            public string MemberSurname { get; set; }
            public string MemberEmail { get; set; }
        }






        [HttpGet("GetAllContractHistory")]
        public IActionResult GetAllContractHistory()
        {
            var contractHistories = _context.Contract_History.ToList();

            if (contractHistories == null || !contractHistories.Any())
            {
                return NotFound("No contract history records found.");
            }

            return Ok(contractHistories);
        }




        // New endpoint to get the number of members subscribed per contract type
        [HttpGet]
        [Route("GetMembersCountPerContractType")]
        public async Task<IActionResult> GetMembersCountPerContractType()
        {
            var contractTypeCounts = await _context.Contracts
                .Where(c => c.Approval_Status)  // Only count approved contracts
                .GroupBy(c => c.Contract_Type_ID)
                .Select(g => new
                {
                    ContractTypeId = g.Key,
                    MemberCount = g.Count()
                })
                .ToListAsync();

            return Ok(contractTypeCounts);
        }

        [HttpGet]
        [Route("DownloadConsentForm/{memberId}")]
        public async Task<IActionResult> DownloadConsentForm(int memberId)
        {
            var userId = User.FindFirstValue("userId");
            var userTypeIdClaim = User.FindFirstValue("User_Type_ID");

            if (userId == null)
            {
                return Unauthorized("User not logged in.");
            }

            if (string.IsNullOrEmpty(userTypeIdClaim))
            {
                return Unauthorized("User Type ID is missing in the token.");
            }

            var userTypeId = int.Parse(userTypeIdClaim);

            if (userTypeId != 1 && userTypeId != 2)
            {
                return Forbid("You are not authorized to access this consent form.");
            }

            var consentForm = await _context.ConsentForms
                                            .FirstOrDefaultAsync(c => c.Member_ID == memberId);

            if (consentForm == null || string.IsNullOrEmpty(consentForm.FileName))
            {
                return NotFound("Consent form not found or file name is missing.");
            }

            var fileName = consentForm.FileName;
            var rootPath = Directory.GetCurrentDirectory();
            var absoluteFilePath = Path.Combine(rootPath, "ConsentForms", fileName); // Ensure this directory is correct

            if (!System.IO.File.Exists(absoluteFilePath))
            {
                return NotFound("Consent form file not found on server.");
            }

            var mimeType = "application/pdf"; // Adjust the MIME type if necessary

            var fileBytes = await System.IO.File.ReadAllBytesAsync(absoluteFilePath);

            return File(fileBytes, mimeType, fileName);
        }

        [HttpGet]
        [Route("DownloadSignedContractForAdmin/{contractId}")]
        public async Task<IActionResult> DownloadSignedContractForAdmin(int contractId)
        {
            var userId = User.FindFirstValue("userId");
            var userTypeIdClaim = User.FindFirstValue("User_Type_ID");

            if (userId == null)
            {
                return Unauthorized("User not logged in.");
            }

            if (string.IsNullOrEmpty(userTypeIdClaim))
            {
                return Unauthorized("User Type ID is missing in the token.");
            }

            var userTypeId = int.Parse(userTypeIdClaim);

            if (userTypeId != 1 && userTypeId != 2)
            {
                return Forbid("You are not authorized to access this contract.");
            }

            var contract = await _context.Contracts
                                          .Include(c => c.Member)
                                          .FirstOrDefaultAsync(c => c.Contract_ID == contractId);

            if (contract == null || !contract.Approval_Status || string.IsNullOrEmpty(contract.Filepath))
            {
                return NotFound("Signed contract not found or file path is missing.");
            }

            var filePath = contract.Filepath;
            var rootPath = Directory.GetCurrentDirectory();
            var absoluteFilePath = Path.Combine(rootPath, filePath);

            if (!System.IO.File.Exists(absoluteFilePath))
            {
                return NotFound("Signed contract file not found on server.");
            }

            var fileName = Path.GetFileName(absoluteFilePath);
            var mimeType = "application/pdf";

            var fileBytes = await System.IO.File.ReadAllBytesAsync(absoluteFilePath);

            return File(fileBytes, mimeType, fileName);
        }


        [HttpGet]
        [Route("DownloadMemberContract")]
        public async Task<IActionResult> DownloadMemberContract()
        {
            // Retrieve the user ID and user type ID from the authentication token
            var userId = User.FindFirstValue("userId");
            var userTypeIdClaim = User.FindFirstValue("User_Type_ID");

            // Check if userId is null (user is not logged in)
            if (userId == null)
            {
                return Unauthorized("User not logged in.");
            }

            // Check if userTypeIdClaim is missing
            if (string.IsNullOrEmpty(userTypeIdClaim))
            {
                return Unauthorized("User Type ID is missing in the token.");
            }

            // Parse userTypeIdClaim to integer
            var userTypeId = int.Parse(userTypeIdClaim);

            // Check if userTypeId is 3 (indicating the user is a member)
            if (userTypeId != 3)
            {
                return Forbid("You are not authorized to access this contract.");
            }

            // Retrieve the member associated with the current user
            var member = await _context.Members
                                       .Include(m => m.User)
                                       .FirstOrDefaultAsync(m => m.User_ID == int.Parse(userId));

            // Check if the member was found
            if (member == null)
            {
                return NotFound("Member not found.");
            }

            // Find the first contract associated with the member
            var contract = await _context.Contracts
                                          .Include(c => c.Member)
                                          .FirstOrDefaultAsync(c => c.Member_ID == member.Member_ID);

            // Check if the contract is valid
            if (contract == null || !contract.Approval_Status || string.IsNullOrEmpty(contract.Filepath))
            {
                return NotFound("Signed contract not found or file path is missing.");
            }

            // Generate the absolute file path to the signed contract
            var filePath = contract.Filepath;
            var rootPath = Directory.GetCurrentDirectory();
            var absoluteFilePath = Path.Combine(rootPath, filePath);

            // Check if the file exists
            if (!System.IO.File.Exists(absoluteFilePath))
            {
                return NotFound("Signed contract file not found on server.");
            }

            // Prepare the file response
            var fileName = Path.GetFileName(absoluteFilePath);
            var mimeType = "application/pdf";
            var fileBytes = await System.IO.File.ReadAllBytesAsync(absoluteFilePath);

            // Return the file as a download response
            return File(fileBytes, mimeType, fileName);
        }




        [HttpPost]
        [Route("SendApprovalEmail")]
        public async Task<IActionResult> SendApprovalEmail([FromBody] ApprovalEmailRequestViewModel model)
        {
            var client = new SendGridClient(_configuration["SendGrid:ApiKey"]);
            var from = new EmailAddress(_configuration["SendGrid:SenderEmail"], _configuration["SendGrid:SenderName"]);
            var subject = "Congratulations! Welcome to AVS Fitness Gym & Wellness Community";
            var to = new EmailAddress(model.Email);
            var loginUrl = "https://localhost:4200/login";

            var htmlContent = $"<strong>You are now fit to use the AV Motion application.</strong><br><br>" +
                              $"It's a pleasure to have you with us. We hope to see you consistently grow and reach your fitness goals.<br><br>" +
                              $"Your contract has been approved. Your first billing is expected on <strong>{model.SubscriptionDate.AddDays(30):yyyy/MM/dd}</strong>.<br><br>" +
                              $"You are now eligible to <a href='{loginUrl}'>login</a>.";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, htmlContent, htmlContent); // Remove plainTextContent

            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                return Ok("Approval email sent.");
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"Error sending approval email: {responseBody}");
            }
        }



        [HttpPost]
        [Route("SendTerminationEmail")]
        public async Task<IActionResult> SendTerminationEmail([FromBody] TerminationEmailRequestViewModel model)
        {
            var client = new SendGridClient(_configuration["SendGrid:ApiKey"]);
            var from = new EmailAddress(_configuration["SendGrid:SenderEmail"], _configuration["SendGrid:SenderName"]);
            var subject = "Notice of Contract Termination";
            var to = new EmailAddress(model.Email);

            var htmlContent = $"<strong>Notice of contract termination: {model.TerminationReasonType}</strong><br><br>" +
                              $"Your contract has been terminated effective as of <strong>{model.TerminationDate:yyyy/MM/dd}</strong>, due to: <strong>{model.TerminationReason}</strong>.<br><br>" +
                              $"Please note all terminations of contracts are final and cannot be appealed. No further communication will be responded to on this matter.<br><br>" +
                              $"For more information about AVS Fitness rules and regulations, please see our <a href='[Community Guidelines Link]'>community guidelines</a>.";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, htmlContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            // Log the response from SendGrid
            var responseBody = await response.Body.ReadAsStringAsync();
            Console.WriteLine($"SendGrid Response: {responseBody}");

            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                return Ok("Termination email sent.");
            }
            else
            {

                return StatusCode((int)response.StatusCode, $"Error sending termination email. Response: {responseBody}");
            }
        }


        public class ApprovalEmailRequestViewModel
        {
            public string Email { get; set; }
            public DateTime SubscriptionDate { get; set; }
        }

        public class TerminationEmailRequestViewModel
        {
            public string Email { get; set; }
            public string TerminationReasonType { get; set; }
            public string TerminationReason { get; set; }
            public DateTime TerminationDate { get; set; }
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
            var user = await _context.Users.FindAsync(parsedUserId);
            if (user == null)
            {
                return "Unknown";
            }

            // Check associated roles
            var owner = await _context.Owners.FirstOrDefaultAsync(o => o.User_ID == user.User_ID);
            if (owner != null)
            {
                return $"{owner.User.Name} {owner.User.Surname} (Owner)";
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.User_ID == user.User_ID);
            if (employee != null)
            {
                return $"{employee.User.Name} {employee.User.Surname} (Employee)";
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.User_ID == user.User_ID);
            if (member != null)
            {
                return $"{member.User.Name} {member.User.Surname} (Member)";
            }

            return "Unknown";
        }

    }
}




