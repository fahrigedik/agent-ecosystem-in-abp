using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace AgentEcosystem.McpTools;

/// <summary>
/// MCP Web Search Tools — Tavily Search API integration.
/// 
/// Provides MCP tools that enable the Researcher Agent to search the web.
/// Requires a valid Tavily API key configured in appsettings.json.
/// </summary>
[McpServerToolType]
public class McpWebSearchTools
{
    private readonly ILogger<McpWebSearchTools> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _tavilyApiKey;

    public McpWebSearchTools(
        ILogger<McpWebSearchTools> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("WebSearch");
        _tavilyApiKey = configuration["Tavily:ApiKey"] ?? "";
    }

    /// <summary>
    /// Searches the web and returns results.
    /// Uses the Tavily Search API. Returns an error if the API key is not configured.
    /// </summary>
    [McpServerTool(Name = "web_search")]
    [Description("Searches the web. Gathers up-to-date information related to the research topic.")]
    public async Task<string> SearchAsync(
        [Description("Search query")] string query)
    {
        _logger.LogInformation("[MCP:WebSearch] Searching: '{Query}'", query);

        if (string.IsNullOrEmpty(_tavilyApiKey))
        {
            _logger.LogWarning("[MCP:WebSearch] Tavily API key is not configured.");
            return "Tavily API key is not configured. Please set 'Tavily:ApiKey' in appsettings.json.";
        }

        var result = await SearchWithTavilyAsync(query);
        if (result != null) return result;

        return $"Tavily Search: No results found for '{query}'.";
    }

    /// <summary>
    /// Tavily Search API — a search API designed for AI agents.
    /// POST https://api.tavily.com/search
    /// </summary>
    private async Task<string?> SearchWithTavilyAsync(string query)
    {
        try
        {
            _logger.LogInformation("[MCP:WebSearch] Searching with Tavily API...");

            var requestBody = new
            {
                api_key = _tavilyApiKey,
                query = query,
                max_results = 10,
                include_answer = true,
                search_depth = "advanced"
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("https://api.tavily.com/search", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("[MCP:WebSearch] Tavily API {StatusCode}: {Body}",
                    response.StatusCode, errorBody);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var results = new List<SearchResult>();

            if (doc.RootElement.TryGetProperty("results", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    results.Add(new SearchResult
                    {
                        Title = item.TryGetProperty("title", out var title)
                            ? title.GetString() ?? "" : "",
                        Url = item.TryGetProperty("url", out var url)
                            ? url.GetString() ?? "" : "",
                        Snippet = item.TryGetProperty("content", out var content)
                            ? content.GetString() ?? "" : "",
                        Source = item.TryGetProperty("url", out var src)
                            ? new Uri(src.GetString() ?? "https://unknown").Host : "unknown"
                    });
                }
            }

            // Include Tavily's AI summary answer as well
            var answer = doc.RootElement.TryGetProperty("answer", out var ans)
                ? ans.GetString() : null;

            _logger.LogInformation(
                "[MCP:WebSearch] Tavily: {Count} results found for: '{Query}'",
                results.Count, query);

            if (results.Count == 0)
                return $"Tavily Search: No results found for '{query}'.";

            var formatted = FormatResults(query, results, "Tavily Search API");

            if (!string.IsNullOrEmpty(answer))
                formatted = $"AI Summary: {answer}\n\n{formatted}";

            return formatted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MCP:WebSearch] Tavily API error");
            return null;
        }
    }

    /// <summary>
    /// Fetches the content of a specified URL.
    /// </summary>
    [McpServerTool(Name = "fetch_url_content")]
    [Description("Fetches the content of a specified URL. Returns the text content of the web page.")]
    public async Task<string> FetchUrlContentAsync(
        [Description("URL to fetch content from")] string url)
    {
        try
        {
            _logger.LogInformation("[MCP:WebSearch] Fetching URL content: {Url}", url);
            var content = await _httpClient.GetStringAsync(url);

            if (content.Length > 5000)
                content = content[..5000] + "\n\n[...content truncated...]";

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MCP:WebSearch] Error fetching URL content: {Url}", url);
            return $"Failed to fetch URL content ({url}): {ex.Message}";
        }
    }

    // ─── Helper Methods ───

    private string FormatResults(string query, List<SearchResult> results, string source)
    {
        return $"Web Search Results: '{query}'\n" +
               $"Source: {source}\n" +
               $"Search Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\n" +
               $"Results Found: {results.Count}\n\n" +
               string.Join("\n\n", results.Select((r, i) =>
                   $"{i + 1}. {r.Title}\n   URL: {r.Url}\n   Source: {r.Source}\n   Summary: {r.Snippet}"));
    }

    private class SearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }
}
