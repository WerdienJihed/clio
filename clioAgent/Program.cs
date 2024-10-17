
using System.Collections.Concurrent;
using clioAgent;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateSlimBuilder(args);
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
var cancellationTokenSource = new CancellationTokenSource();

var cancelationToken = cancellationTokenSource.Token;

IConfigurationRoot configuration = new ConfigurationBuilder()
	.SetBasePath(Directory.GetCurrentDirectory())
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
	.Build();
var creatioProducts = configuration.GetSection("CreatioProducts").Get<CreatioProducts[]>();
var db = configuration.GetSection("Db").Get<Db[]>();
Settings settings = new Settings(creatioProducts, db);


builder.Services.AddSingleton(new ConcurrentBag<DeployJob>());
builder.Services.AddSingleton<Worker>();
builder.Services.AddTransient<DeployEndpointHandler>();

var app = builder.Build();

var todosApi = app.MapGroup("/executeAsync");

todosApi.MapPost("/{commandName}", (string commandName, [FromBody]Dictionary<string, object> commandObj) => commandName switch {
	"deploy" => app.Services.GetRequiredService<DeployEndpointHandler>().Execute(commandObj),
	
	_ => Results.NotFound()
});

app.MapGet("/getBag", ([FromServices]ConcurrentBag<DeployJob> bag) => {
	return Results.Text(bag.Count.ToString());
});



new Thread(async () => {
	var worker = app.Services.GetRequiredService<Worker>();
	await worker.RunAsync(cancelationToken);
}).Start();


app.Run();









public class BaseJob {
	public Guid Id { get; init; } = Guid.NewGuid();
	public DateTime Date { get; init; } = DateTime.UtcNow;
}
public class DeployJob : BaseJob{

	public string Product { get; init; }

	public string CurrentState { get; private set; }
	
	public void MoveNext(){
		
	}
	
}



public class Chain {

	public Chain(List<IStep> steps){
		Steps = steps;
		
	}
	public List<IStep> Steps { get; init; }

}




public interface IStep {

	public IStep NextStep { get; set; }
	public void Execute();

}


public abstract class SStep : IStep {

	public SStep(IStep nextStep){
		NextStep = nextStep;
	}
	public IStep NextStep { get; set; }

	public abstract void Execute();
}


public abstract class PStep : IStep {

	private readonly IEnumerable<IStep> _steps;
	private readonly IStep _nextStep;

	public PStep(IEnumerable<IStep> steps, IStep nextStep = null){
		_steps = steps;
		_nextStep = nextStep;
	}
	
	public IStep NextStep { get; set; }

	public virtual void Execute(){
		if(_steps.Count() == 1) {
			_steps.First().Execute();
		}else {
			Task.WhenAll(_steps.Select(s => s.Execute()));
		}
		_nextStep?.Execute();
		
	}
}






public class RestoreDbStep : IStep {

	public IStep NextStep { get; set; }

	public void Execute(){
		
	}
}
public class ConfigureIISStep : IStep {

	public IStep NextStep { get; set; }

	public void Execute(){
		
	}
}












