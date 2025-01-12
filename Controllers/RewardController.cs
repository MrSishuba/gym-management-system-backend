using av_motion_api.Data;
using av_motion_api.Models;
using av_motion_api.Services;
using av_motion_api.ViewModels;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RewardController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;
        private readonly QualifyingMembersService _qualifyingMembersService;
        private readonly ILogger<RewardController> _logger;

        public RewardController(AppDbContext appDbContext, QualifyingMembersService qualifyingMembersService, ILogger<RewardController> logger)
        {
            _appDbContext = appDbContext;
            _qualifyingMembersService = qualifyingMembersService;
            _logger = logger;
        }

        [HttpGet]
        [Route("getAllRewardTypes")]
        public async Task<IActionResult> GetAllRewardTypes()
        {
            try
            {
                var rewardTypes = await _appDbContext.Reward_Types.ToListAsync();
                return Ok(rewardTypes);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpGet]
        [Route("getRewardTypeById/{id}")]
        public async Task<IActionResult> GetRewardTypeById(int id)
        {
            try
            {
                var rewardType = await _appDbContext.Reward_Types.FindAsync(id);

                if (rewardType == null)
                {
                    return NotFound();
                }

                return Ok(rewardType);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("createRewardType")]
        public async Task<IActionResult> CreateRewardType(RewardTypeViewModel rt)
        {
            try
            {
                var existingRewardType = await _appDbContext.Reward_Types
                    .FirstOrDefaultAsync(r => r.Reward_Type_Name.ToLower() == rt.Reward_Type_Name.ToLower()
                                   || r.Reward_Criteria.ToLower() == rt.Reward_Criteria.ToLower());


                if (existingRewardType != null)
                {
                    return Conflict(new { message = "Reward type name or criteria already exists." });
                }


                var rewardType = new Reward_Type
                {
                    Reward_Type_Name = rt.Reward_Type_Name,
                    Reward_Criteria = rt.Reward_Criteria

                };

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail
                var audit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"Reward Type Created: Name '{rewardType.Reward_Type_Name}', Criteria '{rewardType.Reward_Criteria}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Reward_Type),
                    Timestamp = DateTime.UtcNow
                };

                _appDbContext.Reward_Types.Add(rewardType);
                await _appDbContext.SaveChangesAsync();

                _appDbContext.Audit_Trails.Add(audit);
                await _appDbContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRewardTypeById), new { id = rewardType.Reward_Type_ID }, rewardType);
            }
            catch (Exception)
            {
                return BadRequest(new { message = "An error occurred while creating the reward type." });
            }
        }

        [HttpPut]
        [Route("updateRewardType/{id}")]
        public async Task<IActionResult> UpdateRewardType(int id, [FromBody] RewardTypeViewModel rt)
        {
            try
            {
                var rewardType = await _appDbContext.Reward_Types.FindAsync(id);

                if (rewardType == null)
                {
                    return NotFound();
                }

                var existingRewardType = await _appDbContext.Reward_Types
                    .FirstOrDefaultAsync(r => (r.Reward_Type_Name.ToLower() == rt.Reward_Type_Name.ToLower()
                                            || r.Reward_Criteria.ToLower() == rt.Reward_Criteria.ToLower())
                                            && r.Reward_Type_ID != id);

                if (existingRewardType != null)
                {
                    return Conflict(new { message = "Reward type name or criteria already exists." });
                }

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Prepare audit trail details
                var auditDetails = new List<string>();

                if (rewardType.Reward_Type_Name != rt.Reward_Type_Name)
                {
                    auditDetails.Add($"Name from '{rewardType.Reward_Type_Name}' to '{rt.Reward_Type_Name}'");
                }

                if (rewardType.Reward_Criteria != rt.Reward_Criteria)
                {
                    auditDetails.Add($"Criteria from '{rewardType.Reward_Criteria}' to '{rt.Reward_Criteria}'");
                }

                // Log audit trail if there are changes
                if (auditDetails.Any())
                {
                    var audit = new Audit_Trail
                    {
                        Transaction_Type = "UPDATE",
                        Critical_Data = $"Reward Type Updated: ID '{rewardType.Reward_Type_ID}', " + string.Join(", ", auditDetails),
                        Changed_By = changedBy,
                        Table_Name = nameof(Reward_Type),
                        Timestamp = DateTime.UtcNow
                    };

                    _appDbContext.Audit_Trails.Add(audit);
                }

                // Update the reward type
                rewardType.Reward_Type_Name = rt.Reward_Type_Name;
                rewardType.Reward_Criteria = rt.Reward_Criteria;

                _appDbContext.Entry(rewardType).State = EntityState.Modified;
                await _appDbContext.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception)
            {
                return BadRequest(new { message = "An error occurred while creating the reward type." });
            }
        }

        [HttpDelete]
        [Route("deleteRewardType/{id}")]
        public async Task<IActionResult> DeleteRewardType(int id)
        {
            try
            {
                var rewardType = await _appDbContext.Reward_Types.FindAsync(id);

                if (rewardType == null)
                {
                    return NotFound();
                }

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail
                var audit = new Audit_Trail
                {
                    Transaction_Type = "DELETE",
                    Critical_Data = $"Reward Type Deleted: ID '{rewardType.Reward_Type_ID}', Name '{rewardType.Reward_Type_Name}', Criteria '{rewardType.Reward_Criteria}'",
                    Changed_By = changedBy,
                    Table_Name = nameof(Reward_Type),
                    Timestamp = DateTime.UtcNow
                };

                _appDbContext.Reward_Types.Remove(rewardType);
                _appDbContext.Audit_Trails.Add(audit);
                await _appDbContext.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }



        [HttpGet]
        [Route("getRewardById/{id}")]
        public async Task<IActionResult> GetRewardById(int id)
        {
            try
            {
                var reward = await _appDbContext.Rewards.FindAsync(id);

                if (reward == null)
                {
                    return NotFound();
                }

                return Ok(reward);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpGet]
        [Route("getAllRewards")]
        public async Task<IActionResult> GetAllRewards()
        {
            try
            {
                var rewards = await _appDbContext.Rewards
                                         .Join(_appDbContext.Reward_Types,
                                               reward => reward.Reward_Type_ID,
                                               rewardType => rewardType.Reward_Type_ID,
                                               (reward, rewardType) => new RewardViewModel
                                               {
                                                   Reward_ID = reward.Reward_ID,
                                                   Reward_Issue_Date = reward.Reward_Issue_Date,
                                                   Reward_Type_Name = rewardType.Reward_Type_Name,
                                                   IsPosted = reward.IsPosted
                                               })
                                         .ToListAsync();
                return Ok(rewards);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpGet]
        [Route("UnredeemedRewards/{memberId}")]
        public async Task<IActionResult> UnredeemedRewards(int memberId)
        {
            try
            {
                var rewards = await _appDbContext.Reward_Members
                    .Where(r => r.Member_ID == memberId && !r.IsRedeemed)
                    .Select(rm => new
                    {
                        rm.Reward_ID,
                        rm.IsRedeemed,
                        RewardTypeName = rm.Reward.Reward_Type.Reward_Type_Name,
                        RewardCriteria = rm.Reward.Reward_Type.Reward_Criteria,
                        RewardIssueDate = rm.Reward.Reward_Issue_Date
                    })
                    .ToListAsync();

                return Ok(rewards);
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("redeemReward")]
        public async Task<IActionResult> RedeemReward([FromBody] RewardRedeemViewModel request)
        {
            try
            {
                // Fetch the reward by ID
                var reward = await _appDbContext.Reward_Members
                                                .FirstOrDefaultAsync(r => r.Reward_ID == request.RewardId && r.Member_ID == request.MemberId);

                if (reward == null)
                {
                    return NotFound("Reward not found or not eligible for the member.");
                }

                // Check if the reward is already redeemed
                if (reward.IsRedeemed)
                {
                    return BadRequest("Reward is already redeemed.");
                }

                // Get an unused discount code
                var discount = await _appDbContext.Discounts
                    .Where(d => DateTime.UtcNow <= d.End_Date)
                    .OrderBy(d => d.Discount_Date) // Get the oldest discount code
                    .FirstOrDefaultAsync();

                // Include discount code in the response
                var discountCode = discount?.Discount_Code ?? string.Empty;

                // Mark the reward as redeemed
                reward.IsRedeemed = true;

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail
                var audit = new Audit_Trail
                {
                    Table_Name = nameof(Reward_Member),
                    Transaction_Type = "UPDATE",
                    Critical_Data = $"Reward Redeemed: ID '{reward.Reward_ID}', Discount Code: '{discountCode}'",
                    Changed_By = changedBy,
                    Timestamp = DateTime.UtcNow,
                };

                _appDbContext.Entry(reward).State = EntityState.Modified;
                _appDbContext.Audit_Trails.Add(audit);
                await _appDbContext.SaveChangesAsync();

                return Ok(new { Status = "Success", Message = "Reward redeemed successfully!", DiscountCode = discountCode });
            }
            catch (Exception)
            {
                return BadRequest("An error occurred while redeeming the reward.");
            }
        }

        [HttpPost]
        [Route("setReward")]
        public async Task<IActionResult> SetReward(RewardSetViewModel r)
        {
            try
            {
                // Check if the reward already exists with the same issue date and reward type
                var existingReward = await _appDbContext.Rewards
                    .FirstOrDefaultAsync(reward => reward.Reward_Issue_Date == r.Reward_Issue_Date
                                                   && reward.Reward_Type_ID == r.Reward_Type_ID);

                if (existingReward != null)
                {
                    return Conflict(new { message = "This reward has already been set." });
                }

                var reward = new Reward
                {
                    Reward_Issue_Date = r.Reward_Issue_Date,
                    Reward_Type_ID = r.Reward_Type_ID,
                    IsPosted = r.IsPosted
                };

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail
                var audit = new Audit_Trail
                {
                    Transaction_Type = "INSERT",
                    Critical_Data = $"Reward type ID '{reward.Reward_Type_ID}' has been set",
                    Changed_By = changedBy,
                    Table_Name = nameof(Reward),
                    Timestamp = DateTime.UtcNow,
                };

                _appDbContext.Rewards.Add(reward);
                await _appDbContext.SaveChangesAsync();

                _appDbContext.Audit_Trails.Add(audit);
                await _appDbContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRewardById), new { id = reward.Reward_ID }, reward);
            }
            catch (DbUpdateException ex)
            {
                // Log the error details for debugging
                _logger.LogError(ex, "Database update error while setting reward.");
                return StatusCode(500, "A database error occurred while setting the reward.");
            }
            catch (Exception ex)
            {
                // Log the error details for debugging
                _logger.LogError(ex, "An unexpected error occurred while setting the reward.");
                return StatusCode(500, "An unexpected error occurred while setting the reward.");
            }
        }

        [HttpPost]
        [Route("postReward")]
        public async Task<IActionResult> PostReward([FromBody] RewardPostViewModel request)
        {
                _logger.LogInformation("Received request to post reward with ID: {RewardId}", request.RewardId);

                var reward = await _appDbContext.Rewards
                                                .FirstOrDefaultAsync(r => r.Reward_ID == request.RewardId);

                if (reward == null)
                {
                    _logger.LogWarning("Reward with ID: {RewardId} not found.", request.RewardId);
                    return NotFound("Reward not found.");
                }

                if (reward.IsPosted)
                {
                    _logger.LogWarning("Reward with ID: {RewardId} is already posted.", request.RewardId);
                    return BadRequest("Reward is already posted.");
                }

                reward.IsPosted = true;

                // Retrieve the changed by information
                var changedBy = await GetChangedByAsync();

                // Log audit trail
                var audit = new Audit_Trail
                {
                    Transaction_Type = "UPDATE",
                    Critical_Data = $"Reward type ID '{reward.Reward_Type_ID}' has been posted",
                    Changed_By = changedBy,
                    Table_Name = nameof(Reward),
                    Timestamp = DateTime.UtcNow
                };


                _appDbContext.Entry(reward).State = EntityState.Modified;
                _appDbContext.Audit_Trails.Add(audit);
                await _appDbContext.SaveChangesAsync();

                if (request.TriggerCheck)
                {
                    _logger.LogInformation("Triggering update for qualifying members for reward ID: {RewardId}", reward.Reward_ID);
                    await UpdateQualifyingMembersForReward(reward.Reward_ID);
                }

                _logger.LogInformation("Successfully posted reward with ID: {RewardId}", reward.Reward_ID);
                return Ok(new { Status = "Success", Message = "Reward posted successfully!" });
        }




        private async Task UpdateQualifyingMembersForReward(int rewardId)
        {
                _logger.LogInformation("Updating qualifying members for reward ID: {RewardId}", rewardId);

                var reward = await _appDbContext.Rewards.FindAsync(rewardId);
                if (reward == null)
                {
                    _logger.LogWarning("Reward with ID: {RewardId} not found during member update.", rewardId);
                    return;
                }

                var rewardType = await _appDbContext.Reward_Types.FindAsync(reward.Reward_Type_ID);
                if (rewardType == null)
                {
                    _logger.LogWarning("Reward type with ID: {RewardTypeId} not found.", reward.Reward_Type_ID);
                    return;
                }

                var qualifyingMembers = await _qualifyingMembersService.GetQualifyingMembersAsync(rewardType.Reward_Criteria);

                foreach (var member in qualifyingMembers)
                {
                    var existingRewardMember = await _appDbContext.Reward_Members
                        .FirstOrDefaultAsync(rm => rm.Member_ID == member.Member_ID && rm.Reward_ID == reward.Reward_ID);

                    if (existingRewardMember == null)
                    {
                        var rewardMember = new Reward_Member
                        {
                            Member_ID = member.Member_ID,
                            Reward_ID = reward.Reward_ID,
                            IsRedeemed = false
                        };
                        _appDbContext.Reward_Members.Add(rewardMember);
                    }
                }

                await _appDbContext.SaveChangesAsync();
                _logger.LogInformation("Successfully updated qualifying members for reward ID: {RewardId}", rewardId);
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
