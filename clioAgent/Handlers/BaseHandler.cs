using System.Diagnostics;
using System.Runtime.CompilerServices;
using ErrorOr;

namespace clioAgent.Handlers;

public abstract class BaseHandler : IHandler {

	#region Fields: Private

	private Activity? _executeActivity;

	#endregion

	#region Properties: Private

	private ActivitySource ActivitySource => new(GetType().Name);

	#endregion

	#region Properties: Public

	public ActivityContext? ActivityContext { get; set; }

	
	/// <summary>
	/// Job Id
	/// </summary>
	public Guid Id { get; set; }

	#endregion

	#region Events: Public

	public event EventHandler<JobStatusChangedEventArgs>? JobStatusChanged;

	#endregion

	#region Methods: Protected

	protected abstract ErrorOr<Success> InternalExecute(Dictionary<string, object> commandObj,
		CancellationToken cancellationToken);

	/// <summary>
	///  Creates an activity for tracing purposes.
	/// </summary>
	/// <param name="tags">Optional tags to add to the activity.</param>
	/// <param name="methodName">
	///  The name of the method creating the activity. This is automatically set by the
	///  CallerMemberName attribute.
	/// </param>
	/// <returns>An <see cref="Activity" /> object for tracing.</returns>
	/// <remarks>
	///  This method uses the <see cref="System.Diagnostics.ActivitySource" /> to create an activity for tracing purposes.
	///  If tags are provided, they are added to the activity. The method name is automatically set by the CallerMemberName
	///  attribute.
	/// </remarks>
	private Activity? CreateActivity(Dictionary<string, object>? tags = null,
		[CallerMemberName] string methodName = ""){
		return tags switch {
			null => ActivitySource.CreateActivity(
				methodName,
				ActivityKind.Internal,
				_executeActivity?.Context ?? new ActivityContext()),
			var _ => ActivitySource.CreateActivity(
				methodName,
				ActivityKind.Internal,
				_executeActivity?.Context ?? new ActivityContext(),
				tags!)
		};
	}
	
	#endregion

	#region Methods: Public

	public virtual void Dispose(){
		_executeActivity?.Dispose();
	}

	public ErrorOr<Success> Execute(Dictionary<string, object> commandObj,
		CancellationToken cancellationToken){
		if (ActivityContext != null) {
			_executeActivity
				= ActivitySource.CreateActivity(GetType().Name, ActivityKind.Internal, ActivityContext.Value);
			_executeActivity?.Start();
		}
		OnJobStatusChanged(new JobStatusChangedEventArgs(Id) {
			CurrentStatus = Status.Started,
			Message = $"{GetType().Name} - {Status.Started.ToString()}"
		});
		
		ErrorOr<Success> result = InternalExecute(commandObj, cancellationToken);
		
		return result.Match(
			onError: errors => {
				OnJobStatusChanged(new JobStatusChangedEventArgs(Id) {
					CurrentStatus = Status.Failed,
					Message = $"{GetType().Name} - {Status.Failed.ToString()}",
				});
				Error error = Error.Failure("Exception", errors.FirstOrDefault().Description);
				_executeActivity?.SetStatus(ActivityStatusCode.Error, $"{error.Code} {error.Description}");
				return errors;
			},
			onValue: ok => {
				OnJobStatusChanged(new JobStatusChangedEventArgs(Id) {
					CurrentStatus = Status.Completed,
					Message = $"{GetType().Name} - {Status.Completed.ToString()}",
				});
				_executeActivity?.SetStatus(ActivityStatusCode.Ok);
				return result;
			});
		
	}

	#endregion

	private protected void OnJobStatusChanged(JobStatusChangedEventArgs e){
		JobStatusChanged?.Invoke(this, e);
	}

	private protected ErrorOr<T> ExecuteWithTrace<T>(Func<ErrorOr<T>> action, Dictionary<string, object>? tags = null,
		[CallerMemberName] string methodName = ""){
		Activity? activity = CreateActivity(tags, methodName);
		activity?.Start();
		Guid stepId = Guid.NewGuid();
		OnJobStatusChanged(new JobStatusChangedEventArgs(Id) {
			CurrentStatus = Status.Started,
			Message = $"{methodName} - {Status.Started.ToString()}",
			StepId = stepId
		});
		
		ErrorOr<T> result = action.Invoke();
		
		return action.Invoke().Match(
			onError: errors => {
				OnJobStatusChanged(new JobStatusChangedEventArgs(Id) {
					CurrentStatus = Status.Failed,
					Message = $"{methodName} - {Status.Failed.ToString()}",
					StepId = stepId
				});
				
				Error error = Error.Failure("Exception", errors.FirstOrDefault().Description);
				activity?.SetStatus(ActivityStatusCode.Error, $"{error.Code} {error.Description}");
				activity?.Dispose();
				return result;
			},
			onValue: _ => {
				OnJobStatusChanged(new JobStatusChangedEventArgs(Id) {
					CurrentStatus = Status.Completed,
					Message = $"{methodName} - {Status.Completed.ToString()}",
					StepId = stepId
				});
				activity?.SetStatus(ActivityStatusCode.Ok);
				activity?.Dispose();
				return result;
			});
	}

}