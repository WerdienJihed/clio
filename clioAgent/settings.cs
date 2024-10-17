namespace clioAgent;


public record Settings(
    CreatioProducts[] CreatioProducts,
    Db[] Db
);

public record Logging(
    LogLevel LogLevel
);

public record CreatioProducts(
    string Path
);

public record Db(
    string Type,
    Servers[] Servers
);

public record Servers(
    string Name,
    string ConnectionString
);

