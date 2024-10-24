using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace clioAgent;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions {
	
	#region Properties: Public

	public string? ApiKeyAdmin { get; set; }

	public string? ApiKeyRead { get; set; }

	#endregion

}

public class ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder) {

	#region Properties: Public

	public static string SchemeName => "ApiKey";

	#endregion

	#region Methods: Protected
	protected override Task<AuthenticateResult> HandleAuthenticateAsync(){
		
		Claim[] claims;
		if (!Request.Headers.TryGetValue("X-API-KEY", out StringValues apiKeyHeaderValues)) {
			return Task.FromResult(AuthenticateResult.Fail("Missing X-API-KEY header"));
		}
		string? providedApiKey = apiKeyHeaderValues.FirstOrDefault();
		string[] allowedApiKeys = new string[2];
		if (Options is {ApiKeyAdmin: not null, ApiKeyRead: not null}) {
			allowedApiKeys = [Options.ApiKeyAdmin, Options.ApiKeyRead];
		}
		if (!allowedApiKeys.Contains(providedApiKey)) {
			return Task.FromResult(AuthenticateResult.Fail("Invalid X-API-KEY header value"));
		}
		
		if(providedApiKey == Options.ApiKeyAdmin) {
			claims = [
				new Claim(ClaimTypes.Role, Roles.Admin), 
				new Claim(ClaimTypes.Role, Roles.Read)
			];
			return Task.FromResult(AuthenticateResult.Success(GetTicketFromClaims(claims)));
		} 
		if(providedApiKey == Options.ApiKeyRead){
			claims = [
				new Claim(ClaimTypes.Role, Roles.Read)
			];
			return Task.FromResult(AuthenticateResult.Success(GetTicketFromClaims(claims)));
		}
		
		claims = [
			new Claim(ClaimTypes.Role, Roles.UnAuthenticate), 
		];
		return Task.FromResult(AuthenticateResult.Success(GetTicketFromClaims(claims)));
	}
	private static AuthenticationTicket GetTicketFromClaims(Claim[] claims){
		ClaimsIdentity identity = new (claims, SchemeName);
		ClaimsPrincipal principal = new (identity);
		AuthenticationTicket ticket = new (principal, SchemeName);
		return ticket;
	}
	

	#endregion

}

public static class  Roles {

	#region Properties: Public

	public const string Admin = "Admin-User";

	public const string Read = "Read-User";
	public const string UnAuthenticate = "UnAuthenticate";

	#endregion
}
