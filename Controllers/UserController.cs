using av_motion_api.Data;
using av_motion_api.Models;
using av_motion_api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using SendGrid.Helpers.Mail;
using SendGrid;
using System.Web;
using System.Security.Cryptography;
using System.Text;
using System;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [EnableCors("AllowAll")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserClaimsPrincipalFactory<User> _claimsPrincipalFactory;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _appDbContext;
        private readonly RoleManager<Role> _roleManager;
        private readonly ILogger<UserController> _logger;

        public UserController(AppDbContext context, UserManager<User> userManager, IUserClaimsPrincipalFactory<User> claimsPrincipalFactory, IConfiguration configuration, RoleManager<Role> roleManager, ILogger<UserController> logger)
        {
            _appDbContext = context;
            _userManager = userManager;
            _claimsPrincipalFactory = claimsPrincipalFactory;
            _configuration = configuration;
            _roleManager = roleManager;
            _logger = logger;
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [Route("RegisterMember")]
        public async Task<IActionResult> RegisterMember([FromForm] MemberViewModel mvm)
        {
            var formCollection = await Request.ReadFormAsync();
            var photo = formCollection.Files.FirstOrDefault();

            // Call RegisterUser and check if there's an error response
            var response = await RegisterUser(mvm, photo);
            if (response is ObjectResult objectResult && objectResult.StatusCode != StatusCodes.Status200OK)
            {
                return response; // Return the error response if any
            }

            // Get the newly registered user's ID
            var newUser = await _userManager.FindByEmailAsync(mvm.Email);
            if (newUser == null)
            {
                return BadRequest(new { Status = "Error", Message = "User registration failed." });
            }

            // Proceed to add the new member to the database
            var newMember = new Member
            {
                User_ID = newUser.Id,
                Membership_Status_ID = mvm.Membership_Status_ID
               
               
            };

            _appDbContext.Members.Add(newMember);
            await _appDbContext.SaveChangesAsync();

            // Retrieve the changed by information
            var changedBy = await GetChangedByAsync();

            // Log audit trail for registering a member
            var memberAudit = new Audit_Trail
            {
                Transaction_Type = "INSERT",
                Critical_Data = $"Member registered: User ID '{newMember.User_ID}', Member ID '{newMember.Member_ID}', Fullname '{newMember.User.Name} {newMember.User.Surname}', Email '{newMember.User.Email}', User Type ID '{newMember.User.User_Type_ID}'",
                Changed_By = changedBy,
                Table_Name = nameof(Member),
                Timestamp = DateTime.UtcNow
            };

            _appDbContext.Audit_Trails.Add(memberAudit);
            await _appDbContext.SaveChangesAsync();  // Ensure the audit trail is saved

            return Ok(new { Status = "Success", Message = "Member profile has been created successfully!" });
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [Route("RegisterEmployee")]
        public async Task<IActionResult> RegisterEmployee([FromForm] EmployeeViewModel evm)
        {
            var formCollection = await Request.ReadFormAsync();
            var photo = formCollection.Files.FirstOrDefault();

            // Call RegisterUser method to create the user
            var userCreationResponse = await RegisterUser(evm, photo);
            if (userCreationResponse is OkObjectResult)
            {
                // Retrieve the created user from the database
                var createdUser = await _userManager.FindByEmailAsync(evm.Email);
                if (createdUser == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving created user.");
                }

                // Add the user to the Employee table
                var newEmployee = new Employee
                {
                    User_ID = createdUser.User_ID, // Use the User_ID of the created user
                    Employment_Date = evm.Employment_Date,
                    Hours_Worked = evm.Hours_Worked,
                    Employee_Type_ID = evm.Employee_Type_ID,
                    Shift_ID = evm.Shift_ID
                };

                try
                {
                    _appDbContext.Employees.Add(newEmployee);
                    await _appDbContext.SaveChangesAsync();

                    // Retrieve the changed by information
                    var changedBy = await GetChangedByAsync();

                    // Log audit trail for registering an employee
                    var employeeAudit = new Audit_Trail
                    {
                        Transaction_Type = "INSERT",
                        Critical_Data = $"Employee registered: User ID '{newEmployee.User_ID}', Employee ID '{newEmployee.Employee_ID}', Fullname '{newEmployee.User.Name} {newEmployee.User.Surname}', Email '{newEmployee.User.Email}', User Type ID ' {newEmployee.User.User_Type_ID}'",
                        Changed_By = changedBy,
                        Table_Name = nameof(Employee),
                        Timestamp = DateTime.UtcNow
                    };

                    _appDbContext.Audit_Trails.Add(employeeAudit);
                    await _appDbContext.SaveChangesAsync();  // Ensure the audit trail is saved
                }
                catch (Exception ex)
                {
                    // Log the detailed error message
                    var innerExceptionMessage = ex.InnerException?.Message ?? ex.Message;
                    return StatusCode(StatusCodes.Status500InternalServerError, $"Error adding employee: {innerExceptionMessage}");
                }

                return Ok(new { Status = "Success", Message = "Employee profile has been created successfully!" });
            }

            return userCreationResponse;
        }



        [HttpPost]
        [DisableRequestSizeLimit]
        [Route("RegisterOwner")]
        public async Task<IActionResult> RegisterOwner([FromForm] OwnerViewModel ovm)
        {
            var formCollection = await Request.ReadFormAsync();
            var photo = formCollection.Files.FirstOrDefault();

            // Call RegisterUser method to create the user
            var userCreationResponse = await RegisterUser(ovm, photo);
            if (userCreationResponse is OkObjectResult)
            {
                // Retrieve the created user from the database
                var createdUser = await _userManager.FindByEmailAsync(ovm.Email);
                if (createdUser == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving created user.");
                }

                // Add the user to the Owner table
                var newOwner = new Owner
                {
                    User_ID = createdUser.User_ID // Use the User_ID of the created user
                };

                try
                {
                    _appDbContext.Owners.Add(newOwner);
                    await _appDbContext.SaveChangesAsync();

                    // Retrieve the changed by information
                    var changedBy = await GetChangedByAsync();

                    // Log audit trail for registering an owner
                    var ownerAudit = new Audit_Trail
                    {
                        Transaction_Type = "INSERT",
                        Critical_Data = $"Owner registered: User ID '{newOwner.User_ID}', Owner ID '{newOwner.Owner_ID}', Fullname '{newOwner.User.Name} {newOwner.User.Surname}', Email '{newOwner.User.Email}', User Type ID ' {newOwner.User.User_Type_ID}'",
                        Changed_By = changedBy,
                        Table_Name = nameof(Owner),
                        Timestamp = DateTime.UtcNow
                    };

                    _appDbContext.Audit_Trails.Add(ownerAudit);
                    await _appDbContext.SaveChangesAsync();  // Ensure the audit trail is saved
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, $"Error adding owner: {ex.Message}");
                }

                return Ok(new { Status = "Success", Message = "Owner profile has been created successfully!" });
            }

            return userCreationResponse;
        }

        private async Task<IActionResult> RegisterUser(UserViewModel uvm, IFormFile photo)
        {
            // Validate required fields
            if (string.IsNullOrEmpty(uvm.Name) || string.IsNullOrEmpty(uvm.Surname) || string.IsNullOrEmpty(uvm.Email) ||
                string.IsNullOrEmpty(uvm.Password) || string.IsNullOrEmpty(uvm.ID_Number) || string.IsNullOrEmpty(uvm.PhoneNumber))
            {
                return BadRequest("All required fields must be provided.");
            }

            // Validate phone number
            var phoneNumberPattern = @"^\d{10}$";
            bool isValidPhoneNumber = Regex.IsMatch(uvm.PhoneNumber, phoneNumberPattern);
            if (!isValidPhoneNumber)
            {
                return BadRequest("Enter a valid 10-digit phone number.");
            }

            // Validate South African ID number
            if (!IsValidSouthAfricanIDNumber(uvm.ID_Number, uvm.Date_of_Birth))
            {
                return BadRequest("Enter a valid South African ID number.");
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(uvm.Email);
            if (existingUser != null)
            {
                return Forbid("User already exists.");
            }

            // Validate photo
            if (photo == null || photo.Length == 0)
            {
                return BadRequest("Photo is required.");
            }

            using (var memoryStream = new MemoryStream())
            {
                await photo.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                string base64Image = Convert.ToBase64String(fileBytes);

                // Generate User_ID and Id
                var lastUser = _appDbContext.Users.OrderByDescending(u => u.User_ID).FirstOrDefault();
                var lastUserID = lastUser?.User_ID ?? 0;
                var lastId = lastUser?.Id ?? 0;

                var user = new User
                {
                    UserName = uvm.Email,
                    Email = uvm.Email,
                    Name = uvm.Name,
                    Surname = uvm.Surname,
                    PasswordHash = _userManager.PasswordHasher.HashPassword(null, uvm.Password),
                    User_Status_ID = uvm.User_Status_ID,
                    User_Type_ID = uvm.User_Type_ID,
                    PhoneNumber = uvm.PhoneNumber,
                    Date_of_Birth = uvm.Date_of_Birth,
                    ID_Number = uvm.ID_Number,
                    Physical_Address = uvm.Physical_Address,
                    Photo = base64Image, // Store the base64 image string
                    User_ID = lastUserID + 1, // Assign User_ID
                    Id = lastId + 1 // Assign Id
                };

                // Begin a database transaction
                using (var transaction = await _appDbContext.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Enable IDENTITY_INSERT for the AspNetUsers table
                        await _appDbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [AspNetUsers] ON");

                        // Create the user
                        IdentityResult result = await _userManager.CreateAsync(user);
                        if (!result.Succeeded)
                        {
                            await _appDbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [AspNetUsers] OFF");
                            await transaction.RollbackAsync();
                            return StatusCode(StatusCodes.Status500InternalServerError, result.Errors.FirstOrDefault()?.Description);
                        }

                        // Disable IDENTITY_INSERT after adding the user
                        await _appDbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [AspNetUsers] OFF");

                        // Assign role based on user type
                        string roleName = GetRoleNameByUserType(uvm.User_Type_ID);
                        if (!string.IsNullOrEmpty(roleName))
                        {
                            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
                            if (!roleResult.Succeeded)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest(new { Status = "Error", Errors = roleResult.Errors });
                            }
                        }

                        // Commit the transaction if everything is successful
                        await transaction.CommitAsync();

                        return Ok(new { Status = "Success", Message = "User registered successfully." });
                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction in case of an error
                        await transaction.RollbackAsync();
                        return StatusCode(StatusCodes.Status500InternalServerError, $"Error occurred: {ex.Message}");
                    }
                }
            }
        }
        private bool IsValidSouthAfricanIDNumber(string idNumber, DateTime dateOfBirth)
        {
            if (idNumber.Length != 13 || !long.TryParse(idNumber, out _))
            {
                return false;
            }

            string dateOfBirthPart = idNumber.Substring(0, 6);
            if (!DateTime.TryParseExact(dateOfBirthPart, "yyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
            {
                return false;
            }

            if (parsedDate.Year % 100 != dateOfBirth.Year % 100)
            {
                return false;
            }

            return true;
        }

        private string GetRoleNameByUserType(int userTypeId)
        {
            return userTypeId switch
            {
                1 => "Administrator",
                2 => "Employee",
                3 => "Member",
                _ => null,
            };
        }

        [HttpPost]
        [Route("Login")]
        public async Task<ActionResult> Login(LoginViewModel lv)
        {
            var user = await _userManager.FindByNameAsync(lv.Email);

            if (user != null && await _userManager.CheckPasswordAsync(user, lv.Password))
            {
                if (user.User_Status_ID == 2)
                {
                    return Unauthorized("Your account is deactivated.");
                }

                if (user.User_Type_ID == 3) // Member
                {
                    var member = await _appDbContext.Members.FirstOrDefaultAsync(m => m.User_ID == user.User_ID);
                    if (member != null)
                    {
                        if (member.Membership_Status_ID != 1)
                        {
                            return Unauthorized("Your membership status does not allow login.");
                        }

                        var contract = await _appDbContext.Contracts.FirstOrDefaultAsync(c => c.Member_ID == member.Member_ID);
                        if (contract != null && !contract.Approval_Status)
                        {
                            return Unauthorized("Your contract has not been approved yet.");
                        }
                    }
                }

                try
                {
                    var principal = await _claimsPrincipalFactory.CreateAsync(user);
                    return await GenerateJWTToken(user);
                }
                catch (Exception)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error. Please contact support.");
                }
            }
            else
            {
                return NotFound("Incorrect email or password, Please Try Again");
            }
        }

        [HttpGet]
        private async Task<ActionResult> GenerateJWTToken(User user)
        {
            var role = await _userManager.GetRolesAsync(user);
            IdentityOptions _identityOptions = new IdentityOptions();
            // Create JWT Token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                // Add user ID claim
                new Claim("userId", user.Id.ToString()),

                new Claim("User_Type_ID", user.User_Type_ID.ToString()),

            };

            if (role.Count() > 0)
            {
                claims.Add(new Claim(_identityOptions.ClaimsIdentity.RoleClaimType, role.FirstOrDefault()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Tokens:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
               issuer: _configuration["Tokens:Issuer"],
               audience: _configuration["Tokens:Audience"],
               claims: claims,
               signingCredentials: credentials,
               expires: DateTime.UtcNow.AddHours(3)
            );

            return Created("", new
            {

                token = new JwtSecurityTokenHandler().WriteToken(token),
                user = user.UserName,

                userTypeId = user.User_Type_ID,
                // Include user ID in the response
                userId = user.Id
            });
        }



        [HttpGet]
        [Route("getAllUsers")]
        public IActionResult GetAllUsers()
        {
            try
            {
                var users = _userManager.Users.ToList();


                if (users == null || users.Count == 0)
                {
                    return NotFound("No users found.");
                }

                return Ok(users);
            }
            catch (Exception)
            {

                return BadRequest("An Error Occured, Please Try Again");
            }
        }

        [HttpGet]
        [Route("getUserById/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var u = await _appDbContext.Users
                                .Include(u => u.User_Status)
                                .Include(u => u.User_Type)
                                .FirstOrDefaultAsync(u => u.Id == id);

                var user = new
                {
                    u.Id,
                    u.Name,
                    u.Surname,
                    u.Email,
                    u.Physical_Address,
                    u.PhoneNumber,
                    u.Date_of_Birth,
                    UserStatus = u.User_Status.User_Status_Description,
                    UserType = u.User_Type.User_Type_Name,
                    u.Photo,
                    u.ID_Number
                };

                return Ok(user);
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine(ex.Message);
                return BadRequest("An error occurred while fetching user details.");
            }
        }


        [HttpGet("GetMemberByUserId/{userId}")]
        public async Task<ActionResult<Member>> GetMemberByUserId(int userId)
        {
            var member = await _appDbContext.Members.FirstOrDefaultAsync(m => m.User_ID == userId);
            if (member == null)
            {
                return NotFound();
            }
            return Ok(member);
        }

        [HttpGet("employee")]
        public async Task<ActionResult<IEnumerable<UserEmployeeViewModel>>> GetEmployees()
        {
            var query = await (from e in _appDbContext.Employees
                               join u in _appDbContext.Users on e.User_ID equals u.User_ID
                               select new UserEmployeeViewModel
                               {
                                   employee_ID = e.Employee_ID,
                                   employee_name = u.Name
                               }).ToListAsync();

            return query;

        }

        [HttpGet("GetEmployeeFullNameAndId")]
        public async Task<IActionResult> GetEmployeeFullNameAndId(int userId)
        {
            // Check if the user exists and is of UserType 2 (Employee)
            var employee = await _appDbContext.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.User_ID == userId && e.User.User_Type_ID == 2);

            if (employee == null)
            {
                return NotFound("Employee not found or UserType is not Employee.");
            }

            // Create the response with full name and employee ID
            var result = new
            {
                FullName = $"{employee.User.Name} {employee.User.Surname}",
                Employee_ID = employee.Employee_ID
            };

            return Ok(result);
        }

        [HttpPut]
        [Route("editUser/{id}")]
        public async Task<IActionResult> EditUser(int id, [FromForm] UpdateUserViewModel uv)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());

                if (user == null)
                {
                    return NotFound("User not found.");
                }

                // Capture the original values for audit trail comparison
                var originalName = user.Name;
                var originalSurname = user.Surname;
                var originalUserName = user.UserName;
                var originalEmail = user.Email;
                var originalPhysicalAddress = user.Physical_Address;
                var originalPhoneNumber = user.PhoneNumber;
                var originalPhoto = user.Photo;

                // Read the form data to get the photo file
                var formCollection = await Request.ReadFormAsync();
                var photo = formCollection.Files.FirstOrDefault();

                user.Name = uv.Name;
                user.Surname = uv.Surname;
                user.UserName = uv.Email;
                user.Email = uv.Email;
                user.Physical_Address = uv.Physical_Address;
                user.PhoneNumber = uv.PhoneNumber;

                if (photo != null && photo.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await photo.CopyToAsync(memoryStream);
                        var fileBytes = memoryStream.ToArray();
                        string base64Image = Convert.ToBase64String(fileBytes);

                        user.Photo = base64Image; // Store the base64 string of the photo
                    }
                }

                // Update the user
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // Retrieve the changed by information
                    var changedBy = await GetChangedByAsync();

                    // Prepare audit trail if any changes were made
                    var auditChanges = new List<string>();

                    if (originalName != user.Name)
                    {
                        auditChanges.Add($"Name from '{originalName}' to '{user.Name}'");
                    }

                    if (originalSurname != user.Surname)
                    {
                        auditChanges.Add($"Surname from '{originalSurname}' to '{user.Surname}'");
                    }

                    if (originalUserName != user.UserName)
                    {
                        auditChanges.Add($"UserName from '{originalUserName}' to '{user.UserName}'");
                    }

                    if (originalEmail != user.Email)
                    {
                        auditChanges.Add($"Email from '{originalEmail}' to '{user.Email}'");
                    }

                    if (originalPhysicalAddress != user.Physical_Address)
                    {
                        auditChanges.Add($"Physical Address from '{originalPhysicalAddress}' to '{user.Physical_Address}'");
                    }

                    if (originalPhoneNumber != user.PhoneNumber)
                    {
                        auditChanges.Add($"Phone Number from '{originalPhoneNumber}' to '{user.PhoneNumber}'");
                    }

                    if (originalPhoto != user.Photo)
                    {
                        auditChanges.Add("Photo: updated");
                    }

                    if (auditChanges.Any())
                    {
                        var userAudit = new Audit_Trail
                        {
                            Transaction_Type = "UPDATE",
                            Critical_Data = $"User updated: ID '{user.Id}', {string.Join(", ", auditChanges)}",
                            Changed_By = changedBy,
                            Table_Name = nameof(User),
                            Timestamp = DateTime.UtcNow
                        };

                        _appDbContext.Audit_Trails.Add(userAudit);
                        await _appDbContext.SaveChangesAsync();  // Ensure the audit trail is saved
                    }

                    return Ok("User updated successfully.");
                }
                else
                {
                    return BadRequest(result.Errors);
                }
            }
            catch (Exception)
            {
                return BadRequest("An Error Occurred, Please Try Again");
            }
        }

        [HttpDelete]
        [Route("deleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());

                if (user == null)
                {
                    return NotFound("User not found.");
                }

                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    // Log audit trail for deleting user
                    var changedBy = await GetChangedByAsync();
                    var auditTrail = new Audit_Trail
                    {
                        Transaction_Type = "DELETE",
                        Critical_Data = $"User deleted: ID '{user.Id}', Fullname '{user.Name} {user.Surname} ', Email ' {user.Email}', User Type ID ' {user.User_Type_ID}'",
                        Changed_By = changedBy,
                        Table_Name = nameof(User),
                        Timestamp = DateTime.UtcNow
                    };

                    _appDbContext.Audit_Trails.Add(auditTrail);
                    await _appDbContext.SaveChangesAsync();

                    return Ok();
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error. Please contact support.");
                }
            }
            catch (Exception)
            {

                return BadRequest("An Error Occured, Please Try Again");
            }
        }

        [HttpPut]
        [Route("DeactivateUser/{id}")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (user.User_Type_ID != 2 && user.User_Type_ID != 3)
            {
                return BadRequest("User cannot be deactivated.");
            }

            user.User_Status_ID = 2; // Deactivated
            user.DeactivatedAt = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // Log audit trail for deactivating user
                var changedBy = await GetChangedByAsync();
                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "UPDATE",
                    Critical_Data = $"User deactivated: ID '{user.Id}', Fullname '{user.Name} {user.Surname} ', Email ' {user.Email}', User Type ID '{user.User_Type_ID}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(User),
                    Timestamp = DateTime.UtcNow
                };

                _appDbContext.Audit_Trails.Add(auditTrail);
                await _appDbContext.SaveChangesAsync();

                var response = new { message = "User deactivated successfully." };
                return Ok(response);
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpPut]
        [Route("ReactivateUser/{id}")]
        public async Task<IActionResult> ReactivateUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (user.User_Type_ID != 2 && user.User_Type_ID != 3)
            {
                return BadRequest("User cannot be deactivated.");
            }

            user.User_Status_ID = 1; // Active
                                     //user.DeactivatedAt = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // Log audit trail for reactivating user
                var changedBy = await GetChangedByAsync();
                var auditTrail = new Audit_Trail
                {
                    Transaction_Type = "UPDATE",
                    Critical_Data = $"User reactivated: ID '{user.Id}', Fullname '{user.Name} {user.Surname} ', Email ' {user.Email}', User Type '{user.User_Type_ID}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(User),
                    Timestamp = DateTime.UtcNow
                };

                _appDbContext.Audit_Trails.Add(auditTrail);
                await _appDbContext.SaveChangesAsync();

                var response = new { message = "User deactivated successfully." };
                return Ok(response);
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }


        //Roles
        [HttpPost]
        [Route("CreateRole")]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                role = new Role
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpper(),
                    isEditable = true,
                };

                var result = await _roleManager.CreateAsync(role);
                if (!result.Succeeded) return BadRequest(result.Errors);
            }
            else
            {
                return Forbid("Role already exists.");
            }

            return Ok();
        }

        [HttpPost]
        [Route("AssignRole")]
        public async Task<IActionResult> AssignRole(string emailAddress, string roleName)
        {
            var user = await _userManager.FindByEmailAsync(emailAddress);
            if (user == null) return NotFound();

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (result.Succeeded) return Ok();

            return BadRequest(result.Errors);
        }

        //Password
        [HttpPost]
        [Route("ChangePassword")]
        public async Task<IActionResult> ChangePassword(int id, ChangePasswordViewModel cpvm)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var result = await _userManager.ChangePasswordAsync(user, cpvm.CurrentPassword, cpvm.NewPassword);
            if (result.Succeeded)
            {
                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                var passwordAudit = new Audit_Trail
                {
                    Transaction_Type = "UPDATE",
                    Critical_Data = $"User password changed for '{user.Name} {user.Surname}' ID '{user.Id}', Password: changed",
                    Changed_By = changedBy,
                    Table_Name = nameof(User),
                    Timestamp = DateTime.UtcNow
                };

                _appDbContext.Audit_Trails.Add(passwordAudit);
                await _appDbContext.SaveChangesAsync();  // Ensure the audit trail is saved

                return Ok("Password changed successfully.");
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpPost]
        [Route("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid data.");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Generate the reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);

            // Create the reset link
            var resetUrl = $"https://localhost:4200/reset-password?token={encodedToken}&email={model.Email}";

            // Send the reset link via email
            var client = new SendGridClient(_configuration["SendGrid:ApiKey"]);
            var from = new EmailAddress(_configuration["SendGrid:SenderEmail"], _configuration["SendGrid:SenderName"]);
            var subject = "Password Reset Request";
            var to = new EmailAddress(user.Email, user.UserName);
            var plainTextContent = $"You requested a password reset. Click the link to reset your password: {resetUrl}";
            var htmlContent = $"<strong>You requested a password reset.</strong><br><br>Click the link to reset your password: <a href=\"{resetUrl}\">Reset Password</a>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                var passwordResetAudit = new Audit_Trail
                {
                    Transaction_Type = "UPDATE",
                    Critical_Data = $"Password reset requested: Email {user.Email}, Password Reset: email sent",
                    Changed_By = changedBy,
                    Table_Name = nameof(User),
                    Timestamp = DateTime.UtcNow
                };

                _appDbContext.Audit_Trails.Add(passwordResetAudit);
                await _appDbContext.SaveChangesAsync();  // Ensure the audit trail is saved


                return Content("Password reset email sent.", "text/plain");
            }
            else
            {
                // Log the response for debugging
                var responseBody = await response.Body.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"Error sending email: {responseBody}");
            }
        }

        [HttpPost]
        [Route("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid data.");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var decodedToken = HttpUtility.UrlDecode(model.Token);

            Console.WriteLine($"ResetPassword - Email: {model.Email}");
            Console.WriteLine($"ResetPassword - Token: {decodedToken}");

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
            if (result.Succeeded)
            {
                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                var passwordResetAudit = new Audit_Trail
                {
                    Transaction_Type = "UPDATE",
                    Critical_Data = $"User password reset for '{user.Name} {user.Surname}' ID '{user.Id}', Password: reset",
                    Changed_By = changedBy,
                    Table_Name = nameof(User),
                    Timestamp = DateTime.UtcNow
                };

                _appDbContext.Audit_Trails.Add(passwordResetAudit);
                await _appDbContext.SaveChangesAsync();  // Ensure the audit trail is saved

                return Content("Password has been reset successfully.", "text/plain");
            }

            // Log errors for debugging
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest($"Error while resetting the password: {errors}");
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
            var user = await _appDbContext.Users.FindAsync(parsedUserId);
            if (user == null)
            {
                return "Unknown";
            }

            // Check associated roles
            var owner = await _appDbContext.Owners.FirstOrDefaultAsync(o => o.User_ID == user.User_ID);
            if (owner != null)
            {
                return $"{owner.User.Name} {owner.User.Surname} (Owner)";
            }

            var employee = await _appDbContext.Employees.FirstOrDefaultAsync(e => e.User_ID == user.User_ID);
            if (employee != null)
            {
                return $"{employee.User.Name} {employee.User.Surname} (Employee)";
            }

            var member = await _appDbContext.Members.FirstOrDefaultAsync(m => m.User_ID == user.User_ID);
            if (member != null)
            {
                return $"{member.User.Name} {member.User.Surname} (Member)";
            }

            return "Unknown";
        }
    }
}
