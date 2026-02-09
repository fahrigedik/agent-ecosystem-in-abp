using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// The A2A NuGet package (global::A2A) conflicts with the project namespace (AgentEcosystem.A2A).
// Therefore we access types from the NuGet package using global:: aliases.
using A2AAgentCard = global::A2A.AgentCard;
using A2AAgentTask = global::A2A.AgentTask;
using A2AAgentTaskStatus = global::A2A.AgentTaskStatus;
using A2AAgentMessage = global::A2A.AgentMessage;
using A2AAgentCapabilities = global::A2A.AgentCapabilities;
using A2AAgentSkill = global::A2A.AgentSkill;
using A2ATaskState = global::A2A.TaskState;
using A2AMessageRole = global::A2A.MessageRole;
using A2ATextPart = global::A2A.TextPart;
using A2AArtifact = global::A2A.Artifact;
using A2APart = global::A2A.Part;

namespace AgentEcosystem.A2A;

/// <summary>
/// A2A Server — the server side of the Agent-to-Agent protocol.
/// 
/// Google's A2A protocol enables agents to discover each other (Agent Card),
/// send tasks (Task), and receive results.
/// 
/// This class is the central component that manages incoming tasks for the A2A server.
/// Each agent publishes its own Agent Card and registers its task handler.
/// 
/// A2A Protocol Flow:
/// 1. Client → GET /.well-known/agent.json → Retrieves Agent Card
/// 2. Client → POST /tasks/send → Sends a task  
/// 3. Server → Processes task → Returns result (artifacts)
/// </summary>
public class A2AServer
{
    private readonly ILogger<A2AServer> _logger;
    private readonly Dictionary<string, Func<A2AAgentTask, CancellationToken, Task<A2AAgentTask>>> _taskHandlers = new();

    public A2AServer(ILogger<A2AServer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a task handler. Each agent registers its own handler.
    /// </summary>
    public void RegisterTaskHandler(
        string agentId,
        Func<A2AAgentTask, CancellationToken, Task<A2AAgentTask>> handler)
    {
        _taskHandlers[agentId] = handler;
        _logger.LogInformation("[A2A:Server] Task handler registered: {AgentId}", agentId);
    }

    /// <summary>
    /// Routes the incoming task to the appropriate handler.
    /// Corresponds to the tasks/send endpoint of the A2A protocol.
    /// </summary>
    public async Task<A2AAgentTask> HandleTaskAsync(
        string agentId,
        A2AAgentTask task,
        CancellationToken cancellationToken = default)
    {
        if (!_taskHandlers.TryGetValue(agentId, out var handler))
        {
            _logger.LogWarning("[A2A:Server] Handler not found: {AgentId}", agentId);
            task.Status = new A2AAgentTaskStatus
            {
                State = A2ATaskState.Failed,
                Timestamp = DateTimeOffset.UtcNow,
                Message = new A2AAgentMessage
                {
                    Role = A2AMessageRole.Agent,
                    MessageId = Guid.NewGuid().ToString(),
                    Parts = new List<A2APart> { new A2ATextPart { Text = $"Agent not found: {agentId}" } }
                }
            };
            return task;
        }

        _logger.LogInformation("[A2A:Server] Processing task: {TaskId} → {AgentId}",
            task.Id, agentId);

        try
        {
            task.Status = new A2AAgentTaskStatus
            {
                State = A2ATaskState.Working,
                Timestamp = DateTimeOffset.UtcNow
            };

            var result = await handler(task, cancellationToken);

            result.Status = new A2AAgentTaskStatus
            {
                State = A2ATaskState.Completed,
                Timestamp = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("[A2A:Server] Task completed: {TaskId}", task.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A2A:Server] Task failed: {TaskId}", task.Id);
            task.Status = new A2AAgentTaskStatus
            {
                State = A2ATaskState.Failed,
                Timestamp = DateTimeOffset.UtcNow,
                Message = new A2AAgentMessage
                {
                    Role = A2AMessageRole.Agent,
                    MessageId = Guid.NewGuid().ToString(),
                    Parts = new List<A2APart> { new A2ATextPart { Text = $"Error: {ex.Message}" } }
                }
            };
            return task;
        }
    }

    /// <summary>
    /// Creates an Agent Card for the Researcher Agent.
    /// Served from the /.well-known/agent.json endpoint in the A2A protocol.
    /// </summary>
    public static A2AAgentCard CreateResearcherAgentCard() => new()
    {
        Name = "Researcher Agent",
        Description = "A research agent that gathers information by searching the web.",
        Url = "https://localhost:44331/a2a/researcher",
        Version = "1.0.0",
        Capabilities = new A2AAgentCapabilities
        {
            Streaming = false,
            PushNotifications = false
        },
        Skills = new List<A2AAgentSkill>
        {
            new()
            {
                Id = "web-research",
                Name = "Web Research",
                Description = "Searches the web on the specified topic and collects raw data.",
                Tags = new List<string> { "research", "web-search", "data-collection" }
            }
        }
    };

    /// <summary>
    /// Creates an Agent Card for the Analysis Agent.
    /// </summary>
    public static A2AAgentCard CreateAnalysisAgentCard() => new()
    {
        Name = "Analysis Agent",
        Description = "An analysis agent that analyzes raw data and produces structured results.",
        Url = "https://localhost:44331/a2a/analyst",
        Version = "1.0.0",
        Capabilities = new A2AAgentCapabilities
        {
            Streaming = false,
            PushNotifications = false
        },
        Skills = new List<A2AAgentSkill>
        {
            new()
            {
                Id = "data-analysis",
                Name = "Data Analysis",
                Description = "Analyzes raw research data, summarizes it, and presents it in a structured format.",
                Tags = new List<string> { "analysis", "summarization", "structuring" }
            }
        }
    };
}
