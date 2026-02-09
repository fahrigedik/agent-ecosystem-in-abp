using System;
using System.Collections.Generic;

namespace AgentEcosystem.Services.Dtos;

/// <summary>
/// Research request DTO.
/// Data model for the research request coming to the API.
/// </summary>
public class ResearchRequestDto
{
    /// <summary>
    /// Research query.
    /// E.g.: "What are the new features in the latest version of Python?"
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Execution mode: "sequential" (ADK) or "a2a" (A2A protocol).
    /// Default: sequential
    /// </summary>
    public string Mode { get; set; } = "sequential";
}

/// <summary>
/// Research result DTO.
/// Contains the full pipeline output.
/// </summary>
public class ResearchResultDto
{
    /// <summary>Session identifier.</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Original research query.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>Raw web search results.</summary>
    public string RawSearchResults { get; set; } = string.Empty;

    /// <summary>Raw research report produced by the Researcher Agent.</summary>
    public string ResearchReport { get; set; } = string.Empty;

    /// <summary>Final analysis report produced by the Analysis Agent.</summary>
    public string AnalysisResult { get; set; } = string.Empty;

    /// <summary>File name where the report was saved.</summary>
    public string SavedFileName { get; set; } = string.Empty;

    /// <summary>Processing status: Completed, Failed.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Total processing time (ms).</summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>Agent events generated during the pipeline.</summary>
    public List<AgentEventDto> AgentEvents { get; set; } = new();
}

/// <summary>
/// Agent event DTO.
/// Represents events produced by each agent in the pipeline.
/// </summary>
public class AgentEventDto
{
    /// <summary>Name of the agent that produced the event.</summary>
    public string Agent { get; set; } = string.Empty;

    /// <summary>Event status: working, completed, failed.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Event timestamp.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Content preview (max 200 characters).</summary>
    public string ContentPreview { get; set; } = string.Empty;
}

/// <summary>
/// Research summary DTO (past research records).
/// </summary>
public class ResearchSummaryDto
{
    public Guid Id { get; set; }
    public string Query { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public long? ProcessingTimeMs { get; set; }
    public string ResultPreview { get; set; } = string.Empty;
}
