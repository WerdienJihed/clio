using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using ErrorOr;
using Microsoft.Extensions.Options;

namespace clioAgent.Handlers;

[OptionsValidator]
public partial class DeploySiteHandlerArgsValidator : IValidateOptions<DeploySiteHandlerArgs> { }

public class DeploySiteHandlerArgs(string zipFolderPath, string siteFolderPath, string siteName, int sitePort) {

	#region Properties: Public

	[Required(ErrorMessage = "Value for {0} is required, and cannot be empty")]
	public string SiteFolderPath { get; init; } = siteFolderPath;

	[Required(ErrorMessage = "Value for {0} is required, and cannot be empty")]
	public string SiteName { get; init; } = siteName;

	[Range(1025, 65_535, ErrorMessage = "Value for {0} must be between {1} and {2}")]
	public int SitePort { get; init; } = sitePort;

	[Required(ErrorMessage = "Value for {0} is required, and cannot be empty")]
	public string UnzippedFolderPath { get; init; } = zipFolderPath;

	#endregion

}

public sealed class DeployIISHandler(ConnectionStringsFileHandler csHandler, Settings settings, IFileSystem fileSystem,
	IValidateOptions<DeploySiteHandlerArgs> validator) : BaseHandler {

	#region Methods: Private

	private ErrorOr<Success> AdjustConnectionStringsFile(IDirectoryInfo dirInfo, CancellationToken cancellationToken) =>
		csHandler.Execute(new Dictionary<string, object> {
			{"FolderPath", dirInfo.FullName}, {
				"DbConnectionString",
				@"Server=127.0.0.1;Port=5434;Database=semse_net472;User ID=postgres;password=Supervisor;Timeout=500; CommandTimeout=400;MaxPoolSize=1024;"
			},
			{"RedisConnectionString", @"host=127.0.0.1;db=67;port=6379"},
			{"IsNetFramework", true}
		}, cancellationToken);

	/// <summary>
	///  Copies all files except for db to IIS Folder.
	/// </summary>
	/// <param name="args">Command args.</param>
	/// <returns>
	///  An <see cref="ErrorOr{T}" /> indicating the result of the operation.
	/// </returns>
	/// <remarks>
	///  Creates necessary folders if they do not exist
	/// </remarks>
	private ErrorOr<IDirectoryInfo> CopyFiles(DeploySiteHandlerArgs args){
		try {
			IDirectoryInfo fromDirInfo = fileSystem.DirectoryInfo.New(args.UnzippedFolderPath);
			if (!fromDirInfo.Exists) {
				return Error.Failure("", $"Directory {fromDirInfo.FullName} does not exist");
			}

			IDirectoryInfo toDirInfo = fileSystem.DirectoryInfo.New(Path.Combine(args.SiteFolderPath, args.SiteName));
			if (!toDirInfo.Exists) {
				toDirInfo.Create();
			}
			string[] skipExtensions = [".backup"];
			foreach (IFileInfo file in fromDirInfo.GetFiles("*", SearchOption.AllDirectories)) {
				if (skipExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)) {
					continue;
				}
				string relativePath = Path.GetRelativePath(fromDirInfo.FullName, file.DirectoryName!);
				string destDir = Path.Combine(toDirInfo.FullName, relativePath);
				if (!fileSystem.Directory.Exists(destDir)) {
					fileSystem.Directory.CreateDirectory(destDir);
				}
				string destFileName = Path.Combine(destDir, file.Name);
				file.CopyTo(destFileName, true);
			}
			return toDirInfo.ToErrorOr();
		} catch (Exception e) {
			Error error = Error.Failure("CopyFile.Exception", e.Message);
			OnJobStatusChanged(new JobStatusChangedEventArgs(Id) {
				CurrentStatus = Status.Failed,
				Message = $"CopyFiles failed with {e.Message}"
			});
			return error;
		}
	}

	/// <summary>
	///  Extracts arguments from the command object and validates them.
	/// </summary>
	/// <param name="commandObj">The command object containing the arguments.</param>
	/// <returns>
	///  An <see cref="ErrorOr{DeploySiteHandlerArgs}" /> containing the validated arguments or validation errors.
	/// </returns>
	private ErrorOr<DeploySiteHandlerArgs> GetArgsFromCommandObj(Dictionary<string, object> commandObj){
		commandObj.TryGetValue(nameof(DeploySiteHandlerArgs.UnzippedFolderPath), out object? unzippedFolderPath);
		commandObj.TryGetValue(nameof(DeploySiteHandlerArgs.SiteFolderPath), out object? siteFolderPath);
		commandObj.TryGetValue(nameof(DeploySiteHandlerArgs.SiteName), out object? siteName);
		commandObj.TryGetValue(nameof(DeploySiteHandlerArgs.SitePort), out object? sitePortObj);
		_ = int.TryParse(sitePortObj?.ToString() ?? string.Empty, out int sitePort);

		DeploySiteHandlerArgs record = new(
			unzippedFolderPath?.ToString() ?? string.Empty,
			siteFolderPath?.ToString() ?? string.Empty,
			siteName?.ToString() ?? string.Empty,
			sitePort);

		ValidateOptionsResult validationResult = validator.Validate(nameof(DeploySiteHandlerArgs), record);
		if (validationResult.Succeeded) {
			return record;
		}
		List<Error> errors = [];
		errors.AddRange(validationResult.Failures!.Select(failure => Error.Validation("Validation", failure)));
		return errors;
	}

	#endregion

	#region Methods: Protected

	protected override ErrorOr<Success> InternalExecute(Dictionary<string, object> commandObj,
		CancellationToken cancellationToken){
		ErrorOr<DeploySiteHandlerArgs> args = GetArgsFromCommandObj(commandObj);
		if (args.IsError) {
			return args.Errors;
		}

		// 1. Copy files to IIS Folder
		Dictionary<string, object> tags = new () {
			{"from", args.Value.UnzippedFolderPath},
			{"to", args.Value.SiteFolderPath}
		};
		ErrorOr<IDirectoryInfo> isDirectoryInfoError = ExecuteWithTrace(() => CopyFiles(args.Value), tags);
		if (isDirectoryInfoError.IsError) {
			return isDirectoryInfoError.Errors;
		}

		//2. Adjust ConnectionStrings File
		ErrorOr<Success> isAdjustConnectionStringsFileError = ExecuteWithTrace(() =>
			AdjustConnectionStringsFile(isDirectoryInfoError.Value, cancellationToken));
		if (isAdjustConnectionStringsFileError.IsError) {
			return isAdjustConnectionStringsFileError.Errors;
		}
		return Result.Success;
	}

	#endregion

}