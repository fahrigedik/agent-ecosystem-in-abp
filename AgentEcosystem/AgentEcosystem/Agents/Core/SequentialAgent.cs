using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentEcosystem.Agents.Core;

/// <summary>
/// .NET adaptation of ADK's SequentialAgent.
/// Runs sub-agents sequentially in a defined order.
/// One agent's output (via state) becomes the next agent's input.
/// </summary>
public class SequentialAgent : BaseAgent
{
    public override async Task<AgentEvent> RunAsync(
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        AgentEvent lastEvent = new() { Author = Name, Status = "working" };

        foreach (var subAgent in SubAgents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lastEvent = await subAgent.RunAsync(context, cancellationToken);
            context.Events.Add(lastEvent);

            // Apply state updates
            if (lastEvent.Actions?.StateUpdates != null)
            {
                foreach (var (key, value) in lastEvent.Actions.StateUpdates)
                {
                    context.SetState(key, value);
                }
            }

            // Escalate check — early exit (e.g. signal from LoopAgent)
            if (lastEvent.Actions?.Escalate == true)
                break;

            // Transfer check — redirect to another agent
            if (!string.IsNullOrEmpty(lastEvent.Actions?.TransferToAgent))
            {
                var targetAgent = FindAgent(lastEvent.Actions.TransferToAgent);
                if (targetAgent != null)
                {
                    lastEvent = await targetAgent.RunAsync(context, cancellationToken);
                    context.Events.Add(lastEvent);
                }
            }
        }

        return new AgentEvent
        {
            Author = Name,
            Status = "completed",
            Content = lastEvent.Content
        };
    }
}
