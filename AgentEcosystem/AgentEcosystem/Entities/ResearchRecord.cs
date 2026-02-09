using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace AgentEcosystem.Entities;

/// <summary>
/// Research record entity.
/// Each research query and its result are stored in this table.
/// Uses ABP's FullAuditedAggregateRoot to automatically maintain
/// creation/update/deletion audit information.
/// </summary>
public class ResearchRecord : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// The research question asked by the user.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Raw data collected by the Researcher Agent.
    /// Web search results, source texts, etc.
    /// </summary>
    public string RawData { get; set; } = string.Empty;

    /// <summary>
    /// Analyzed result produced by the Analysis Agent.
    /// Structured, summarized final output.
    /// </summary>
    public string AnalyzedResult { get; set; } = string.Empty;

    /// <summary>
    /// JSON list of sources used in the research.
    /// </summary>
    public string Sources { get; set; } = string.Empty;

    /// <summary>
    /// Research status.
    /// </summary>
    public ResearchStatus Status { get; set; } = ResearchStatus.Pending;

    /// <summary>
    /// Time when the research was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Session identifier that initiated the research.
    /// Used for task tracking in the A2A protocol.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Processing time (milliseconds).
    /// </summary>
    public long? ProcessingTimeMs { get; set; }
}
