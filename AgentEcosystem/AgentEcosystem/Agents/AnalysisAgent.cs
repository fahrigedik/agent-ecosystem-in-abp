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
/// Analiz Ajanı — GPT destekli veri analiz ajanı.
/// 
/// ADK'nın LlmAgent kavramının .NET uyarlaması:
/// - Araştırmacı Ajan'dan gelen verileri (State üzerinden) okur
/// - GPT ile analiz eder, özetler ve yapılandırır
/// - MCP araçlarıyla (FileSystem, Database) sonuçları kaydeder
/// - Final sonucu kullanıcıya sunar
/// 
/// A2A Akışında Bu Ajan:
/// - Araştırmacı Ajan'dan A2A task olarak veri alır
/// - Analiz sonucunu A2A artifact olarak döner
/// - MCP üzerinden dosya ve veritabanına kaydeder
/// </summary>
public class AnalysisAgent : BaseAgent
{
    private readonly Kernel _kernel;
    private readonly McpFileSystemTools _fileSystemTools;
    private readonly McpDatabaseTools _databaseTools;
    private readonly ILogger<AnalysisAgent> _logger;

    public AnalysisAgent(
        Kernel kernel,
        McpFileSystemTools fileSystemTools,
        McpDatabaseTools databaseTools,
        ILogger<AnalysisAgent> logger)
    {
        _kernel = kernel;
        _fileSystemTools = fileSystemTools;
        _databaseTools = databaseTools;
        _logger = logger;

        Name = "AnalysisAgent";
        Description = "Araştırma verilerini analiz edip yapılandırılmış sonuç üreten analiz ajanı.";
    }

    /// <summary>
    /// Analiz görevini çalıştırır.
    /// State'ten araştırma raporunu okur, analiz eder, kaydeder.
    /// </summary>
    public override async Task<AgentEvent> RunAsync(
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[AnalysisAgent] Analiz başlıyor...");

        try
        {
            // === ADIM 1: State'ten Veri Oku (ADK Pattern) ===
            // Araştırmacı Ajan'ın state'e yazdığı verileri oku
            var query = context.GetState<string>("research_query") ?? context.UserQuery;
            var searchResults = context.GetState<string>("search_results") ?? "";
            var researchReport = context.GetState<string>("research_report") ?? "";

            if (string.IsNullOrEmpty(researchReport))
            {
                return new AgentEvent
                {
                    Author = Name,
                    Status = "failed",
                    Content = "Analiz edilecek araştırma verisi bulunamadı. " +
                              "Önce Araştırmacı Ajan çalıştırılmalıdır."
                };
            }

            // === ADIM 2: GPT ile Analiz Et ===
            var systemPrompt = """
                Sen uzman bir analiz ajanısın. Görevin:
                1. Araştırma raporunu derinlemesine analiz et
                2. Ana temaları ve kalıpları belirle
                3. Bilgileri mantıksal bir yapıda düzenle
                4. Profesyonel bir Markdown raporu oluştur
                
                Rapor Formatı:
                # [Konu Başlığı]
                
                ## Yönetici Özeti
                [Kısa ve öz ana bulgular]
                
                ## Detaylı Analiz
                ### [Alt Başlık 1]
                [Detaylı analiz]
                
                ### [Alt Başlık 2]
                [Detaylı analiz]
                
                ## Kaynaklar ve Referanslar
                [Kaynak listesi]
                
                ## Sonuç ve Değerlendirme
                [Genel değerlendirme ve öneriler]
                
                ---
                *Bu rapor AI Agent Ecosystem tarafından otomatik üretilmiştir.*
                *Araştırmacı Ajan → Analiz Ajanı pipeline'ı ile oluşturulmuştur.*
                
                Türkçe yanıt ver. Akademik, profesyonel ve anlaşılır bir ton kullan.
                """;

            var userMessage = $"""
                Araştırma Konusu: {query}
                
                Araştırma Raporu:
                {researchReport}
                
                Ham Arama Sonuçları:
                {searchResults}
                
                Bu verileri analiz et ve yukarıdaki formatta yapılandırılmış bir rapor hazırla.
                """;

            _logger.LogInformation("[AnalysisAgent] Semantic Kernel ile analiz raporu hazırlanıyor...");

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(userMessage);

            var result = await chatService.GetChatMessageContentsAsync(
                history,
                cancellationToken: cancellationToken);

            var analysisResult = result.LastOrDefault()?.Content ?? "Analiz raporu oluşturulamadı.";

            _logger.LogInformation(
                "[AnalysisAgent] Analiz raporu hazırlandı ({Length} karakter)",
                analysisResult.Length);

            // === ADIM 3: MCP Araçlarıyla Kaydet ===
            // MCP protokolü ajanların dış kaynaklara (dosya, DB) erişimini sağlar

            // 3a. Dosyaya kaydet (MCP:FileSystem)
            var fileName = $"{SanitizeForFileName(query)}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.md";
            var fileSaveResult = await _fileSystemTools.SaveResearchToFileAsync(fileName, analysisResult);
            _logger.LogInformation("[AnalysisAgent] MCP:FileSystem sonuç: {Result}", fileSaveResult);

            // 3b. Veritabanına kaydet (MCP:Database)
            var dbSaveResult = await _databaseTools.SaveResearchAsync(
                query, searchResults, analysisResult, "web-search");
            _logger.LogInformation("[AnalysisAgent] MCP:Database sonuç: {Result}", dbSaveResult);

            // === ADIM 4: State'e Yaz ve Sonuç Dön ===
            context.SetState("analysis_result", analysisResult);
            context.SetState("analysis_file", fileName);
            context.SetState("analysis_status", "completed");

            return new AgentEvent
            {
                Author = Name,
                Status = "completed",
                Content = analysisResult,
                Actions = new EventActions
                {
                    StateUpdates = new Dictionary<string, object>
                    {
                        ["analysis_result"] = analysisResult,
                        ["analysis_file"] = fileName
                    },
                    // Escalate = true: Pipeline tamamlandı, üst ajana bildir
                    Escalate = true
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AnalysisAgent] Analiz hatası");

            return new AgentEvent
            {
                Author = Name,
                Status = "failed",
                Content = $"Analiz sırasında hata oluştu: {ex.Message}"
            };
        }
    }

    private static string SanitizeForFileName(string text)
    {
        var sanitized = text.Length > 50 ? text[..50] : text;
        foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            sanitized = sanitized.Replace(c, '-');
        return sanitized.Replace(' ', '-').ToLowerInvariant();
    }
}
