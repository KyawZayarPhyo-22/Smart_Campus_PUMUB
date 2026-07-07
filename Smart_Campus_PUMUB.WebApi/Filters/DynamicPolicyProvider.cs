using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Smart_Campus_PUMUB.WebApi.Filters;

public class DynamicPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public DynamicPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check if policy already exists
        var policy = await base.GetPolicyAsync(policyName);
        if (policy != null) return policy;

        // Dynamically create policy requiring the "Permission" claim
        return new AuthorizationPolicyBuilder()
            .RequireClaim("Permission", policyName)
            .Build();
    }
}
