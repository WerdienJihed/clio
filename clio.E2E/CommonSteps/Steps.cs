using Allure.NUnit.Attributes;
using clio.E2E.Common;

namespace clio.E2E.CommonSteps;

public enum CommandName {

	ping, 
	healthcheck

}

public interface IClioSteps {

	#region Methods: Public

	string ExecuteClioCommand(CommandName commandName, string args, bool useDefaultEnv = true);

	string ExecuteClioCommandWithoutArgs(CommandName commandName, bool useDefaultEnv = true);

	#endregion

}

public class ClioSteps : IClioSteps {

	#region Fields: Private

	private readonly IClio _clio;

	#endregion

	#region Constructors: Public

	public ClioSteps(IClio clio){
		_clio = clio;
	}

	#endregion

	#region Methods: Private

	[AllureStep("Execute clio command {commandName} with arguments {args} and return result")]
	private string ExecuteClioCommand(string commandName, string args, bool useDefaultEnv = true) =>
		_clio.ExecuteCommand(commandName, args, useDefaultEnv);

	[AllureStep("Execute clio command {commandName} without arguments, and return result")]
	private string ExecuteClioCommandWithoutArgs(string commandName, bool useDefaultEnv = true) =>
		_clio.ExecuteCommand(commandName, string.Empty, useDefaultEnv);

	#endregion

	#region Methods: Public

	public string ExecuteClioCommand(CommandName commandName, string args, bool useDefaultEnv = true) =>
		ExecuteClioCommand(commandName.ToString(), args, useDefaultEnv);

	public string ExecuteClioCommandWithoutArgs(CommandName commandName, bool useDefaultEnv = true) =>
		ExecuteClioCommandWithoutArgs(commandName.ToString(), useDefaultEnv);

	#endregion

}