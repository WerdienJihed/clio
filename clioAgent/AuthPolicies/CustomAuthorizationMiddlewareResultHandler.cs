using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace clioAgent.AuthPolicies;

public class CustomAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
	private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

	public async Task HandleAsync(
		RequestDelegate next,
		HttpContext context,
		AuthorizationPolicy policy,
		PolicyAuthorizationResult authorizeResult){
		
		if (authorizeResult.Succeeded) {
			// Authorization was successful, proceed to the next middleware
			await next(context);
			return;
		}
		if (authorizeResult.Forbidden) {
			// User is authenticated but does not have sufficient permissions
			context.Response.StatusCode = StatusCodes.Status403Forbidden;
			context.Response.ContentType = "application/json";
			List<string>? msg = authorizeResult.AuthorizationFailure?.FailureReasons?.Select(r => r.Message).ToList();
			CustomProblem fProblem = CustomProblem.CreateCustomProblem(StatusCodes.Status403Forbidden, msg ?? ["Unauthorized"]);
			await context.Response.WriteAsync(fProblem.ToString());
			return;
		}

		if (authorizeResult.Challenged) {
			// User is not authenticated
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			context.Response.ContentType = "application/json";
			
			// This is weird, why do I need to call AuthenticateAsync() here?
			var authResult = await context.AuthenticateAsync();
			var problem = new CustomProblem(StatusCodes.Status401Unauthorized, authResult.Failure?.Message ?? "UnAuthenticated");
			await context.Response.WriteAsync(problem.ToString());
			return;
		}
		// Fallback to the default handler for any other case
		await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
	}
}