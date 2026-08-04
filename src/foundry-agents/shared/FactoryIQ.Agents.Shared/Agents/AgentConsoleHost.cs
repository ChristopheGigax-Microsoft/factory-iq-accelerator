using FactoryIQ.Agents.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Shared.Agents;

public static class AgentConsoleHost
{
    public static async Task RunAsync(
        IFactoryAgent agent,
        FoundryConfig config,
        ILogger logger,
        string[] args,
        CancellationToken ct = default)
    {
        await agent.RegisterAsync(ct);

        try
        {
            if (args.Length > 0)
            {
                string query = string.Join(' ', args);
                string response = await agent.RunAsync(query, ct);
                Console.WriteLine(response);
                return;
            }

            logger.LogInformation("Interactive mode started for {AgentName}. Type 'exit' or 'quit' to stop.", agent.Name);

            while (true)
            {
                Console.Write($"{agent.Name}> ");
                string? input = Console.ReadLine();

                if (input is null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(input, "quit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                string response = await agent.RunAsync(input, ct);
                Console.WriteLine();
                Console.WriteLine(response);
                Console.WriteLine();
            }
        }
        finally
        {
            if (config.DeletePersistentAgentOnExit)
            {
                await agent.DeleteAsync(ct);
            }
            else if (agent.IsLocal)
            {
                logger.LogInformation("Local Factory IQ agent {AgentName} session finished.", agent.Name);
            }
            else
            {
                logger.LogInformation("Leaving Foundry agent {AgentName} registered in Foundry Agent Service.", agent.Name);
            }
        }
    }
}
