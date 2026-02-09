using System;
using System.Collections.Generic;

namespace AgentEcosystem.Agents.Core;

/// <summary>
/// .NET adaptation of ADK's InvocationContext concept.
/// Carries the agent execution context — session, state, and event history.
/// </summary>
public class AgentContext
{
    /// <summary>
    /// Unique session identifier.
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The user's original query.
    /// </summary>
    public string UserQuery { get; set; } = string.Empty;

    /// <summary>
    /// Shared state store. Used for data sharing between agents.
    /// Corresponds to session.state in ADK.
    /// </summary>
    public Dictionary<string, object> State { get; set; } = new();

    /// <summary>
    /// List of events that occurred during the session.
    /// </summary>
    public List<AgentEvent> Events { get; set; } = new();

    /// <summary>
    /// Reads a typed value from state.
    /// </summary>
    public T? GetState<T>(string key)
    {
        return State.TryGetValue(key, out var value) && value is T typed
            ? typed
            : default;
    }

    /// <summary>
    /// Writes a value to state.
    /// </summary>
    public void SetState(string key, object value)
    {
        State[key] = value;
    }

    /// <summary>
    /// Checks whether the specified key exists in state.
    /// </summary>
    public bool HasState(string key)
    {
        return State.ContainsKey(key);
    }
}
