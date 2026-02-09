using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentEcosystem.Agents;
using AgentEcosystem.Entities;
using AgentEcosystem.Services.Dtos;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace AgentEcosystem.Services;

/// <summary>
/// Research Application Service — Becomes a REST endpoint via ABP Auto API Controller.
/// 
/// Thanks to ABP's ConventionalControllers feature, this class is
/// automatically mapped to the /api/app/research endpoint.
/// 
/// Endpoints:
///   POST /api/app/research/execute    → Start a new research
///   GET  /api/app/research/history    → List past research records
///   GET  /api/app/research/{id}       → Get a specific research record
///   GET  /api/app/research/agent-cards → Get A2A Agent Cards
/// </summary>
public class ResearchAppService : AgentEcosystemAppService
{
    private readonly ResearchOrchestrator _orchestrator;
    private readonly IRepository<ResearchRecord, Guid> _repository;
    private readonly ILogger<ResearchAppService> _logger;

    public ResearchAppService(
        ResearchOrchestrator orchestrator,
        IRepository<ResearchRecord, Guid> repository,
        ILogger<ResearchAppService> logger)
    {
        _orchestrator = orchestrator;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Starts a new research.
    /// Supports two modes:
    /// - "sequential": ADK SequentialAgent pattern (default)
    /// - "a2a": Agent communication via A2A protocol
    /// </summary>
    public async Task<ResearchResultDto> ExecuteAsync(ResearchRequestDto input)
    {
        _logger.LogInformation(
            "[API] Research request received: '{Query}' (Mode: {Mode})",
            input.Query, input.Mode);

        if (string.IsNullOrWhiteSpace(input.Query))
            throw new Volo.Abp.UserFriendlyException("Research query cannot be empty.");

        ResearchResultDto result;

        if (input.Mode?.ToLowerInvariant() == "a2a")
        {
            // Execute via A2A protocol
            result = await _orchestrator.ExecuteResearchViaA2AAsync(input.Query);
        }
        else
        {
            // Execute via ADK SequentialAgent pattern (default)
            result = await _orchestrator.ExecuteResearchAsync(input.Query);
        }

        _logger.LogInformation(
            "[API] Research completed: '{Query}' - {Status} ({ElapsedMs}ms)",
            input.Query, result.Status, result.ProcessingTimeMs);

        return result;
    }

    /// <summary>
    /// Lists past research records.
    /// </summary>
    public async Task<List<ResearchSummaryDto>> GetHistoryAsync()
    {
        var queryable = await _repository.GetQueryableAsync();
        var records = queryable
            .OrderByDescending(r => r.CompletedAt)
            .Take(20)
            .ToList();

        return records.Select(r => new ResearchSummaryDto
        {
            Id = r.Id,
            Query = r.Query,
            Status = r.Status.ToString(),
            CompletedAt = r.CompletedAt,
            ProcessingTimeMs = r.ProcessingTimeMs,
            ResultPreview = r.AnalyzedResult.Length > 200
                ? r.AnalyzedResult[..200] + "..."
                : r.AnalyzedResult
        }).ToList();
    }

    /// <summary>
    /// Gets a specific research record by id.
    /// </summary>
    public async Task<ResearchResultDto?> GetByIdAsync(Guid id)
    {
        var record = await _repository.FindAsync(id);
        if (record == null) return null;

        return new ResearchResultDto
        {
            SessionId = record.SessionId ?? "",
            Query = record.Query,
            RawSearchResults = record.RawData,
            AnalysisResult = record.AnalyzedResult,
            Status = record.Status.ToString(),
            ProcessingTimeMs = record.ProcessingTimeMs ?? 0
        };
    }

    /// <summary>
    /// Returns A2A Agent Cards.
    /// Served from the /.well-known/agent.json endpoint in the A2A protocol.
    /// Clients discover agents using these cards.
    /// </summary>
    public Task<object> GetAgentCardsAsync()
    {
        var cards = new
        {
            ResearcherAgent = new
            {
                Name = "Researcher Agent",
                Description = "A research agent that gathers information by searching the web.",
                Url = "https://localhost:44331/a2a/researcher",
                Version = "1.0.0",
                Skills = new[]
                {
                    new { Id = "web-research", Name = "Web Research", Tags = new[] { "research", "web-search" } }
                }
            },
            AnalysisAgent = new
            {
                Name = "Analysis Agent",
                Description = "An analysis agent that produces structured results by analyzing raw data.",
                Url = "https://localhost:44331/a2a/analyst",
                Version = "1.0.0",
                Skills = new[]
                {
                    new { Id = "data-analysis", Name = "Data Analysis", Tags = new[] { "analysis", "summarization" } }
                }
            }
        };

        return Task.FromResult<object>(cards);
    }
}
