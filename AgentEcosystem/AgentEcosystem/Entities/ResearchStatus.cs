namespace AgentEcosystem.Entities;

/// <summary>
/// Research status enum.
/// Represents the states in the lifecycle of a research.
/// </summary>
public enum ResearchStatus
{
    /// <summary>Research has not started yet.</summary>
    Pending = 0,

    /// <summary>Researcher Agent is collecting data.</summary>
    Researching = 1,

    /// <summary>Analysis Agent is analyzing the data.</summary>
    Analyzing = 2,

    /// <summary>Research completed successfully.</summary>
    Completed = 3,

    /// <summary>An error occurred during the research.</summary>
    Failed = 4
}
