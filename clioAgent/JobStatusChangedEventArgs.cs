using clioAgent.Handlers;
using ErrorOr;

namespace clioAgent;

public class JobStatusChangedEventArgs(Guid jobId) {

	#region Properties: Public
	public Status CurrentStatus { get; init; }
	public List<Error>? Error { get; init; }
	public Guid JobId { get; set; } = jobId;
	public string? Message { get; init; }
	public Guid StepId { get; init; }

	public DateTime Type { get; set; } = DateTime.UtcNow;

	#endregion

}