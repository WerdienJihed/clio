
namespace clioAgent;

public record Settings(
    CreatioProducts[]? CreatioProducts,
    Db[]? Db,
    string WorkingDirectoryPath,
    TraceServer? TraceServer
);

public record Logging(
    LogLevel LogLevel
);

public record CreatioProducts(
    string? Path
);

public record Db(
    string Type,
    //IEnumerable<Server> Servers
    Server[]? Servers
);

public record Server(
    string? Name,
    string? ConnectionString,
    string? BinFolderPath
);

public record TraceServer(
    bool? Enabled,
    Uri? UiUrl,
    Uri? CollectorUrl
);

