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
/// Araştırmacı Ajan — GPT destekli web araştırma ajanı.
/// 
/// ADK'nın LlmAgent kavramının .NET uyarlaması:
/// - BaseAgent'tan türer (ADK hierarchy)
/// - IChatClient üzerinden GPT ile konuşur (Microsoft.Extensions.AI)
/// - MCP araçlarını (WebSearchTools) kullanarak web'de arama yapar
/// - A2A protokolüyle Analiz Ajanı'na veri gönderir
/// 
/// Akış:
/// 1. Kullanıcı sorusunu alır
/// 2. MCP WebSearch aracıyla web'de arama yapar
/// 3. GPT'ye ham sonuçları gönderip "araştırma raporu" ürettirir
/// 4. Ham veriyi ve raporu AgentContext.State'e yazar
/// 5. A2A üzerinden Analiz Ajanı'na aktarılır
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
        Description = "Web'de arama yaparak bilgi toplayan araştırmacı ajan.";
    }

    /// <summary>
    /// Araştırma görevini çalıştırır.
    /// ADK'daki _run_async_impl'in karşılığı.
    /// </summary>
    public override async Task<AgentEvent> RunAsync(
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        var query = context.UserQuery;
        _logger.LogInformation("[ResearcherAgent] Araştırma başlıyor: '{Query}'", query);

        try
        {
            // === ADIM 1: MCP WebSearch Aracını Kullan ===
            // MCP protokolünde araçlar (tools) LLM'lerin dış dünya ile
            // etkileşim kurmasını sağlar. Burada web araması yapılıyor.
            _logger.LogInformation("[ResearcherAgent] MCP:WebSearch aracı çağrılıyor...");
            var searchResults = await _webSearchTools.SearchAsync(query);

            // === ADIM 2: GPT ile Araştırma Raporu Oluştur ===
            // Microsoft.Extensions.AI'ın IChatClient arayüzü kullanılıyor.
            // Bu arayüz, OpenAI, Azure OpenAI, Ollama vb. tüm LLM'leri destekler.
            var systemPrompt = """
                Sen uzman bir araştırmacı ajansın. Görevin:
                1. Verilen arama sonuçlarını dikkatle incele
                2. En önemli ve güvenilir bilgileri belirle
                3. Bilgileri kaynaklarıyla birlikte derle
                4. Yapılandırılmış bir araştırma raporu oluştur
                
                Raporunda şunları içer:
                - Ana bulgular (madde madde)
                - Kaynak bilgileri
                - Önemli detaylar ve veriler
                - Varsa çelişkili bilgiler
                
                Türkçe yanıt ver. Akademik ve profesyonel bir ton kullan.
                """;

            var userMessage = $"""
                Araştırma Konusu: {query}
                
                Web Arama Sonuçları:
                {searchResults}
                
                Bu sonuçları analiz et ve kapsamlı bir araştırma raporu hazırla.
                """;

            _logger.LogInformation("[ResearcherAgent] Semantic Kernel ile araştırma raporu hazırlatılıyor...");

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(userMessage);

            var result = await chatService.GetChatMessageContentsAsync(
                history,
                cancellationToken: cancellationToken);

            var researchReport = result.LastOrDefault()?.Content ?? "Araştırma raporu oluşturulamadı.";

            _logger.LogInformation(
                "[ResearcherAgent] Araştırma raporu hazırlandı ({Length} karakter)",
                researchReport.Length);

            // === ADIM 3: State'e Yaz (ADK Pattern) ===
            // ADK'da ajanlar arası veri paylaşımı session.state üzerinden yapılır.
            // Araştırma sonuçları state'e yazılıp bir sonraki ajana aktarılır.
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
            _logger.LogError(ex, "[ResearcherAgent] Araştırma hatası: '{Query}'", query);

            return new AgentEvent
            {
                Author = Name,
                Status = "failed",
                Content = $"Araştırma sırasında hata oluştu: {ex.Message}"
            };
        }
    }
}
