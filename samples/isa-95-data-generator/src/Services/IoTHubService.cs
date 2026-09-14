using System.Text;
using System.Text.Json;
using Isa95DataGenerator.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;

namespace Isa95DataGenerator.Services;

public interface IIoTHubService
{
    Task SendMessageAsync(TelemetryMessage payload, CancellationToken cancellationToken = default);
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

    public async Task SendMessageAsync(
        TelemetryMessage payload,
        CancellationToken cancellationToken = default)
    {
        var envelope = new RawTelemetryEnvelope
        {
            IngestedAt = DateTime.UtcNow,
            EventTime = payload.Timestamp,
            SourceSystem = "Isa95DataGenerator",
            SourceSchema = "isa95-demo.v1",
            SourceRecordId = Guid.NewGuid().ToString(),
            Payload = payload
        };
        var json = JsonSerializer.Serialize(envelope);
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
