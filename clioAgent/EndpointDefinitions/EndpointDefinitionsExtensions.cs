using System.Diagnostics.CodeAnalysis;


namespace clioAgent.EndpointDefinitions;

public static class EndpointDefinitionsExtensions {
	public static void AddEndpointDefinitions(this IServiceCollection services, params Type[] scanMarkers) {
		List<IEndpointDefinition> endpointDefinitions = [
			Activator.CreateInstance<ExecuteAsyncEndpointDefinition>(),
			Activator.CreateInstance<JobEndpointDefinition>(),
			Activator.CreateInstance<TestEndpointDefinition>(),
		];
		
		foreach (IEndpointDefinition endpointDefinition in endpointDefinitions) {
			endpointDefinition.DefineServices(services);
		}
		services.AddSingleton<IReadOnlyCollection<IEndpointDefinition>>(endpointDefinitions);
	}
	
	
	public static void UseEndpointDefinitions(this WebApplication app){
		IReadOnlyCollection<IEndpointDefinition> defs = app.Services.GetRequiredService<IReadOnlyCollection<IEndpointDefinition>>();
		foreach (IEndpointDefinition endpointDefinition in defs) {
			endpointDefinition.DefineEndpoints(app);
		}
	}

}