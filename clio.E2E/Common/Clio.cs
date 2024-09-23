using System.Diagnostics;
using System.Reflection;
using FluentAssertions;

namespace clio.E2E.Common;

/// <summary>
///  Interface for executing commands with specified parameters.
/// </summary>
public interface IClio {

	#region Methods: Public

	/// <summary>
	///  Executes a command using the specified parameters.
	/// </summary>
	/// <param name="commandName">The name of the command to execute.</param>
	/// <param name="clioArgs">The arguments to pass to the command.</param>
	/// <param name="useDefEnvironment">Auto applies Url, Login, Password from app-settings fille</param>
	/// <param name="workingDirectory">The working directory for the command execution. Optional.</param>
	/// <param name="envVariables">Environment variables to set for the command execution. Optional.</param>
	/// <returns>The standard output of the executed command.</returns>
	string ExecuteCommand(string commandName, string clioArgs, bool useDefEnvironment = true, 
		string workingDirectory = "", Dictionary<string, string>? envVariables = null);

	#endregion

}

/// <summary>
///  Class for executing commands with specified parameters.
/// </summary>
/// <param name="appSettings">The application settings to use for command execution.</param>
public class Clio(IEnvironmentSettings appSettings) : IClio {

	#region Fields: Private

	private readonly IEnvironmentSettings _appSettings = appSettings;

	#endregion

	#region Methods: Public


	public string ExecuteCommand(string commandName, string clioArgs, bool useDefEnvironment = true, string workingDirectory = "",
		Dictionary<string, string>? envVariables = null){
		
		string mainLocation = Assembly.GetExecutingAssembly().Location;
		string mainLocationDirPath = Path.GetDirectoryName(mainLocation)
			?? throw new Exception("Could not get the directory of the current assembly"); //Should never happen

		//TODO: Once we change the project structure, this path will need to be updated (i.e. NET8)
		string clioDevPath = Path.Combine(mainLocationDirPath, "..", "..", "..", "..", "clio", "bin", "Debug", "net6.0",
			"clio.exe");

		string envArgs = useDefEnvironment switch {
			true => $"-u {_appSettings.Url} -l {_appSettings.Login} -p {_appSettings.Password} -i {_appSettings.IsNetCore}",
			false => string.Empty,
		};

		ProcessStartInfo psi = new() {
			FileName = clioDevPath,
			Arguments = $"{commandName} {clioArgs} {envArgs}",
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};

		if (!string.IsNullOrWhiteSpace(workingDirectory)) {
			psi.WorkingDirectory = workingDirectory;
		}

		if (envVariables != null) {
			foreach (KeyValuePair<string, string> kvp in envVariables) {
				psi.EnvironmentVariables.Add(kvp.Key, kvp.Value);
			}
		}

		Process? process = Process.Start(psi);
		if (process is null) {
			process.Should().NotBeNull("because the clio-dev process should start successfully");
		}
		process!.WaitForExit();
		return process.StandardOutput.ReadToEnd();
	}

	#endregion

}