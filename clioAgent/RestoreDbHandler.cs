using System.Diagnostics;
using Clio;
using Clio.Common;
using Clio.Common.db;

namespace clioAgent;

public class RestoreDbHandler {

	private readonly Settings _settings;

	 public RestoreDbHandler(Settings settings){
	 	_settings = settings;
	 	
	 }
	public IResult Execute(Dictionary<string, object> commandObj){
		
		commandObj.TryGetValue("CreatioBuildZipPath", out object creatioBuildZipPath);
		commandObj.TryGetValue("Name", out object nameObj);
		
		//1. CopyFile
		string fileName=Path.GetFileName(creatioBuildZipPath.ToString());
		var ext = Path.GetExtension(fileName);
		var fileNameWihtoutExt = fileName.Replace(ext, "");
		
		// File.Copy(creatioBuildZipPath.ToString(), 
		// 	Path.Combine(_settings.WorkingDirectoryPath, fileName));
		//
		//2. Unzip
		 string extractPath = Path.Combine(_settings.WorkingDirectoryPath, fileNameWihtoutExt);
		// System.IO.Compression.ZipFile.ExtractToDirectory(
		// 	Path.Combine(_settings.WorkingDirectoryPath, fileName), 
		// 	extractPath
		// );
		
		
		//3. Copy db to ps data folder
		//skip for pg
		
		var binPath = _settings.Db.Where(db=> db.Type == "PGSQL").FirstOrDefault()
			.Servers.FirstOrDefault().BinFolderPath;
		
		var cs = _settings.Db.Where(db=> db.Type == "PGSQL").FirstOrDefault()
			.Servers.FirstOrDefault().ConnectionString;
		
		
		//CreateDb
		var postgres = new Postgres(cs);
		postgres.CreateDb(nameObj.ToString());
		
		
		ProcessStartInfo startInfo = new () {
			FileName = Path.Combine(binPath, "pg_restore.exe"),
			ArgumentList = { 
				$"--dbname={nameObj}", 
				"--verbose",
				"--no-owner", 
				"--no-privileges", 
				"--jobs=4", 
				"--username=postgres",
				$"{Path.Combine(extractPath, "db", "BPMonline820SalesEnterprise_Marketing_ServiceEnterprise.backup")}" 
			},
			UseShellExecute = false,
			WorkingDirectory = binPath,
			EnvironmentVariables = {
				{"PGPASSWORD","Supervisor"}
			},
		};
		using Process? process = Process.Start(startInfo);
		
		process.OutputDataReceived += (sender, args) => {
			Console.WriteLine(args.Data);
		};
		process.ErrorDataReceived += (sender, args) => {
			Console.WriteLine(args.Data);
		};
		
		process.WaitForExit();
		
		
		//4. pgrestore
		//5. SQL operations to convert template to db
		//6. return connection string
		
		var logger = ConsoleLogger.Instance; 
		System.IO.Abstractions.IFileSystem fileSystem = new System.IO.Abstractions.FileSystem();
		WorkingDirectoriesProvider provider = new WorkingDirectoriesProvider(logger, fileSystem);
		var tempDir = provider.CreateTempDirectory();
		
		
		
		return Results.Ok();
	}
}