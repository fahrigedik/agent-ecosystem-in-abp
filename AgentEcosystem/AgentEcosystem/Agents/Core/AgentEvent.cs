using System;
using System.Collections.Generic;

namespace AgentEcosystem.Agents.Core;

/// <summary>
/// .NET adaptation of ADK's Event concept.
/// Represents events that occur during a session.
/// </summary>
public class AgentEvent
{
    /// <summary>
    /// Name of the agent that created the event.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Event status: working, completed, failed.
    /// </summary>
    public string Status { get; set; } = "working";

    /// <summary>
    /// Event content — text produced by the agent.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Actions associated with the event (escalate, transfer, state updates).
    /// </summary>
    public EventActions? Actions { get; set; }

    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// .NET adaptation of ADK's EventActions concept.
/// Defines actions to be performed along with the event.
/// </summary>
public class EventActions
{
    /// <summary>
    /// If true, escalates to the upstream (parent) agent.
    /// Used to terminate loops in loop agents.
    /// </summary>
    public bool Escalate { get; set; }

    /// <summary>
    /// Transfer to the specified agent. Used for LLM-driven delegation.
    /// </summary>
    public string? TransferToAgent { get; set; }

    /// <summary>
    /// Updates to be applied to state.
    /// </summary>
    public Dictionary<string, object>? StateUpdates { get; set; }
}
