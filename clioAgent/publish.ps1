dotnet publish -c Release -r  win-x64 --force;
Copy-Item -Path .\appsettings.json -Destination .\bin\Release\net8.0\win-x64\native\appsettings.json -Recurse -Force;
Copy-Item -Path .\start-service.ps1 -Destination .\bin\Release\net8.0\win-x64\native\start-service.ps1 -Recurse -Force;


