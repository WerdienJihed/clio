using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Autofac;
using Clio.Common;
using Clio.Tests.Extensions;
using Clio.Tests.Infrastructure;
using Clio.Workspaces;
using NSubstitute;
using NUnit.Framework;
using IFileSystem = System.IO.Abstractions.IFileSystem;

namespace Clio.Tests.Workspaces;

[Author("Kirill Krylov", "k.krylov@creatio.com")]
[Category("UnitTests")]
[TestFixture]
public class WorkspacePathBuilderTests
{

	#region Constants: Private

	private const string AppName = "iframe-sample";

	#endregion

	#region Fields: Private

	private readonly Action<ContainerBuilder> _customRegistrations = cb => {
		cb.RegisterInstance(EnvironmentSettings).As<EnvironmentSettings>();
		cb.RegisterInstance(WorkingDirectoriesProviderMock).As<IWorkingDirectoriesProvider>();
	};

	private readonly IFileSystem _fileSystem = TestFileSystem.MockFileSystem();

	#endregion

	#region Properties: Private

	private static EnvironmentSettings EnvironmentSettings =>
		new() {
			Uri = "http://localhost",
			Login = "Supervisor",
			Password = "Supervisor",
			IsNetCore = true
		};

	private static IWorkingDirectoriesProvider WorkingDirectoriesProviderMock = Substitute.For<IWorkingDirectoriesProvider>();
	
	private BindingsModule BindingModule { get; set; }

	private IContainer Container { get; set; }

	#endregion

	#region Methods: Public

	[OneTimeSetUp]
	public void OneTimeSetUp(){
		string p = Path.Combine("Examples", "workspaces", AppName);
		((MockFileSystem)_fileSystem).MockFolderWithDir(p);

		BindingModule = new BindingsModule(_fileSystem);
		Container = BindingModule.Register(EnvironmentSettings, false, _customRegistrations);
	}

	#endregion

	[Test]
	public void TestOne(){
		
		string originClioSourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
		string exampleWorkspacePath = Path.Combine(originClioSourcePath, "Examples", "workspaces", AppName);
		WorkingDirectoriesProviderMock.CurrentDirectory.Returns(exampleWorkspacePath);
		var sut = Container.Resolve<IWorkspacePathBuilder>();
		var p = sut.BuildPackagePath("test");
		
	}

}