using Terrasoft.Common;

namespace clio.E2E.Common;

public interface IServiceUrlBuilder
{

	#region Methods: Public

	string Build(string serviceEndpoint);

	string Build(ServiceUrlBuilder.KnownRoute knownRoute);

	string Build(string serviceEndpoint, IEnvironmentSettings environmentSettings);

	string Build(ServiceUrlBuilder.KnownRoute knownRoute, IEnvironmentSettings environmentSettings);

	#endregion

}

public class ServiceUrlBuilder : IServiceUrlBuilder
{

	#region Enum: Public

	public enum KnownRoute
	{

		/// <summary>
		///     DataService Select Query
		/// </summary>
		Select = 1,

		/// <summary>
		///     DataService Insert Query
		/// </summary>
		Insert = 2,

		/// <summary>
		///     DataService Update Query
		/// </summary>
		Update = 3,

		/// <summary>
		///     DataService Delete Query
		/// </summary>
		Delete = 4,
		
		
		GetBusinessRules = 5,

		/// <summary>
		///     Start Business Process
		/// </summary>
		RunProcess = 6,

		/// <summary>
		///     Completes or continues business process
		/// </summary>
		CompleteExecuting = 7,
		GetWebHookSourceForUser = 8,
		SaveWizaTokenForUser = 9,
		DisableProcess = 10,
		EnableProcess = 11,
		Ping = 12,
		HealthCheck = 13
	}

	#endregion

	#region Constants: Private

	private const string WebAppAlias = "0/";

	#endregion

	#region Fields: Private

	private readonly IReadOnlyDictionary<KnownRoute, string> _knownRoutes = new Dictionary<KnownRoute, string> {
		{KnownRoute.Select, "DataService/json/SyncReply/SelectQuery"},
		{KnownRoute.Insert, "DataService/json/SyncReply/InsertQuery"},
		{KnownRoute.Update, "DataService/json/SyncReply/UpdateQuery"},
		{KnownRoute.Delete, "DataService/json/SyncReply/DeleteQuery"},
		{KnownRoute.RunProcess, "ServiceModel/ProcessEngineService.svc/RunProcess"},
		{KnownRoute.CompleteExecuting, "ServiceModel/ProcessEngineService.svc/CompleteExecuting"},
		{KnownRoute.GetWebHookSourceForUser, "rest/WizaAppService/GetWebHookSourceForUser"},
		{KnownRoute.SaveWizaTokenForUser, "rest/WizaAppService/SaveWizaTokenForUser"},
		{KnownRoute.DisableProcess, "ServiceModel/ProcessEngineService.svc/DisableProcess"},
		{KnownRoute.EnableProcess, "ServiceModel/ProcessEngineService.svc/EnableProcess"},
		{KnownRoute.Ping, "ping"},
		{KnownRoute.HealthCheck, "api/HealthCheck/Ping"},
	};

	private IEnvironmentSettings _environmentSettings;

	#endregion

	#region Constructors: Public

	public ServiceUrlBuilder(IEnvironmentSettings environmentSettings){
		environmentSettings.CheckArgumentNull(nameof(environmentSettings));
		_environmentSettings = environmentSettings;
	}

	#endregion

	#region Methods: Private

	private string CreateUrl(string route){
		bool isBase = Uri.TryCreate(_environmentSettings.Url, UriKind.Absolute, out Uri? baseUri);
		if (!isBase) {
			throw new ArgumentException("Misconfigured Url, check settings and try again ", nameof(_environmentSettings.Url));
		}
		
		return baseUri switch {
			_ when baseUri!.ToString().EndsWith('/') && route.StartsWith('/') => $"{baseUri}{route[1..]}",
			_ when (baseUri.ToString().EndsWith('/') && !route.StartsWith('/')) 
				|| (!baseUri.ToString().EndsWith('/') && route.StartsWith('/')) 
				=> $"{baseUri}{route}",
			_ => $"{baseUri}/{route}"
		};
	}

	#endregion

	#region Methods: Public

	public string Build(string serviceEndpoint){
		return _environmentSettings.IsNetCore switch {
			true => CreateUrl(serviceEndpoint),
			false => CreateUrl(
				$"{WebAppAlias}{(serviceEndpoint.StartsWith('/') ? serviceEndpoint[1..] : serviceEndpoint)}")
		};
	}

	public string Build(KnownRoute knownRoute) => Build(_knownRoutes[knownRoute]);
	public string Build(string serviceEndpoint, IEnvironmentSettings environmentSettings){
		_environmentSettings = environmentSettings;
		return _environmentSettings.IsNetCore switch {
			true => CreateUrl(serviceEndpoint),
			false => CreateUrl(
				$"{WebAppAlias}{(serviceEndpoint.StartsWith('/') ? serviceEndpoint[1..] : serviceEndpoint)}")
		};
	}

	public string Build(KnownRoute knownRoute, IEnvironmentSettings environmentSettings) =>
		Build(_knownRoutes[knownRoute], environmentSettings);

	#endregion

}
