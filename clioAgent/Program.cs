using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;
using System.Security.Claims;
using System.Text.Json;
using clioAgent;
using clioAgent.AuthPolicies;
using clioAgent.EndpointDefinitions;
using clioAgent.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ConnectionStringsFileHandlerArgsValidator = clioAgent.Handlers.ConnectionStringsFileHandlerArgsValidator;

CancellationTokenSource cancellationTokenSource = new();
const string defaultWorkingDirectoryPath = "C:\\ClioAgent";
const string serviceName = "clioAgent";
WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Host.UseWindowsService(s=> {
});


var s = Guid.NewGuid().ToString();
Guid.TryParse(s, CultureInfo.InvariantCulture, out Guid g);

builder.Services.ConfigureHttpJsonOptions(options => {
	options.SerializerOptions.
		TypeInfoResolverChain.Insert(0, clioAgent.AgentJsonSerializerContext.Default);
});
Settings settings = ConfigureSettings();

if(settings.TraceServer?.Enabled == true  && settings.TraceServer.CollectorUrl != null){
	AddTelemetry();
}
ConfigureDi();
ConfigureAuth();
ConfigureObjectValidators();
builder.Services.AddEndpointDefinitions([typeof(Program)]);
ConfigureSwagger();

WebApplication app = builder.Build();
UseSwagger();

StartWorkerThread();
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpointDefinitions();


app.MapGet("/turn-off", (IHostApplicationLifetime appLifetime) => {
	cancellationTokenSource.Cancel();
	appLifetime.StopApplication();
	Thread.Sleep(1_000);
	return Results.Ok("Shutting down");
});

app.Run();
return;


void UseSwagger(){
	app.UseSwagger();
	app.UseSwaggerUI();
}


void AddTelemetry(){
	builder.Services.AddOpenTelemetry()
		.WithTracing(tracing => tracing
		.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
		.AddAspNetCoreInstrumentation()
		.AddSource(nameof(RestoreDbHandler))
		.AddNpgsql()
		.AddOtlpExporter( options => {
			options.Endpoint =  settings!.TraceServer!.CollectorUrl!;
			options.Protocol = OtlpExportProtocol.Grpc;
		}));
}

void ConfigureAuth(){
	builder.Services.AddAuthorizationBuilder()
		.AddPolicy("AdminPolicy", policy =>
			policy.Requirements.Add(new AuthorizationRequirement(ClaimTypes.Role, Roles.Admin)))
		.AddPolicy("ReadPolicy", policy =>
			policy.Requirements.Add(new AuthorizationRequirement(ClaimTypes.Role, Roles.Read)));

	builder.Services.AddAuthentication(options => {
			options.DefaultAuthenticateScheme = ApiKeyAuthenticationHandler.SchemeName;
			options.DefaultChallengeScheme = ApiKeyAuthenticationHandler.SchemeName;
		})
		.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
			ApiKeyAuthenticationHandler.SchemeName, options=> {
				builder.Configuration.Bind("ApiKeySettings", options);
				options.TimeProvider = TimeProvider.System;
			});
}


void ConfigureDi(){
	builder.Services.AddSingleton<IFileSystem>(new FileSystem());
	builder.Services.AddSingleton(new ConcurrentQueue<BaseJob<IHandler>>());
	builder.Services.AddSingleton(new ConcurrentDictionary<Guid, JobStatus>());
	builder.Services.AddSingleton(new ConcurrentBag<JobStatus>());
	builder.Services.AddSingleton<Worker>();
	builder.Services.AddTransient<RestoreDbHandler>();
	builder.Services.AddTransient<DeployIISHandler>();
	builder.Services.AddSingleton<IAuthorizationHandler, CustomAuthorizationHandler>();
	builder.Services.AddSingleton<ConnectionStringsFileHandler>();
	builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, CustomAuthorizationMiddlewareResultHandler>();
	
}

void ConfigureSwagger(){
	//https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-8.0&tabs=visual-studio%2Cminimal-apis
	builder.Services.Configure<RouteOptions>(options => options.SetParameterPolicy<RegexInlineRouteConstraint>("regex"));
	builder.Services.AddEndpointsApiExplorer();
	builder.Services.AddSwaggerGen(options=> {
		options.SwaggerDoc("v1", new OpenApiInfo { Title = "clioAgent API", Version = "v1" });
	});
}


[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Data annotations validation is safe for this scenario")]
void ConfigureObjectValidators(){
	builder.Services.AddOptions<DeploySiteHandlerArgs>().ValidateDataAnnotations();
	builder.Services.AddSingleton<IValidateOptions<DeploySiteHandlerArgs>, DeploySiteHandlerArgsValidator>();
	
	builder.Services.AddOptions<BaseJobValidator<IHandler>>().ValidateDataAnnotations();
	builder.Services.AddSingleton<IValidateOptions<BaseJob<IHandler>>, BaseJobValidator<IHandler>>();
	
	builder.Services.AddOptions<ConnectionStringsFileHandlerArgs>().ValidateDataAnnotations();
	builder.Services.AddSingleton<IValidateOptions<ConnectionStringsFileHandlerArgs>, ConnectionStringsFileHandlerArgsValidator>();
}


Settings ConfigureSettings(){
	string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

	Console.WriteLine($"Environment: {environment}");
	Console.WriteLine($"BaseDirectory: {AppContext.BaseDirectory}");
	Console.WriteLine($"CurrentDirectory: {Directory.GetCurrentDirectory()}");
	
	IConfigurationRoot configuration = new ConfigurationBuilder()
		// .SetBasePath(Directory.GetCurrentDirectory())
		.SetBasePath(AppContext.BaseDirectory)
		.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
		.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
		.Build();
	builder.Configuration.AddConfiguration(configuration);
	CreatioProducts[]? creatioProducts = configuration.GetSection("CreatioProducts").Get<CreatioProducts[]>();
	var db = configuration.GetSection("Db").Get<Db[]>();
	var workingDirectoryPath = configuration.GetSection("WorkingDirectoryPath").Get<string>();
	var traceServer = configuration.GetSection("TraceServer").Get<TraceServer>();
	Settings settings = new (creatioProducts, db, workingDirectoryPath ?? defaultWorkingDirectoryPath, traceServer);
	builder.Services.AddSingleton(settings);
	return settings;
}

void StartWorkerThread(){
	int coreCount = Environment.ProcessorCount;
	for (int i = 0; i < coreCount; i++) {
		new Thread(() => {
			Worker worker = app.Services.GetRequiredService<Worker>();
			worker.Run(cancellationTokenSource.Token);
		}).Start();
	}
}

public record StepStatus(Guid JobId, string? Message, DateTime Date, string CurrentStatus, Guid? StepId);