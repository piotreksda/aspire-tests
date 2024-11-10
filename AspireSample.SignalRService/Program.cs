using IntegrationCommands;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// Add SignalR services
builder.Services.AddSignalR();

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<IntegrationCommandConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("MessageBus");
        
        // Configure the RabbitMQ host using the connection string
        cfg.Host(connectionString);
        
        // Define the message consumer for IntegrationCommand
        cfg.ReceiveEndpoint("integration-command-queue", e =>
        {
            e.Consumer<IntegrationCommandConsumer>(context);
        });
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Map SignalR hub
app.MapHub<NotificationHub>("/notifications");

app.Run();

// Consumer for handling integration commands
public class IntegrationCommandConsumer : IConsumer<NotifySignalR>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<IntegrationCommandConsumer> _logger;

    public IntegrationCommandConsumer(IHubContext<NotificationHub> hubContext, ILogger<IntegrationCommandConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<NotifySignalR> context)
    {
        // Log that we are about to send a message to all connected clients
        _logger.LogInformation("Attempting to send message to all connected SignalR clients.");

        // Send message to all connected SignalR clients
        await _hubContext.Clients.All.SendAsync("ReceiveMessage", context.Message.Content);

        _logger.LogInformation("Message sent to all clients: {MessageContent}", context.Message.Content);
    }
}

// SignalR hub
public sealed class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        // Log when a new client connects
        _logger.LogInformation("Client connected with connection ID: {ConnectionId}", Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Log when a client disconnects
        _logger.LogInformation("Client disconnected with connection ID: {ConnectionId}", Context.ConnectionId);

        // Log the exception if the disconnection was due to an error
        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected due to an error.");
        }

        await base.OnDisconnectedAsync(exception);
    }
}