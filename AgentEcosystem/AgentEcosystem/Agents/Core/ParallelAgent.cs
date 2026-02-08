using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentEcosystem.Agents.Core;

/// <summary>
/// ADK'nın ParallelAgent'ının .NET uyarlaması.
/// Alt ajanları eşzamanlı (paralel) olarak çalıştırır.
/// Her alt ajan aynı AgentContext'i paylaşır; çakışmayı önlemek için
/// farklı state key'leri kullanılmalıdır.
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

            // Her alt ajanın state güncellemelerini uygula
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
