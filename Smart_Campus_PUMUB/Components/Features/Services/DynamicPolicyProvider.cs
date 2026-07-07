using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Features.Services;

public class DynamicPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public DynamicPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // 💡 Check if policy exists in memory first (like roles or default rules)
        var policy = await base.GetPolicyAsync(policyName);
        if (policy != null) return policy;

        // 💡 If not found, dynamically generate a policy requiring the Permission claim with this name
        return new AuthorizationPolicyBuilder()
            .RequireClaim("Permission", policyName)
            .Build();
    }
}
