using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AgentEcosystem.Agents.Core;

/// <summary>
/// ADK'nın BaseAgent kavramının .NET uyarlaması.
/// Tüm ajanlar bu sınıftan türer.
/// </summary>
public abstract class BaseAgent
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BaseAgent? ParentAgent { get; set; }
    public List<BaseAgent> SubAgents { get; set; } = new();

    /// <summary>
    /// Ajanı çalıştırır. ADK'daki _run_async_impl'in karşılığı.
    /// </summary>
    public abstract Task<AgentEvent> RunAsync(
        AgentContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Agent hiyerarşisinde isimle ajan bulma. ADK'daki find_agent.
    /// </summary>
    public BaseAgent? FindAgent(string name)
    {
        if (Name == name) return this;
        foreach (var sub in SubAgents)
        {
            var found = sub.FindAgent(name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Alt ajan ekler ve ebeveyn ilişkisini kurar.
    /// </summary>
    public void AddSubAgent(BaseAgent agent)
    {
        if (agent.ParentAgent != null)
            throw new InvalidOperationException(
                $"Agent '{agent.Name}' zaten bir ebeveyne sahip: '{agent.ParentAgent.Name}'");
        
        agent.ParentAgent = this;
        SubAgents.Add(agent);
    }
}
