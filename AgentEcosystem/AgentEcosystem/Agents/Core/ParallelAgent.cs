using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentEcosystem.Agents.Core;

/// <summary>
/// .NET adaptation of ADK's ParallelAgent.
/// Runs sub-agents concurrently (in parallel).
/// Each sub-agent shares the same AgentContext; different state keys
/// should be used to avoid conflicts.
/// </summary>
public class ParallelAgent : BaseAgent
{
    public override async Task<AgentEvent> RunAsync(
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        var tasks = SubAgents.Select(agent =>
            agent.RunAsync(context, cancellationToken));

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            context.Events.Add(result);

            // Apply state updates from each sub-agent
            if (result.Actions?.StateUpdates != null)
            {
                foreach (var (key, value) in result.Actions.StateUpdates)
                {
                    context.SetState(key, value);
                }
            }
        }

        return new AgentEvent
        {
            Author = Name,
            Status = "completed",
            Content = string.Join("\n\n---\n\n",
                results.Select(r => $"[{r.Author}]: {r.Content}"))
        };
    }
}
