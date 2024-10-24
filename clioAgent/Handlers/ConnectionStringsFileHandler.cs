using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Xml;
using ErrorOr;
using Microsoft.Extensions.Options;

namespace clioAgent.Handlers;

[OptionsValidator]
public partial class ConnectionStringsFileHandlerArgsValidator : IValidateOptions<ConnectionStringsFileHandlerArgs> { }

public class ConnectionStringsFileHandlerArgs {

	#region Properties: Public

	[Required]
	public string? DbConnectionString { get; init; }

	[Required]
	public string? FolderPath { get; init; }

	[Required]
	public bool IsNetFramework { get; init; }

	[Required]
	public string? RedisConnectionString { get; init; }

	#endregion

}

public class ConnectionStringsFileHandler(IValidateOptions<ConnectionStringsFileHandlerArgs> validator) : BaseHandler {


	protected override ErrorOr<Success> InternalExecute(Dictionary<string, object> commandObj, CancellationToken cancellationToken){
		
		ErrorOr<ConnectionStringsFileHandlerArgs> args = GetArgsFromCommandObj(commandObj);
		string cnPath = Path.Join(args.Value.FolderPath, "ConnectionStrings.config");
		ErrorOr<Success> isConfigureConnectionStringsError
			= ConfigureConnectionStrings(cnPath, args.Value.DbConnectionString, args.Value.RedisConnectionString);
		if (isConfigureConnectionStringsError.IsError) {
			return isConfigureConnectionStringsError.Errors;
		}
		
		if (!args.Value.IsNetFramework) {
			string webConfigPath = Path.Join(args.Value.FolderPath, "Terrasoft.WebHost.dll.config");
			ErrorOr<Success> isUpdateWebConfigError = UpdateWebConfig(webConfigPath);
			if (isUpdateWebConfigError.IsError) {
				return isUpdateWebConfigError.Errors;
			}
		}
		return Result.Success;
	}


	#region Methods: Private

	private ErrorOr<Success> ConfigureConnectionStrings(string cnFilePath, string db, string redis){
		
		try {
			string cnFileContent = File.ReadAllText(cnFilePath);
			XmlDocument doc = new();
			doc.LoadXml(cnFileContent);
			XmlNode root = doc.DocumentElement;

			XmlNode dbPostgreSqlNode = root?.SelectSingleNode("descendant::add[@name='dbPostgreSql']");
			if (dbPostgreSqlNode != null) {
				dbPostgreSqlNode.Attributes["connectionString"].Value = db;
			}

			XmlNode dbNode = root?.SelectSingleNode("descendant::add[@name='db']");
			if (dbNode != null) {
				dbNode.Attributes["connectionString"].Value = db;
			}

			XmlNode redisNode = root?.SelectSingleNode("descendant::add[@name='redis']");
			if (redisNode != null) {
				redisNode.Attributes["connectionString"].Value = redis;
			}
			doc.Save(cnFilePath);
			return Result.Success;
		} 
		catch (Exception e) {
			OnJobStatusChanged(new JobStatusChangedEventArgs(Id) {
				CurrentStatus = Status.Failed,
				Message = $"Configuring connection strings in {cnFilePath}"
			});
			return Error.Failure("ConfigureConnectionStrings", e.Message);
		}
	}

	private ErrorOr<Success> UpdateWebConfig(string webConfigPath){
		try {
			string configContent = File.ReadAllText(webConfigPath);
			XmlDocument doc = new();
			doc.LoadXml(configContent);
			XmlNode root = doc.DocumentElement;
			XmlNode cookiesSameSiteModeNode = root?.SelectSingleNode("descendant::add[@key='CookiesSameSiteMode']");
			if (cookiesSameSiteModeNode != null) {
				cookiesSameSiteModeNode.Attributes["value"].Value = "Lax";
			}
			doc.Save(webConfigPath);
			return Result.Success;
		} 
		catch (Exception e) {
			OnJobStatusChanged(new JobStatusChangedEventArgs(Id) {
				CurrentStatus = Status.Failed,
				Message = $"Configuring webConfig in {webConfigPath} failed"
			});
			return Error.Failure("UpdateWebConfig", e.Message);
		}
	}

	/// <summary>
	///  Extracts arguments from the command object and validates them.
	/// </summary>
	/// <param name="commandObj">The command object containing the arguments.</param>
	/// <returns>
	///  An <see cref="ErrorOr{DeploySiteHandlerArgs}" /> containing the validated arguments or validation errors.
	/// </returns>
	private ErrorOr<ConnectionStringsFileHandlerArgs> GetArgsFromCommandObj(Dictionary<string, object> commandObj){
		commandObj.TryGetValue(nameof(ConnectionStringsFileHandlerArgs.FolderPath), out object? folderPath);
		commandObj.TryGetValue(nameof(ConnectionStringsFileHandlerArgs.DbConnectionString),
			out object? dbConnectionString);
		commandObj.TryGetValue(nameof(ConnectionStringsFileHandlerArgs.RedisConnectionString),
			out object? redisConnectionString);
		commandObj.TryGetValue(nameof(ConnectionStringsFileHandlerArgs.IsNetFramework), out object? isNetFrameworkObj);
		_ = bool.TryParse(isNetFrameworkObj?.ToString() ?? string.Empty, out bool isNetFramework);

		ConnectionStringsFileHandlerArgs record = new() {
			FolderPath = folderPath?.ToString() ?? string.Empty,
			DbConnectionString = dbConnectionString?.ToString() ?? string.Empty,
			RedisConnectionString = redisConnectionString?.ToString() ?? string.Empty,
			IsNetFramework = isNetFramework
		};

		ValidateOptionsResult validationResult = validator.Validate(nameof(ConnectionStringsFileHandlerArgs), record);
		if (validationResult.Succeeded) {
			return record;
		}
		List<Error> errors = [];
		errors.AddRange(validationResult.Failures!.Select(failure => Error.Validation("Validation", failure)));
		return errors;
	}

	#endregion
	

}