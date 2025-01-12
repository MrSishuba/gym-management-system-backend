using av_motion_api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace av_motion_api.Factory
{
    public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, Role>
    {
        public AppUserClaimsPrincipalFactory(UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
        {
        }
    }
}
