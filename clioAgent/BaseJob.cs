using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using clioAgent.Handlers;
using Microsoft.Extensions.Options;

namespace clioAgent;

[OptionsValidator]
public partial class BaseJobValidator<T> : IValidateOptions<BaseJob<T>> where T : IHandler { }

public class BaseJob<T> where T : IHandler {
	
	public Guid Id { get; init; } = Guid.NewGuid();
	
	public DateTime Date { get; init; } = DateTime.UtcNow;
	
	public string CurrentState { get; init; } = "Pending";
	
	public string HandlerName { get; init; } = string.Empty;
	
	[Required]
	public T? Handler { get; init; }
	
	public Dictionary<string, object> CommandObj { get; init;} = new ();
	
	public ActivityContext ActivityContext { get; init; }


}