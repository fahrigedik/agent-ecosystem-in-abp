using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AgentEcosystem.Agents.Core;

/// <summary>
/// .NET adaptation of ADK's BaseAgent concept.
/// All agents derive from this class.
/// </summary>
public abstract class BaseAgent
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BaseAgent? ParentAgent { get; set; }
    public List<BaseAgent> SubAgents { get; set; } = new();

    /// <summary>
    /// Runs the agent. Corresponds to _run_async_impl in ADK.
    /// </summary>
    public abstract Task<AgentEvent> RunAsync(
        AgentContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an agent by name in the agent hierarchy. Corresponds to find_agent in ADK.
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
    /// Adds a sub-agent and establishes the parent relationship.
    /// </summary>
    public void AddSubAgent(BaseAgent agent)
    {
        if (agent.ParentAgent != null)
            throw new InvalidOperationException(
                $"Agent '{agent.Name}' already has a parent: '{agent.ParentAgent.Name}'");
        
        agent.ParentAgent = this;
        SubAgents.Add(agent);
    }
}
