using IntegrationCommands;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddLogging();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("MessageBus");
        
        cfg.Host(connectionString);
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapPost("/send-command", async ([FromServices] IBus bus, [FromServices] ILogger<Program> logger, string content) =>
{
    logger.LogInformation("Sending IntegrationCommand with content: {Content}", content);
    
    var integrationCommand = new NotifySignalR(content);

    await bus.Publish(integrationCommand);

    return Results.Ok("IntegrationCommand sent successfully");
});

app.Run();
