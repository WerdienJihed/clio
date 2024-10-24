namespace clioAgent.EndpointDefinitions;

public interface IEndpointDefinition {

	/// <summary>
    /// Defines the endpoints for the application.
    /// </summary>
	void DefineEndpoints(WebApplication app);
	
	/// <summary>
	/// Defines the services for the application.
	/// Provides facility to register services in DI container.
	/// </summary>
	void DefineServices(IServiceCollection services);

}