using System.Text;
using System.Text.Json;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;

namespace Isa95DataGenerator.Services;

public interface IIoTHubService
{
    Task SendMessageAsync<T>(T payload, CancellationToken cancellationToken = default);
}

public class IoTHubService : IIoTHubService, IAsyncDisposable
{
    private readonly DeviceClient _deviceClient;
    private readonly ILogger<IoTHubService> _logger;

    public IoTHubService(string connectionString, ILogger<IoTHubService> logger)
    {
        _logger = logger;
        _deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
        _logger.LogInformation("IoT Hub device client initialized");
    }

    public async Task SendMessageAsync<T>(T payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload);
        using var message = new Message(Encoding.UTF8.GetBytes(json))
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8"
        };

        await _deviceClient.SendEventAsync(message, cancellationToken);
        _logger.LogDebug("→ IoT Hub: {message}", json);
    }

    public async ValueTask DisposeAsync()
    {
        await _deviceClient.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
