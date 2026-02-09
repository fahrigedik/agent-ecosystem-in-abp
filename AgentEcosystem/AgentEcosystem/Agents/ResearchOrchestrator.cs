using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentEcosystem.A2A;
using AgentEcosystem.Agents.Core;
using AgentEcosystem.Services.Dtos;
using Microsoft.Extensions.Logging;

// A2A NuGet package namespace aliases (to avoid collision with project namespace)
using A2AAgentTask = global::A2A.AgentTask;
using A2AAgentMessage = global::A2A.AgentMessage;
using A2AMessageRole = global::A2A.MessageRole;
using A2ATextPart = global::A2A.TextPart;
using A2AArtifact = global::A2A.Artifact;
using A2APart = global::A2A.Part;

namespace AgentEcosystem.Agents;

/// <summary>
/// Research Orchestrator — The central component that coordinates the entire system.
/// 
/// This class uses three protocols together:
/// 
/// 1. ADK (Agent Development Kit) Pattern:
///    - Runs agents sequentially with SequentialAgent
///    - Manages state with AgentContext
///    - Provides event-based communication with AgentEvent
/// 
/// 2. A2A (Agent-to-Agent) Protocol:
///    - Sending tasks between agents (tasks/send)
///    - Discovery via Agent Card
///    - Task lifecycle management
/// 
/// 3. MCP (Model Context Protocol):
///    - Web search tool (ResearcherAgent)
///    - File saving tool (AnalysisAgent)
///    - Database tool (AnalysisAgent)
/// 
/// Flow Diagram:
/// ┌─────────────┐     ┌──────────────────┐     ┌──────────────┐
/// │  User        │────▶│ ResearcherAgent   │────▶│ AnalysisAgent │
/// │  Query       │     │ (MCP:WebSearch)   │ A2A │ (MCP:File+DB) │
/// └─────────────┘     └──────────────────┘     └──────────────┘
///                              │                        │
///                              ▼                        ▼
///                     ┌──────────────┐         ┌──────────────┐
///                     │ Raw Data      │         │ Final Report  │
///                     │ (State)       │         │ (File + DB)   │
///                     └──────────────┘         └──────────────┘
/// </summary>
public class ResearchOrchestrator
{
    private readonly ResearcherAgent _researcherAgent;
    private readonly AnalysisAgent _analysisAgent;
    private readonly A2AServer _a2aServer;
    private readonly ILogger<ResearchOrchestrator> _logger;

    public ResearchOrchestrator(
        ResearcherAgent researcherAgent,
        AnalysisAgent analysisAgent,
        A2AServer a2aServer,
        ILogger<ResearchOrchestrator> logger)
    {
        _researcherAgent = researcherAgent;
        _analysisAgent = analysisAgent;
        _a2aServer = a2aServer;
        _logger = logger;

        // Register A2A task handlers
        RegisterA2AHandlers();
    }

    /// <summary>
    /// Registers A2A task handlers.
    /// Each agent registers its own A2A handler with the server.
    /// </summary>
    private void RegisterA2AHandlers()
    {
        _a2aServer.RegisterTaskHandler("researcher", async (task, ct) =>
        {
            var context = new AgentContext
            {
                UserQuery = ExtractQueryFromTask(task)
            };

            var result = await _researcherAgent.RunAsync(context, ct);

            task.Artifacts = new List<A2AArtifact>
            {
                new()
                {
                    ArtifactId = Guid.NewGuid().ToString(),
                    Name = "research_report",
                    Parts = new List<A2APart>
                    {
                        new A2ATextPart { Text = result.Content }
                    }
                }
            };

            return task;
        });

        _a2aServer.RegisterTaskHandler("analyst", async (task, ct) =>
        {
            var context = new AgentContext
            {
                UserQuery = ExtractQueryFromTask(task)
            };

            // Transfer raw data from task messages to context state
            var rawData = ExtractDataFromTask(task);
            if (!string.IsNullOrEmpty(rawData))
            {
                context.SetState("research_report", rawData);
                context.SetState("search_results", rawData);
                context.SetState("research_query", context.UserQuery);
            }

            var result = await _analysisAgent.RunAsync(context, ct);

            task.Artifacts = new List<A2AArtifact>
            {
                new()
                {
                    ArtifactId = Guid.NewGuid().ToString(),
                    Name = "analysis_report",
                    Parts = new List<A2APart>
                    {
                        new A2ATextPart { Text = result.Content }
                    }
                }
            };

            return task;
        });

        _logger.LogInformation("[Orchestrator] A2A task handlers registered.");
    }

    /// <summary>
    /// Runs the full research pipeline.
    /// ADK SequentialAgent pattern + A2A communication + MCP tools.
    /// </summary>
    public async Task<ResearchResultDto> ExecuteResearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var sessionId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "[Orchestrator] ===== Research starting =====\n" +
            "  Session: {SessionId}\n  Query: '{Query}'",
            sessionId, query);

        try
        {
            // ───────────────────────────────────────────────
            // METHOD 1: ADK SequentialAgent Pattern
            // ───────────────────────────────────────────────
            // Run two agents sequentially. The first agent's output
            // (via state) becomes the second agent's input.

            var pipeline = new SequentialAgent
            {
                Name = "ResearchPipeline",
                Description = "Research → Analysis sequential pipeline"
            };

            pipeline.AddSubAgent(_researcherAgent);
            pipeline.AddSubAgent(_analysisAgent);

            var context = new AgentContext
            {
                SessionId = sessionId,
                UserQuery = query
            };

            _logger.LogInformation("[Orchestrator] Starting ADK SequentialAgent pipeline...");
            var finalEvent = await pipeline.RunAsync(context, cancellationToken);

            stopwatch.Stop();

            // ───────────────────────────────────────────────
            // BUILD RESULT DTO
            // ───────────────────────────────────────────────
            var result = new ResearchResultDto
            {
                SessionId = sessionId,
                Query = query,
                RawSearchResults = context.GetState<string>("search_results") ?? "",
                ResearchReport = context.GetState<string>("research_report") ?? "",
                AnalysisResult = context.GetState<string>("analysis_result") ?? finalEvent.Content,
                SavedFileName = context.GetState<string>("analysis_file") ?? "",
                Status = finalEvent.Status == "completed" ? "Completed" : "Failed",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                AgentEvents = context.Events.Select(e => new AgentEventDto
                {
                    Agent = e.Author,
                    Status = e.Status,
                    Timestamp = e.Timestamp,
                    ContentPreview = e.Content.Length > 200
                        ? e.Content[..200] + "..."
                        : e.Content
                }).ToList()
            };

            _logger.LogInformation(
                "[Orchestrator] ===== Research completed =====\n" +
                "  Session: {SessionId}\n  Duration: {ElapsedMs}ms\n  Status: {Status}",
                sessionId, stopwatch.ElapsedMilliseconds, result.Status);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "[Orchestrator] Research pipeline error: {SessionId}", sessionId);

            return new ResearchResultDto
            {
                SessionId = sessionId,
                Query = query,
                Status = "Failed",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                AnalysisResult = $"An error occurred during research: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Runs research via the A2A protocol.
    /// Sends tasks between agents and retrieves results.
    /// </summary>
    public async Task<ResearchResultDto> ExecuteResearchViaA2AAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var sessionId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "[Orchestrator:A2A] Research starting (A2A Mode)\n" +
            "  Session: {SessionId}\n  Query: '{Query}'",
            sessionId, query);

        try
        {
            // ───────────────────────────────────────────────
            // STEP 1: Send A2A Task to Researcher Agent
            // ───────────────────────────────────────────────
            var researchTask = new A2AAgentTask
            {
                Id = Guid.NewGuid().ToString(),
                ContextId = sessionId,
                History = new List<A2AAgentMessage>
                {
                    new()
                    {
                        Role = A2AMessageRole.User,
                        MessageId = Guid.NewGuid().ToString(),
                        Parts = new List<A2APart>
                        {
                            new A2ATextPart { Text = query }
                        }
                    }
                }
            };

            _logger.LogInformation("[Orchestrator:A2A] → Sending task to Researcher Agent...");
            var researchResult = await _a2aServer.HandleTaskAsync(
                "researcher", researchTask, cancellationToken);

            var researchReport = ExtractTextFromArtifacts(researchResult);

            // ───────────────────────────────────────────────
            // STEP 2: Send A2A Task to Analysis Agent
            // ───────────────────────────────────────────────
            var analysisTask = new A2AAgentTask
            {
                Id = Guid.NewGuid().ToString(),
                ContextId = sessionId,
                History = new List<A2AAgentMessage>
                {
                    new()
                    {
                        Role = A2AMessageRole.User,
                        MessageId = Guid.NewGuid().ToString(),
                        Parts = new List<A2APart>
                        {
                            new A2ATextPart { Text = query }
                        }
                    },
                    new()
                    {
                        Role = A2AMessageRole.Agent,
                        MessageId = Guid.NewGuid().ToString(),
                        Parts = new List<A2APart>
                        {
                            new A2ATextPart { Text = researchReport }
                        }
                    }
                }
            };

            _logger.LogInformation("[Orchestrator:A2A] → Sending task to Analysis Agent...");
            var analysisResult = await _a2aServer.HandleTaskAsync(
                "analyst", analysisTask, cancellationToken);

            var analysisReport = ExtractTextFromArtifacts(analysisResult);

            stopwatch.Stop();

            return new ResearchResultDto
            {
                SessionId = sessionId,
                Query = query,
                RawSearchResults = "",
                ResearchReport = researchReport,
                AnalysisResult = analysisReport,
                Status = "Completed",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                AgentEvents = new List<AgentEventDto>
                {
                    new()
                    {
                        Agent = "ResearcherAgent",
                        Status = researchResult.Status.State.ToString() ?? "unknown",
                        Timestamp = DateTime.UtcNow,
                        ContentPreview = Truncate(researchReport, 200)
                    },
                    new()
                    {
                        Agent = "AnalysisAgent",
                        Status = analysisResult.Status.State.ToString() ?? "unknown",
                        Timestamp = DateTime.UtcNow,
                        ContentPreview = Truncate(analysisReport, 200)
                    }
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[Orchestrator:A2A] A2A pipeline error: {SessionId}", sessionId);

            return new ResearchResultDto
            {
                SessionId = sessionId,
                Query = query,
                Status = "Failed",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                AnalysisResult = $"A2A research error: {ex.Message}"
            };
        }
    }

    #region Helper Methods

    private static string ExtractQueryFromTask(A2AAgentTask task)
    {
        var firstMessage = task.History?.FirstOrDefault(m => m.Role == A2AMessageRole.User);
        if (firstMessage?.Parts == null) return "";

        foreach (var part in firstMessage.Parts)
        {
            if (part is A2ATextPart textPart)
                return textPart.Text;
        }
        return "";
    }

    private static string ExtractDataFromTask(A2AAgentTask task)
    {
        var agentMessage = task.History?.LastOrDefault(m => m.Role == A2AMessageRole.Agent);
        if (agentMessage?.Parts == null) return "";

        foreach (var part in agentMessage.Parts)
        {
            if (part is A2ATextPart textPart)
                return textPart.Text;
        }
        return "";
    }

    private static string ExtractTextFromArtifacts(A2AAgentTask task)
    {
        if (task.Artifacts == null || !task.Artifacts.Any())
            return "";

        foreach (var artifact in task.Artifacts)
        {
            if (artifact.Parts == null) continue;
            foreach (var part in artifact.Parts)
            {
                if (part is A2ATextPart textPart)
                    return textPart.Text;
            }
        }
        return "";
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text ?? "";
        return text[..maxLength] + "...";
    }

    #endregion
}
