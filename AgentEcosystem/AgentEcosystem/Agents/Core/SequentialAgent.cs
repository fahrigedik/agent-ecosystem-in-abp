using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentEcosystem.Agents.Core;

/// <summary>
/// ADK'nın SequentialAgent'ının .NET uyarlaması.
/// Alt ajanları tanımlı sırada ardışık olarak çalıştırır.
/// Bir ajanın çıktısı (state üzerinden) bir sonrakinin girdisi olur.
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

            // State güncellemelerini uygula
            if (lastEvent.Actions?.StateUpdates != null)
            {
                foreach (var (key, value) in lastEvent.Actions.StateUpdates)
                {
                    context.SetState(key, value);
                }
            }

            // Escalate kontrolü — erken çıkış (LoopAgent'tan gelen sinyal gibi)
            if (lastEvent.Actions?.Escalate == true)
                break;

            // Transfer kontrolü — başka ajana yönlendirme
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
