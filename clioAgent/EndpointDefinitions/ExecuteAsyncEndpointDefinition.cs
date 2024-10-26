using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json.Serialization;
using clioAgent.AuthPolicies;
using clioAgent.Handlers;
using Microsoft.OpenApi.Models;

namespace clioAgent.EndpointDefinitions;
public class ExecuteAsyncEndpointDefinition : IEndpointDefinition{

	public void DefineEndpoints(WebApplication app){
		RouteGroupBuilder route = app
			.MapGroup("/executeAsync")
			.RequireAuthorization("AdminPolicy");
		
		route
			.MapPost("/restoredb", RestoreDb)
			.WithOpenApi(operation => new OpenApiOperation(operation) {
				Summary = "Restores a database",
				Description = "Restores a database - description",
			})
			.Produces<RestoreDbResponse>();
		
		route
			.MapPost("/createSite", CreateSite)
			.Produces<RestoreDbResponse>();
	}
	
	private static IResult RestoreDb(Worker worker, Settings settings, 
		ConcurrentQueue<BaseJob<IHandler>> jobs, Dictionary<string, object> commandObj) 
		=> CreateResponse(worker, settings, jobs, commandObj, "RestoreDb");

	private static IResult CreateSite(Worker worker, Settings settings, 
		ConcurrentQueue<BaseJob<IHandler>> jobs, Dictionary<string, object> commandObj) 
		=> CreateResponse(worker, settings, jobs, commandObj, "CreateSite");

	
	private static IResult CreateResponse(Worker worker, Settings settings, 
		ConcurrentQueue<BaseJob<IHandler>> jobs, Dictionary<string, object> commandObj, string commandName)
		=> worker.AddJobToQueue(jobs, commandObj, commandName).Match(
			onValue: job=>Results.Ok(new RestoreDbResponse(
				job.Id, 
				job.ActivityContext.TraceId.ToString(), 
				settings.TraceServer?.Enabled ?? false 
					? new Uri($"{settings.TraceServer.UiUrl}trace/{job.ActivityContext.TraceId}", UriKind.Absolute)
					: null)),
			onError: errors=>Results.BadRequest(CustomProblem.CreateCustomProblem(400, errors))
		);
	
	
	public void DefineServices(IServiceCollection services){}
}

public record RestoreDbResponse(Guid JobId, string TraceId, Uri? TraceUrl);