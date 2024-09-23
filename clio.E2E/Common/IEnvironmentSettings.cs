using System.Text.Json.Serialization;

namespace clio.E2E.Common;

public interface IEnvironmentSettings
{

	#region Properties: Public

	string Password { get; }
	string Login { get; }
	bool IsNetCore { get; }
	public string ClientId { get; }
	public string ClientSecret { get; }
	string Url { get; }
	string AuthAppUri { get; }
	
	#endregion

}

internal record EnvironmentSettings(
	[property: JsonPropertyName("CREATIO_PASSWORD")] string Password,
	[property: JsonPropertyName("CREATIO_LOGIN")] string Login,
	[property: JsonPropertyName("CREATIO_CLIENT_ID")] string ClientId,
	[property: JsonPropertyName("CREATIO_CLIENT_SECRET")] string ClientSecret,
	[property: JsonPropertyName("CREATIO_AUTH_APP_URI")] string AuthAppUri,
	[property: JsonPropertyName("CREATIO_URL")] string Url,
	[property: JsonPropertyName("CREATIO_IS_NETCORE")] bool IsNetCore
	) : IEnvironmentSettings;