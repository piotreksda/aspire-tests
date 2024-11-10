var builder = DistributedApplication.CreateBuilder(args);

var redisBackplane = builder.AddRedis("redisBackplane");
var redisCache = builder.AddRedis("cache");
var messageBus = builder.AddRabbitMQ("messageBus");

var signalRService = builder.AddProject<Projects.AspireSample_SignalRService>("signalR")
    .WithReference(messageBus)
    .WithReference(redisBackplane)
    .WithHttpsEndpoint(name: "httpsProxies", port: 21371)
    .WithReplicas(2);
var apiService = builder.AddProject<Projects.AspireSample_ApiService>("apiservice")
    .WithReference(messageBus)
    .WithReference(redisCache)
    .WithHttpsEndpoint(name: "httpsProxies", port: 21372)
    .WithReplicas(2);

builder.Build().Run();
