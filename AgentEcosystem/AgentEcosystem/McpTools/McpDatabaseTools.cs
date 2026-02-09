using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentEcosystem.Entities;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Volo.Abp.Domain.Repositories;

namespace AgentEcosystem.McpTools;

/// <summary>
/// MCP Database Tools — integrated with Model Context Protocol SDK.
/// 
/// This class is automatically discovered and exposed as tools
/// by the MCP Server via the [McpServerToolType] attribute.
/// 
/// Each method is marked with the [McpServerTool] attribute.
/// MCP clients (including LLMs) can call these tools directly.
/// 
/// Uses ABP Framework's Repository pattern to store and query
/// research results in the database.
/// </summary>
[McpServerToolType]
public class McpDatabaseTools
{
    private readonly IRepository<ResearchRecord, Guid> _repository;
    private readonly ILogger<McpDatabaseTools> _logger;

    public McpDatabaseTools(
        IRepository<ResearchRecord, Guid> repository,
        ILogger<McpDatabaseTools> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Saves a research result to the database.
    /// </summary>
    [McpServerTool(Name = "save_research_to_database")]
    [Description("Saves a research result to the database. Takes query, rawData, analyzedResult, and sources parameters.")]
    public async Task<string> SaveResearchAsync(
        [Description("Research query")] string query,
        [Description("Raw research data")] string rawData,
        [Description("Analyzed result")] string analyzedResult,
        [Description("Sources (comma-separated)")] string sources)
    {
        try
        {
            var record = new ResearchRecord
            {
                Query = query,
                RawData = rawData,
                AnalyzedResult = analyzedResult,
                Sources = sources,
                Status = ResearchStatus.Completed,
                CompletedAt = DateTime.UtcNow
            };

            var saved = await _repository.InsertAsync(record, autoSave: true);

            _logger.LogInformation(
                "[MCP:Database] Research saved: {Id} - {Query}",
                saved.Id, query);

            return $"Research saved successfully. ID: {saved.Id}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MCP:Database] Error saving research: {Query}", query);
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Searches past research by keyword.
    /// </summary>
    [McpServerTool(Name = "search_past_research")]
    [Description("Searches past research by keyword. Returns records where the query or result contains the keyword.")]
    public async Task<string> SearchPastResearchAsync(
        [Description("Keyword to search for")] string keyword)
    {
        try
        {
            var queryable = await _repository.GetQueryableAsync();
            var results = queryable
                .Where(r => r.Query.Contains(keyword) ||
                            r.AnalyzedResult.Contains(keyword))
                .OrderByDescending(r => r.CompletedAt)
                .Take(10)
                .ToList();

            if (!results.Any())
                return $"No past research found related to '{keyword}'.";

            var output = results.Select(r =>
                $"[{r.Id}] {r.Query} (Status: {r.Status}, Date: {r.CompletedAt:yyyy-MM-dd HH:mm})");

            _logger.LogInformation(
                "[MCP:Database] {Count} past research records found for: '{Keyword}'",
                results.Count, keyword);

            return string.Join("\n", output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MCP:Database] Error searching research: {Keyword}", keyword);
            return $"Search error: {ex.Message}";
        }
    }

    /// <summary>
    /// Retrieves the last N research records.
    /// </summary>
    [McpServerTool(Name = "get_recent_research")]
    [Description("Lists the last N research records. Defaults to the last 5 records.")]
    public async Task<string> GetRecentResearchAsync(
        [Description("Number of research records to retrieve (default: 5)")] int count = 5)
    {
        try
        {
            var queryable = await _repository.GetQueryableAsync();
            var results = queryable
                .OrderByDescending(r => r.CompletedAt)
                .Take(count)
                .ToList();

            if (!results.Any())
                return "No research records saved yet.";

            var output = results.Select(r =>
                $"[{r.Id}] {r.Query}\n  Status: {r.Status} | Date: {r.CompletedAt:yyyy-MM-dd HH:mm}\n  Summary: {Truncate(r.AnalyzedResult, 200)}");

            return string.Join("\n---\n", output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MCP:Database] Error listing recent research");
            return $"Listing error: {ex.Message}";
        }
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? string.Empty;
        return text[..maxLength] + "...";
    }
}
