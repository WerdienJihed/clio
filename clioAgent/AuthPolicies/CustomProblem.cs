using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace clioAgent.AuthPolicies;

public class CustomProblem: ProblemDetails {
	
	private CustomProblem(int status){
		Status = status;
		Type = "https://github.com/Advance-Technologies-Foundation/clio/blob/master/README.md";
	}
	public CustomProblem(int status, string detail): this(status){
		Title = ((HttpStatusCode)status).ToString();
		Detail = detail;
	}
	public CustomProblem(int status, IEnumerable<string> extensions): this(status){
		Title = ((HttpStatusCode)status).ToString();
		Detail = extensions.FirstOrDefault();
		Extensions = new Dictionary<string, object?> {
			{ "reasons", extensions }
		};
	}
	
	public static CustomProblem CreateCustomProblem(int status, IEnumerable<string> extensions) 
		=> new(status, extensions);

	public static CustomProblem CreateCustomProblem(int status, string detail) 
		=> new(status, detail);

	public static CustomProblem CreateCustomProblem(int status, IEnumerable<Error> errors) 
		=> new(status, errors.Select(e => e.Description));

	public static CustomProblem CreateCustomProblem(int status, IEnumerable<ValidationResult> errors) 
		=> new(status, errors.Select(e => e.ErrorMessage ?? "Unknown Error"));

	
	public override string ToString() => JsonSerializer.Serialize(this, AgentJsonSerializerContext.Default.CustomProblem);
}