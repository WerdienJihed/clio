using Allure.Net.Commons;
using Allure.NUnit.Attributes;
using clio.E2E.Common;
using FluentAssertions;

namespace clio.E2E.CommonAsserts;

public class HealthCheckAsserts {

	private readonly IServiceUrlBuilder _urlBuilder;
	private readonly IEnvironmentSettings _settings;

	public HealthCheckAsserts(IServiceUrlBuilder urlBuilder, IEnvironmentSettings settings) {
		_urlBuilder = urlBuilder;
		_settings = settings;
		
	}
	
	[AllureStep("Checks result for Success - WebAppLoader")]
	public void HealthcheckCommand_Checks_WebAppLoader_Success(string result) {
		IEnvironmentSettings modifiedSettings = new EnvironmentSettings(_settings.Password, _settings.Login, 
			string.Empty, _settings.ClientSecret, string.Empty, _settings.Url, true);
		string expectedRouteForWebAppLoader = _urlBuilder.Build(ServiceUrlBuilder.KnownRoute.HealthCheck, modifiedSettings);
		string expectedResultForWebAppLoader = $"""
												[INF] - Checking WebAppLoader {expectedRouteForWebAppLoader} ...
												[INF] - 	WebAppLoader - OK
												""";
		result.Should().Contain(expectedResultForWebAppLoader);
	}
	
	[AllureStep("Checks result for Error - WebAppLoader")]
	public void HealthcheckCommand_Checks_WebAppLoader_Error(string url, string result) {
		IEnvironmentSettings modifiedSettings = new EnvironmentSettings(_settings.Password, _settings.Login, 
			string.Empty, _settings.ClientSecret, string.Empty, url, true);
		string expectedRouteForWebAppLoader = _urlBuilder.Build(ServiceUrlBuilder.KnownRoute.HealthCheck, modifiedSettings);
		string expectedResultForWebAppLoader = $"""
												[INF] - Checking WebAppLoader {expectedRouteForWebAppLoader} ...
												[ERR] - 	Error: The remote server returned an error: (404) Not Found.
												""";
		result.Should().Contain(expectedResultForWebAppLoader);
	}

	[AllureStep("Checks result for Success - WebHost")]
	public void HealthcheckCommand_Checks_WebHost_Success(string result) {
		IEnvironmentSettings modifiedSettings = new EnvironmentSettings(_settings.Password, _settings.Login, 
			string.Empty, string.Empty, string.Empty, _settings.Url, true);
		string expectedRouteForWebHost = _urlBuilder.Build(ServiceUrlBuilder.KnownRoute.HealthCheck, modifiedSettings);
		string expectedResultForWebHost =$"""
										[INF] - Checking WebHost {expectedRouteForWebHost} ...
										[INF] - 	WebHost - OK
										""";
		result.Should().Contain(expectedResultForWebHost);
	}
	[AllureStep("Checks result for Error - WebHost")]
	public void HealthcheckCommand_Checks_WebHost_Error(string url, string result) {
		IEnvironmentSettings modifiedSettings = new EnvironmentSettings(_settings.Password, _settings.Login, 
			string.Empty, string.Empty, string.Empty, url, true);
		string expectedRouteForWebHost = _urlBuilder.Build(ServiceUrlBuilder.KnownRoute.HealthCheck, modifiedSettings);
		string expectedResultForWebHost =$"""
										[INF] - Checking WebHost {expectedRouteForWebHost} ...
										[ERR] - 	Error: The remote server returned an error: (404) Not Found.
										""";
		result.Should().Contain(expectedResultForWebHost);
	}
}
