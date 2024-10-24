using clioAgent.AuthPolicies;
using clioAgent.Handlers;
using Microsoft.Extensions.Options;

namespace clioAgent.EndpointDefinitions;

public class TestEndpointDefinition: IEndpointDefinition {

	public void DefineEndpoints(WebApplication app){
		RouteGroupBuilder group = app.MapGroup("/Test");
		group.MapPost("ObjectValidation", ObjValidation);
			
	}

	private IResult ObjValidation(IValidateOptions<DeploySiteHandlerArgs> v){
		DeploySiteHandlerArgs args  = new("","","",0);
		ValidateOptionsResult x = v.Validate(nameof(DeploySiteHandlerArgs), args);
		return x switch  {
			var _ when x.Failed => Results.BadRequest(CustomProblem.CreateCustomProblem(400, x.Failures)),
			var _ => Results.Ok("Object is valid")
		};
	}

	public void DefineServices(IServiceCollection services){}

}