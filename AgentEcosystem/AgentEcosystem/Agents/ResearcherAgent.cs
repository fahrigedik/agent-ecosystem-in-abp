using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentEcosystem.Agents.Core;
using AgentEcosystem.McpTools;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgentEcosystem.Agents;

/// <summary>
/// Researcher Agent — GPT-powered web research agent.
/// 
/// .NET adaptation of ADK's LlmAgent concept:
/// - Inherits from BaseAgent (ADK hierarchy)
/// - Communicates with GPT via IChatClient (Microsoft.Extensions.AI)
/// - Performs web searches using MCP tools (WebSearchTools)
/// - Sends data to Analysis Agent via A2A protocol
/// 
/// Flow:
/// 1. Receives the user query
/// 2. Searches the web using MCP WebSearch tool
/// 3. Sends raw results to GPT to produce a "research report"
/// 4. Writes raw data and the report to AgentContext.State
/// 5. Passed to the Analysis Agent via A2A
/// </summary>
public class ResearcherAgent : BaseAgent
{
    private readonly Kernel _kernel;
    private readonly McpWebSearchTools _webSearchTools;
    private readonly ILogger<ResearcherAgent> _logger;

    public ResearcherAgent(
        Kernel kernel,
        McpWebSearchTools webSearchTools,
        ILogger<ResearcherAgent> logger)
    {
        _kernel = kernel;
        _webSearchTools = webSearchTools;
        _logger = logger;

        Name = "ResearcherAgent";
        Description = "A researcher agent that gathers information by searching the web.";
    }

    /// <summary>
    /// Executes the research task.
    /// Equivalent of ADK's _run_async_impl.
    /// </summary>
    public override async Task<AgentEvent> RunAsync(
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        var query = context.UserQuery;
        _logger.LogInformation("[ResearcherAgent] Research starting: '{Query}'", query);

        try
        {
            // === STEP 1: Use MCP WebSearch Tool ===
            // In the MCP protocol, tools enable LLMs to interact with
            // the external world. Here a web search is performed.
            _logger.LogInformation("[ResearcherAgent] Calling MCP:WebSearch tool...");
            var searchResults = await _webSearchTools.SearchAsync(query);

            // === STEP 2: Generate Research Report with GPT ===
            // Uses the IChatClient interface from Microsoft.Extensions.AI.
            // This interface supports all LLMs: OpenAI, Azure OpenAI, Ollama, etc.
            var systemPrompt = """
                You are an expert researcher agent. Your task is to conduct comprehensive research on the given topic using web search tools.
                1. Carefully examine the provided search results
                2. Identify the most important and reliable information
                3. Compile the information along with their sources
                4. Create a structured research report
                
                Your report should include:
                - Key findings (bullet points)
                - Source information
                - Important details and data
                - Conflicting information, if any
                
                Respond in English. Use an academic and professional tone.
                """;

            var userMessage = $"""
                Research Topic: {query}
                
                Web Search Results:
                {searchResults}
                
                Analyze these results and prepare a comprehensive research report.
                """;

            _logger.LogInformation("[ResearcherAgent] Generating research report with Semantic Kernel...");

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(userMessage);

            var result = await chatService.GetChatMessageContentsAsync(
                history,
                cancellationToken: cancellationToken);

            var researchReport = result.LastOrDefault()?.Content ?? "Failed to generate research report.";

            _logger.LogInformation(
                "[ResearcherAgent] Research report prepared ({Length} characters)",
                researchReport.Length);

            // === STEP 3: Write to State (ADK Pattern) ===
            // In ADK, data sharing between agents is done via session.state.
            // Research results are written to state and passed to the next agent.
            context.SetState("search_results", searchResults);
            context.SetState("research_report", researchReport);
            context.SetState("research_query", query);
            context.SetState("research_status", "completed");

            return new AgentEvent
            {
                Author = Name,
                Status = "completed",
                Content = researchReport,
                Actions = new EventActions
                {
                    StateUpdates = new Dictionary<string, object>
                    {
                        ["search_results"] = searchResults,
                        ["research_report"] = researchReport,
                        ["research_query"] = query
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResearcherAgent] Research error: '{Query}'", query);

            return new AgentEvent
            {
                Author = Name,
                Status = "failed",
                Content = $"An error occurred during research: {ex.Message}"
            };
        }
    }
}
