var builder = DistributedApplication.CreateBuilder(args);

var platformService = builder.AddProject<Projects.PlatformService>("platformservice");

var commandsService = builder.AddProject<Projects.CommandsService>("commandsservice");

builder.Build().Run();
