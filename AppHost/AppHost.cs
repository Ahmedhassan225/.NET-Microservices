var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", password: sqlPassword, port: 1433)
    .WithDataVolume();

var platformsDb = sql.AddDatabase("platformsdb", "PlatformsDB");

var rabbitmq = builder.AddRabbitMQ("rabbitmq", port: 5672)
    .WithManagementPlugin();

var platformService = builder.AddProject<Projects.PlatformService>("platformservice")
    .WithReference(platformsDb)
    .WithReference(rabbitmq)
    .WaitFor(platformsDb)
    .WaitFor(rabbitmq);

var commandsService = builder.AddProject<Projects.CommandsService>("commandsservice")
    .WithReference(rabbitmq)
    .WithReference(platformService)
    .WithEnvironment("GrpcPlatform", platformService.GetEndpoint("https"))
    .WaitFor(platformService)
    .WaitFor(rabbitmq);

builder.Build().Run();
