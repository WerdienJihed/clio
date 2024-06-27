namespace Clio.Command
{
	using System;
	using System.Linq;
	using System.Xml.Linq;
	using Clio.Common;
	using Clio.Package;
	using Clio.Workspaces;
	using CommandLine;

	#region Class: DownloadLibsCommandOptions

	[Verb("download-configuration", Aliases = new [] { "dconf" },
		HelpText = "Download libraries from web-application")]
	public class DownloadConfigurationCommandOptions : EnvironmentOptions
	{
	}

	#endregion

	#region Class: DownloadLibsCommand

	public class DownloadConfigurationCommand : Command<DownloadConfigurationCommandOptions>
	{
		
		#region Fields: Private

		private readonly IApplicationDownloader _applicationDownloader;
		private readonly IWorkspace _workspace;
		private IPackageDownloader _packageDownloader;
		private readonly IWorkspacePathBuilder _workspacePathBuilder;

		#endregion

		#region Constructors: Public

		public DownloadConfigurationCommand(IApplicationDownloader applicationDownloader, 
			IPackageDownloader packageDownloader, IWorkspacePathBuilder workspacePathBuilder, IWorkspace workspace) {
			applicationDownloader.CheckArgumentNull(nameof(applicationDownloader));
			workspace.CheckArgumentNull(nameof(workspace));
			_applicationDownloader = applicationDownloader;
			_packageDownloader = packageDownloader;
			_workspace = workspace;
			_workspacePathBuilder = workspacePathBuilder;
		}

		#endregion

		private void DownloadDependentPackages() {
			var dependentPackages = _workspace.WorkspaceSettings.Packages.SelectMany(package => {
				var csprojFilePath = System.IO.Path.Combine(_workspacePathBuilder.PackagesFolderPath, package,
					$"Files/{package}.csproj");
				var document = XDocument.Load(csprojFilePath);
				var currentPackages = document
					.Descendants("ItemGroup")
					.Where(itemGroup => itemGroup.Attribute("Label")?.Value == "Package References")
					.Elements("Reference")
					.Select(reference => new {
						PackageName = reference.Attribute("Include")?.Value,
						FilePath = reference.Element("HintPath")?.Value
					})
					.Where(pkg => !string.IsNullOrEmpty(pkg.PackageName) && !string.IsNullOrEmpty(pkg.FilePath))
					.Select(pkg => pkg.PackageName);
				return currentPackages;
			}).Distinct().ToList();

			_packageDownloader.DownloadPackages(dependentPackages,
				destinationPath: _workspacePathBuilder.ApplicationPackagesFolderPath);
		}

		#region Methods: Public

		public override int Execute(DownloadConfigurationCommandOptions options) {
			try {
				_applicationDownloader.Download(_workspace.WorkspaceSettings.Packages);
				DownloadDependentPackages();

				Console.WriteLine("Done");
				return 0;
			} catch (Exception e) {
				Console.WriteLine(e.Message);
				return 1;
			}
		}

		#endregion


	}

	#endregion

}