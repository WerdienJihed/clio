using Clio.Command.WebServer;
using Clio.UserEnvironment;
using CommandLine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using HttpVersion = System.Net.HttpVersion;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Clio.Command;

[Verb("start-server", Aliases = new[] { "ss" }, HelpText = "Start web server")]
public class StartServerOptions : EnvironmentNameOptions
{

    #region Properties: Public

    [Option("Port", Required = false, HelpText = "Default server port", Default = 19_999)]
    public int Port { get; set; }

    #endregion

}

public class StartServerCommand : Command<StartServerOptions>
{

    #region Fields: Private

    private readonly ISettingsRepository _settingsRepository;
    private readonly Microsoft.AspNetCore.Builder.WebApplication _app;

    #endregion

    #region Constructors: Public

    /// <summary>
    ///     Initializes a new instance of the <see cref="StartServerCommand" /> class.
    /// </summary>
    /// <param name="settingsRepository">An instance of <see cref="ISettingsRepository" /> to manage environment settings.</param>
    /// <remarks>
    ///     <list type="bullet">
    ///         This constructor sets up a web server with specific configurations:
    ///         <item>1. Initializes a new `WebApplicationBuilder` instance.</item>
    ///         <item>2. Configures a CORS policy that allows any origin, method, and header.</item>
    ///         <item>3. Registers `ISettingsRepository` and `ITokenManager` as singleton services.</item>
    ///         <item>4. Configures an `HttpClient` named "IdentityClient" with specific version settings.</item>
    ///         <item>5. Registers `TokenMessageHandler` and `LimiterHandler` as transient services.</item>
    ///         <item>
    ///             6. Configures another `HttpClient` named "CreatioClient" with specific version settings, a timeout,
    ///             message handlers, a retry policy, and a primary message handler.
    ///         </item>
    ///         <item>7. Builds the web application and stores it in the `_app` field.</item>
    ///         <item>8. Applies the configured CORS policy.</item>
    ///         <item>
    ///             9. Maps a proxy endpoint that handles GET requests and proxies them to another server. The proxying logic
    ///             is handled by the `ProxyHandlerHandler` method.
    ///         </item>
    ///     </list>
    /// </remarks>
    public StartServerCommand(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;

        WebApplicationBuilder builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy",
                policyBuilder => { policyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
        });
        builder.Services.AddSingleton(_settingsRepository);
        builder.Services.AddSingleton<ITokenManager, TokenManager>();
        builder.Services.AddHttpClient("IdentityClient", client =>
        {
            client.DefaultRequestVersion = HttpVersion.Version20;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        });

        builder.Services.AddTransient<TokenMessageHandler>();
        builder.Services.AddTransient<LimiterHandler>();

        builder.Services.AddHttpClient("CreatioClient", client =>
        {
            client.DefaultRequestVersion = HttpVersion.Version20;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            client.Timeout = TimeSpan.FromSeconds(1_000);
        })
            .AddHttpMessageHandler<LimiterHandler>()
            .AddHttpMessageHandler<TokenMessageHandler>()
            .AddPolicyHandler(GetRetryPolicy())
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseDefaultCredentials = false
            });

        _app = builder.Build();
        _app.UseCors("CorsPolicy");
        _app.MapMethods(
            "/proxy/{env}/{*proxyString}",
            new[] {
                HttpMethods.Get
				// HttpMethods.Delete,
				// HttpMethods.Head,
				// HttpMethods.Options,
				// HttpMethods.Trace,
				// HttpMethods.Connect
			},
            async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string env,
                    string proxyString) =>
                await ProxyHandlerHandler(httpClientFactory, context, env, proxyString)
        );
    }

    #endregion

    #region Methods: Private

    /// <summary>
    ///     Constructs and returns a retry policy. This policy defines the conditions under which a failed operation
    ///     should be retried, the maximum number of retry attempts, and the delay between retry attempts.
    /// </summary>
    /// <returns>A RetryPolicy object that encapsulates the retry conditions and behavior.</returns>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // Handles HttpRequestException, 5XX and 408
            .Or<SocketException>() // Handles SocketException
            .WaitAndRetryAsync(5, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (outcome, timespan, retryAttempt, context) =>
                {
                    // This is a good place to log the details of the retry
                    Console.WriteLine(
                        $"Retrying due to: {outcome.Exception?.Message ?? "No exception message."}. Retry attempt: {retryAttempt}. Waiting {timespan} before next retry.");
                });
    }

    /// <summary>
    ///     Acts as a proxy for HTTP requests. It retrieves the environment settings based on the provided environment name,
    ///     constructs a new URI from the environment settings, and creates an HttpClient instance with its BaseAddress set to
    ///     the new URI.
    ///     It then creates a new HttpRequestMessage, populates it with details from the incoming HttpContext, and sends it
    ///     using the HttpClient. The method modifies the response headers and content before returning it.
    /// </summary>
    /// <param name="httpClientFactory"></param>
    /// <param name="context">HTTP Context</param>
    /// <param name="env">Environment Name where we take base url from</param>
    /// <param name="proxyString">Partial URL of the destination</param>
    /// <returns>
    ///     A Task that represents the asynchronous operation. The task result contains the IResult representing the result
    ///     of the operation.
    /// </returns>
    /// <remarks>
    ///     See <see cref="LimiterHandler" /> to understand how $top= is added to the unbound requests
    /// </remarks>
    private async Task<IResult> ProxyHandlerHandler(IHttpClientFactory httpClientFactory, HttpContext context,
        string env, string proxyString)
    {
        EnvironmentSettings environment = _settingsRepository.GetEnvironment(env);
        bool isUri = Uri.TryCreate(environment.Uri, UriKind.Absolute, out Uri baseUri);
        if (!isUri)
        {
            return Results.BadRequest(new
            {
                Error = $"Environment with key {env} does not have correct Uri: {environment.Uri}",
                EnvironmentName = env
            });
        }
        HttpClient client = httpClientFactory.CreateClient("CreatioClient");
        client.BaseAddress = baseUri;

        HttpRequestMessage httpRequestMessage = new();
        HttpRequestOptionsKey<string> environmentName = new("environment-name");
        httpRequestMessage.Options.Set(environmentName, env);
        Uri.TryCreate(baseUri, proxyString + context.Request.QueryString.Value, out Uri requestUri);
        httpRequestMessage.RequestUri = requestUri;
        httpRequestMessage.Method = new HttpMethod(context.Request.Method);
        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

        //OData specific stuff
        context.Response.Headers.Add("OData-Version", "4.0");
        context.Response.Headers.Add("Pragma", "no-cache");
        context.Response.Headers.Add("Cache-Control", "no-cache");
        context.Response.Headers.Add("Expires", "-1");
        context.Response.Headers.Add("Prefer", "odata.maxpagesize=100");

        string stringContent = await response.Content.ReadAsStringAsync();
        string returnContent = stringContent.Replace(environment.Uri!, $"http://127.0.0.1:19999/proxy/{env}");

        return response.StatusCode switch
        {
            HttpStatusCode.NoContent => Results.NoContent(),
            _ => Results.Content(returnContent, response.Content.Headers.ContentType?.ToString(), Encoding.UTF8)
        };
    }

    #endregion

    #region Methods: Public

    public override int Execute(StartServerOptions options)
    {
        _app.Urls.Add($"http://*:{options.Port}");
        _app.Run();
        return 0;
    }

    #endregion


}