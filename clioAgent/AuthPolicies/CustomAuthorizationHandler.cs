using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace clioAgent.AuthPolicies;

public class AuthorizationRequirement(string claimType, string claimValue) : IAuthorizationRequirement {
	public string ClaimType { get; } = claimType;
	public string ClaimValue { get; } = claimValue;

}


public class CustomAuthorizationHandler : AuthorizationHandler<AuthorizationRequirement> {
	protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizationRequirement requirement){
		ClaimsPrincipal user = context.User;
		if (user.HasClaim(requirement.ClaimType, requirement.ClaimValue)) {
			context.Succeed(requirement);
		}
		else {
			string failureReason = $"User does not have {requirement.ClaimType} claim with value {requirement.ClaimValue}.";
			var reason = new AuthorizationFailureReason(this, failureReason);
			context.Fail(reason);
		}

		return Task.CompletedTask;
	}
}



