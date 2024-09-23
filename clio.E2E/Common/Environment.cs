using System.Collections;
using System.Text.Json;
using ATF.Repository.Providers;
using clio.E2E.CommonAsserts;
using clio.E2E.CommonSteps;
using Creatio.Client;
using Microsoft.Extensions.DependencyInjection;

namespace clio.E2E.Common;

public interface IEnvironment
{

	#region Methods: Public

	/// <summary>
	///  Resolves a service of type <typeparamref name="T" /> from the dependency injection container.
	/// </summary>
	/// <typeparam name="T">The type of service to resolve.</typeparam>
	/// <returns>The resolved service of type <typeparamref name="T" />.</returns>
	T Resolve<T>();

	#endregion

}

public class Environment : IEnvironment
{

	#region Fields: Private

	private readonly ServiceProvider _serviceProvider;

	#endregion

	#region Constructors: Private

	private Environment(){
		ServiceCollection collection = new();
		IEnvironmentSettings environmentSettings = InitEnvSettings() ?? InitSettingsFromFile("appsettings.json");
		collection.AddSingleton(environmentSettings);
		
		collection.AddSingleton<ICreatioClient>(sp=> {
			IEnvironmentSettings settings = sp.GetRequiredService<IEnvironmentSettings>();
			return new CreatioClient(settings.Url, settings.Login,
				settings.Password, settings.IsNetCore);
		});
		
		collection.AddSingleton<IDataProvider>(sp => {
			IEnvironmentSettings settings = sp.GetRequiredService<IEnvironmentSettings>();
			return new RemoteDataProvider(settings.Url, settings.Login, settings.Password, settings.IsNetCore);
		});

		collection.AddTransient<IClioSteps, ClioSteps>();
		collection.AddTransient<IClio, Clio>();
		collection.AddSingleton<IServiceUrlBuilder, ServiceUrlBuilder>();
		collection.AddTransient<HealthCheckAsserts>();
		_serviceProvider = collection.BuildServiceProvider();
	}

	#endregion

	#region Methods: Private

	private static EnvironmentSettings? InitEnvSettings(){
		IDictionary envVars = System.Environment.GetEnvironmentVariables();
		bool isNetCore = false;
		string login = string.Empty;
		string password = string.Empty;
		string clientId = string.Empty;
		string clientSecret = string.Empty;
		string url = string.Empty;
		string authAppUri = string.Empty;
		bool t1 = false;
		bool t2 = false;
		bool t3 = false;
		bool t4 = false;

		if (envVars.Contains("CREATIO_IS_NETCORE")) {
			string isNetCoreStr = envVars["CREATIO_IS_NETCORE"]?.ToString() ?? string.Empty;
			t1 = bool.TryParse(isNetCoreStr, out isNetCore);
		}
		if (envVars.Contains("CREATIO_URL")) {
			url = envVars["CREATIO_URL"]?.ToString() ?? string.Empty;
			t2 = !string.IsNullOrWhiteSpace(url);
		}
		if (envVars.Contains("CREATIO_LOGIN")) {
			login = envVars["CREATIO_LOGIN"]?.ToString() ?? string.Empty;
			t3 = !string.IsNullOrWhiteSpace(login);
		}
		if (envVars.Contains("CREATIO_PASSWORD")) {
			password = envVars["CREATIO_PASSWORD"]?.ToString() ?? string.Empty;
			t4 = !string.IsNullOrWhiteSpace(password);
		}
		
		if (envVars.Contains("CREATIO_CLIENT_ID")) {
			clientId = envVars["CREATIO_CLIENT_ID"]?.ToString() ?? string.Empty;
		}
		
		if (envVars.Contains("CREATIO_CLIENT_SECRET")) {
			clientSecret = envVars["CREATIO_CLIENT_SECRET"]?.ToString() ?? string.Empty;
		}
		
		if (envVars.Contains("CREATIO_AUTH_APP_URI")) {
			authAppUri = envVars["CREATIO_AUTH_APP_URI"]?.ToString() ?? string.Empty;
		}

		if (t1 && t2 && t3 && t4) {
			return new EnvironmentSettings(password, login,clientId,clientSecret, authAppUri, url, isNetCore);
		}
		return null;
	}

	private static EnvironmentSettings InitSettingsFromFile(string fileName){
		string json = File.ReadAllText(fileName);
		return JsonSerializer.Deserialize<EnvironmentSettings>(json) ?? throw new Exception("Invalid settings file.");
	}

	#endregion

	#region Methods: Public

	/// <summary>
	///  Gets an instance of the <see cref="IEnvironment" />.
	/// </summary>
	/// <returns>An instance of the <see cref="IEnvironment" />.</returns>
	public static IEnvironment GetInstance() => new Environment();

	/// <summary>
	///  Resolves a service of type <typeparamref name="T" /> from the dependency injection container.
	/// </summary>
	/// <typeparam name="T">The type of service to resolve.</typeparam>
	/// <returns>The resolved service of type <typeparamref name="T" />.</returns>
	/// <exception cref="Exception">Thrown when the service of type <typeparamref name="T" /> is not found in the DI container.</exception>
	public T Resolve<T>(){
		T? service = _serviceProvider.GetService<T>();
		if (service is null) {
			throw new Exception($"Service {typeof(T).Name} not found in DI container");
		}
		return service;
	}

	#endregion

}