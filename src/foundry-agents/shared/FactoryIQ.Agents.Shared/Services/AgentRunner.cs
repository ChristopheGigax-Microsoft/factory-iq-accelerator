using System.Text;
using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Shared.Services;

/// <summary>
/// Runs a persistent Foundry agent thread, including polling, tool dispatch, and final message extraction.
/// </summary>
public sealed class AgentRunner(PersistentAgentsClient client, ILogger<AgentRunner> logger)
{
    public async Task<string> RunAsync(
        PersistentAgent agent,
        string userQuery,
        Func<RequiredFunctionToolCall, CancellationToken, Task<string>> toolCallHandler,
        CancellationToken ct = default)
    {
        PersistentAgentThread thread = await client.Threads.CreateThreadAsync(
            messages: null,
            toolResources: null,
            metadata: null,
            cancellationToken: ct);

        await client.Messages.CreateMessageAsync(
            thread.Id,
            MessageRole.User,
            userQuery,
            attachments: null,
            metadata: null,
            cancellationToken: ct);

        ThreadRun run = await client.Runs.CreateRunAsync(
            thread.Id,
            agent.Id,
            overrideModelName: null,
            overrideInstructions: null,
            additionalInstructions: null,
            additionalMessages: null,
            overrideTools: null,
            stream: null,
            temperature: null,
            topP: null,
            maxPromptTokens: null,
            maxCompletionTokens: null,
            truncationStrategy: null,
            toolChoice: null,
            responseFormat: null,
            parallelToolCalls: true,
            metadata: null,
            include: null,
            cancellationToken: ct);

        run = await WaitForTerminalStateAsync(run, toolCallHandler, ct);
        return await GetFinalResponseAsync(run, ct);
    }

    private async Task<ThreadRun> WaitForTerminalStateAsync(
        ThreadRun run,
        Func<RequiredFunctionToolCall, CancellationToken, Task<string>> toolCallHandler,
        CancellationToken ct)
    {
        var delay = TimeSpan.FromMilliseconds(500);

        while (run.Status == RunStatus.Queued
            || run.Status == RunStatus.InProgress
            || run.Status == RunStatus.RequiresAction)
        {
            if (run.Status == RunStatus.RequiresAction)
            {
                run = await HandleRequiredActionAsync(run, toolCallHandler, ct);
                delay = TimeSpan.FromMilliseconds(500);
                continue;
            }

            await Task.Delay(delay, ct);
            delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 1.5, 5000));
            run = await client.Runs.GetRunAsync(run.ThreadId, run.Id, ct);
        }

        if (run.Status != RunStatus.Completed)
        {
            var errorMessage = run.LastError?.Message ?? $"Run ended with status {run.Status}.";
            throw new InvalidOperationException(errorMessage);
        }

        return run;
    }

    private async Task<ThreadRun> HandleRequiredActionAsync(
        ThreadRun run,
        Func<RequiredFunctionToolCall, CancellationToken, Task<string>> toolCallHandler,
        CancellationToken ct)
    {
        if (run.RequiredAction is not SubmitToolOutputsAction submitToolOutputsAction)
        {
            throw new InvalidOperationException(
                $"Unsupported required action type: {run.RequiredAction?.GetType().Name ?? "unknown"}");
        }

        List<ToolOutput> toolOutputs = [];

        foreach (RequiredToolCall toolCall in submitToolOutputsAction.ToolCalls)
        {
            if (toolCall is not RequiredFunctionToolCall functionToolCall)
            {
                throw new InvalidOperationException($"Unsupported tool call type: {toolCall.GetType().Name}");
            }

            logger.LogInformation("Executing tool {ToolName} for run {RunId}", functionToolCall.Name, run.Id);
            string output = await toolCallHandler(functionToolCall, ct);
            toolOutputs.Add(new ToolOutput(functionToolCall, output));
        }

        return await client.Runs.SubmitToolOutputsToRunAsync(run, toolOutputs, ct);
    }

    private async Task<string> GetFinalResponseAsync(ThreadRun run, CancellationToken ct)
    {
        List<string> responses = [];

        await foreach (PersistentThreadMessage message in client.Messages.GetMessagesAsync(
            run.ThreadId,
            run.Id,
            limit: 50,
            order: null,
            after: null,
            before: null,
            cancellationToken: ct))
        {
            string content = ExtractText(message);
            if (!string.IsNullOrWhiteSpace(content))
            {
                responses.Add(content);
            }
        }

        if (responses.Count == 0)
        {
            return "Run completed, but no textual response was returned.";
        }

        responses.Reverse();
        return string.Join(Environment.NewLine + Environment.NewLine, responses);
    }

    private static string ExtractText(PersistentThreadMessage message)
    {
        StringBuilder builder = new();

        foreach (MessageContent contentItem in message.ContentItems)
        {
            if (contentItem is MessageTextContent textItem && !string.IsNullOrWhiteSpace(textItem.Text))
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(textItem.Text);
            }
        }

        return builder.ToString().Trim();
    }
}
