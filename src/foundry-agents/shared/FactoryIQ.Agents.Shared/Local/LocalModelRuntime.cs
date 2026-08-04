using System.Text;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using FactoryIQ.Agents.Shared.Models;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Shared.Local;

public sealed class LocalModelRuntime(
    FoundryConfig config,
    ILogger<LocalModelRuntime> logger)
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private Func<string, CancellationToken, Task<string>>? _complete;
    private Func<Task>? _unload;

    public async Task EnsureReadyAsync(CancellationToken ct = default)
    {
        if (_complete is not null)
        {
            return;
        }

        await _initializationLock.WaitAsync(ct);
        try
        {
            if (_complete is not null)
            {
                return;
            }

            Configuration localConfig = new()
            {
                AppName = "factory-iq-local",
                LogLevel = Microsoft.AI.Foundry.Local.LogLevel.Information,
            };

            await FoundryLocalManager.CreateAsync(localConfig, logger);
            FoundryLocalManager manager = FoundryLocalManager.Instance;

            var executionProviders = manager.DiscoverEps();
            if (executionProviders.Length > 0)
            {
                await manager.DownloadAndRegisterEpsAsync((_, _) => { });
            }

            var catalog = await manager.GetCatalogAsync();
            var model = await catalog.GetModelAsync(config.LocalModelDeploymentName)
                ?? throw new InvalidOperationException(
                    $"Foundry Local model '{config.LocalModelDeploymentName}' was not found in the local catalog.");

            await model.DownloadAsync(_ => { });
            await model.LoadAsync();
            var chatClient = await model.GetChatClientAsync();

            _complete = async (query, cancellationToken) =>
            {
                List<ChatMessage> messages =
                [
                    new ChatMessage
                    {
                        Role = "system",
                        Content = "Answer only from facts supplied by the user or configured local tools. Never invent machine data.",
                    },
                    new ChatMessage { Role = "user", Content = query },
                ];

                StringBuilder response = new();
                await foreach (var chunk in chatClient.CompleteChatStreamingAsync(messages, cancellationToken))
                {
                    if (chunk.Choices.Count > 0 && chunk.Choices[0].Message.Content is { } content)
                    {
                        response.Append(content);
                    }
                }

                return response.ToString();
            };

            _unload = () => model.UnloadAsync(CancellationToken.None);
            logger.LogInformation(
                "Foundry Local model {ModelAlias} is ready. Cached models are reused on subsequent starts.",
                config.LocalModelDeploymentName);
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task<string> CompleteAsync(string query, CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        return await _complete!(query, ct);
    }

    public async Task UnloadAsync()
    {
        if (_unload is not null)
        {
            await _unload();
            _unload = null;
            _complete = null;
        }
    }
}
