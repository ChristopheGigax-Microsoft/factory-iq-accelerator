using FactoryIQ.Agents.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Shared.Agents;

public static class AgentConsoleHost
{
    public const string RegisterOnlyArgument = "--register-only";

    public static async Task RunAsync(
        IFactoryAgent agent,
        FoundryConfig config,
        ILogger logger,
        string[] args,
        CancellationToken ct = default)
    {
        bool registerOnly = IsRegisterOnly(args);

        await agent.RegisterAsync(ct);
        if (registerOnly)
        {
            logger.LogInformation("Registered {AgentName}; exiting without starting interactive mode.", agent.Name);
            return;
        }

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

    public static bool IsRegisterOnly(string[] args)
    {
        if (args.Length == 0)
        {
            return false;
        }

        bool hasRegisterOnly = args.Any(arg => string.Equals(arg, RegisterOnlyArgument, StringComparison.OrdinalIgnoreCase));
        if (!hasRegisterOnly)
        {
            return false;
        }

        if (args.Length > 1)
        {
            throw new InvalidOperationException($"{RegisterOnlyArgument} cannot be combined with a query.");
        }

        return true;
    }
}
