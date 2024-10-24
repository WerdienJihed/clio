using System.Diagnostics;
using ErrorOr;

namespace clioAgent.Handlers;

public interface IHandler : IDisposable {
	ActivityContext? ActivityContext { get; set; }
	Guid Id { get; set; }
	ErrorOr<Success> Execute(Dictionary<string, object> commandObj, CancellationToken cancellationToken);
	event EventHandler<JobStatusChangedEventArgs>? JobStatusChanged;
}