using Allure.Net.Commons;
using Allure.NUnit.Attributes;
using clio.E2E.CommonAsserts;
using clio.E2E.CommonSteps;
using FluentAssertions;

namespace clio.E2E.Commands;

[AllureSeverity(SeverityLevel.blocker)]
public class HealthcheckTestsFixture : BaseCommandTestFixture {


	#region Properties: Protected
	
	protected override Uri ReadmeUrl => new("https://github.com/Advance-Technologies-Foundation/clio?tab=readme-ov-file#healthcheck");
	protected override CommandName CommandUnderTest => CommandName.healthcheck;

	
	#endregion
	
	[AllureName("Check application health")]
	[AllureDescription("Checks application health")]
	[TestCase(true, true)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	public void HealthcheckCommand_Checks_Creatio(bool checkWebapp, bool checkWebHost) {
		//Arrange
		SetTestDescription();
		IClioSteps clioSteps = Environment.Resolve<IClioSteps>();
		
		//Act
		string result = (checkWebapp, checkWebHost) switch {
			(true, true) => clioSteps.ExecuteClioCommand(CommandUnderTest, "-a true -h true"),
			(true, false) => clioSteps.ExecuteClioCommand(CommandUnderTest, "-a true -h false"),
			(false, true) => clioSteps.ExecuteClioCommand(CommandUnderTest, "-a false -h true"),
			(false, false) => clioSteps.ExecuteClioCommand(CommandUnderTest, "-a false -h false"),
		};
		
		//Assert
		HealthCheckAsserts healthCheckAsserts = Environment.Resolve<HealthCheckAsserts>();
		Action assert = (checkWebapp, checkWebHost) switch {
			(true, true) => () => {
				healthCheckAsserts.HealthcheckCommand_Checks_WebAppLoader_Success(result);
				healthCheckAsserts.HealthcheckCommand_Checks_WebHost_Success(result);
			},
			(true, false) => () => healthCheckAsserts.HealthcheckCommand_Checks_WebAppLoader_Success(result),
			(false, true) => () => healthCheckAsserts.HealthcheckCommand_Checks_WebHost_Success(result),
			(false, false) => () => result.Should().BeEmpty(),
		};
		assert();
	}
	
	[AllureName("Check non existent application health")]
	[TestCase(true, true)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	public void HealthcheckCommand_Checks_Empty(bool checkWebapp, bool checkWebHost){
		//Arrange
		SetTestDescription();
		
		//Act
		IClioSteps clioSteps = Environment.Resolve<IClioSteps>();
		const string fakeUrl = "https://google.com";
		const string fakeEnvironmentArgs = $"-u {fakeUrl} -l S -p S -i true";
		string result = (checkWebapp, checkWebHost) switch {
			(true, true) => clioSteps.ExecuteClioCommand(CommandUnderTest, $"-a true -h true {fakeEnvironmentArgs}", false),
			(true, false) => clioSteps.ExecuteClioCommand(CommandUnderTest, $"-a true -h false {fakeEnvironmentArgs}",false),
			(false, true) => clioSteps.ExecuteClioCommand(CommandUnderTest, $"-a false -h true {fakeEnvironmentArgs}", false),
			(false, false) => clioSteps.ExecuteClioCommand(CommandUnderTest, $"-a false -h false {fakeEnvironmentArgs}", false),
		};
		
		//Assert
		HealthCheckAsserts healthCheckAsserts = Environment.Resolve<HealthCheckAsserts>();
		Action assert = (checkWebapp, checkWebHost) switch {
			(true, true) => () => {
				healthCheckAsserts.HealthcheckCommand_Checks_WebAppLoader_Error(fakeUrl, result);
				healthCheckAsserts.HealthcheckCommand_Checks_WebHost_Error(fakeUrl, result);
			},
			(true, false) => () => healthCheckAsserts.HealthcheckCommand_Checks_WebAppLoader_Error(fakeUrl, result),
			(false, true) => () => healthCheckAsserts.HealthcheckCommand_Checks_WebHost_Error(fakeUrl, result),
			(false, false) => () => result.Should().BeEmpty(),
		};
		assert();
	}
}