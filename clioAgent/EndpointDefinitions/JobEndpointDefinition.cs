using System.Collections.Concurrent;
using clioAgent.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace clioAgent.EndpointDefinitions;

public class JobEndpointDefinition : IEndpointDefinition{

	public void DefineEndpoints(WebApplication app){
		RouteGroupBuilder jobsApi = app.MapGroup("/Job"); //.RequireAuthorization("ReadPolicy"); -> Fallback policy
		jobsApi.MapGet("/Count", JobsCount);
		jobsApi.MapGet("/Status/All", JobStatusAll);
		
		//Cannot use guid, see https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/3117
		jobsApi.MapGet("/Status/{id}",JobStatusById);
		jobsApi.MapGet("/Status/Steps/{id:guid}", StatusStepById);
	}

	/// <summary>
	/// Returns the count of jobs in the provided ConcurrentQueue.
	/// </summary>
	/// <param name="jobs">A ConcurrentQueue of BaseJob objects.</param>
	/// <returns>An IResult containing the count of jobs.</returns>
	private static IResult JobsCount(ConcurrentQueue<BaseJob<IHandler>> jobs) => Results.Ok(jobs.Count);
	
	/// <summary>
	/// Returns the status of all jobs in the provided ConcurrentDictionary.
	/// </summary>
	/// <param name="jobs">A ConcurrentDictionary of job statuses.</param>
	/// <returns>An IResult containing all job statuses.</returns>
	private static IResult JobStatusAll(ConcurrentDictionary<Guid, JobStatus> jobs)=> Results.Ok(jobs);
	
	/// <summary>
	/// Returns the status steps of a job by its ID.
	/// </summary>
	/// <param name="id">The ID of the job.</param>
	/// <param name="steps">A ConcurrentBag of job status steps.</param>
	/// <returns>An IResult containing the status steps of the job.</returns>
	private static IResult StatusStepById([FromRoute]Guid id, ConcurrentBag<JobStatus> steps)=> 
		Results.Ok(
		steps.Where(x => x.JobId == id)
		//steps.Where(x => x.JobId == Guid.Parse(id))
			.Select(x => 
				new StepStatus(x.JobId, x.Message, x.Date, x.CurrentStatus.ToString(), x.StepId))
	);
	
	/// <summary>
	/// Returns the status of a job by its ID.
	/// </summary>
	/// <param name="id">The ID of the job.</param>
	/// <param name="jobs">A ConcurrentDictionary of job statuses.</param>
	/// <returns>An IResult containing the status of the job or a not found message.</returns>
	private static IResult JobStatusById([FromRoute]Guid id, ConcurrentDictionary<Guid, JobStatus> jobs) =>
		Results.Ok(jobs.TryGetValue(id, out JobStatus? status) 
		//Results.Ok(jobs.TryGetValue(Guid.Parse(id), out JobStatus? status) 
			? string.Join(' ', [status?.CurrentStatus, status?.Message]) 
			: $"Job not found by id {id}");

	public void DefineServices(IServiceCollection services){}
}