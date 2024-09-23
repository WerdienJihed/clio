using Allure.Net.Commons;
using Allure.NUnit.Attributes;
using clio.E2E.Common;
using clio.E2E.CommonSteps;
using FluentAssertions;

namespace clio.E2E.Commands;

[AllureSeverity(SeverityLevel.blocker)]
public class PingCommandTestsFixture : BaseCommandTestFixture {


	#region Properties: Protected
	
	protected override Uri ReadmeUrl => new("https://github.com/Advance-Technologies-Foundation/clio?tab=readme-ov-file#ping-application");
	protected override CommandName CommandUnderTest => CommandName.ping;

	
	#endregion

	[Test]
	[AllureName("Ping command returns INF messages when server is up and healthy")]
	[AllureDescription("Ping command returns INF messages when server is up and healthy")]
	public void PingCommand_ShouldReturnSuccess(){
		//Arrange
		SetTestDescription();
		
		//Act
		string result = Environment.Resolve<IClioSteps>().ExecuteClioCommandWithoutArgs(CommandUnderTest);

		//Assert
		string expectedRoute = Environment.Resolve<IServiceUrlBuilder>().Build(ServiceUrlBuilder.KnownRoute.Ping);
		string expectedResult = $"""
								[INF] - Ping {expectedRoute} ...
								[INF] - Done ping-app

								""";
		result.Should().Be(expectedResult, "Ping command should return 'expected response' whenever is up");
	}

	
	[TestCase("https://google.com", "S", "S")]
	[AllureName("Ping command returns Error when server returns anything other than Pong")]
	[AllureDescription("Ping command should return 'expected error' whenever url returns anything other than Pong")]
	public void PingCommand_ShouldReturnError(string url, string login, string password){
		//Arrange
		SetTestDescription();

		//Act
		string result = Environment
			.Resolve<IClioSteps>()
			.ExecuteClioCommand(CommandUnderTest, $"-u {url} -l {login} -p {password}", false);

		//Assert
		string expectedErrorMessage = $"""
									[INF] - Ping {url}/0/ping ...
									[ERR] - Ping failed, expected to receive 'Pong' instead saw:
									""";
		result.Should().StartWith(expectedErrorMessage, "Ping command should return 'expected error' whenever url is incorrect");
	}
	
	[TestCase("http://localhost:34545", "S", "S")]
	[AllureName("Ping command returns Error when server does not exist")]
	[AllureDescription("Ping command should return 'expected error' whenever url does not exist")]
	public void PingCommand_ShouldReturnErrorWhenUrlDoesNotExist(string url, string login, string password){
		//Arrange
		SetTestDescription();

		//Act
		string result = Environment
			.Resolve<IClioSteps>()
			.ExecuteClioCommand(CommandUnderTest, $"-u {url} -l {login} -p {password}", false);

		//Assert
		string expectedErrorMessage = $"""
										[INF] - Ping {url}/0/ping ...
										[ERR] - One or more errors occurred. (No connection could be made because the target machine actively refused it.
										""";
		result.Should().StartWith(expectedErrorMessage, "Ping command should return 'expected error' whenever url is incorrect");
	}
}