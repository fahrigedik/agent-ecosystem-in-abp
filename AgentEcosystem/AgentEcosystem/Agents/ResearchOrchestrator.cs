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

// A2A NuGet paketi namespace alias'ları (proje namespace'i ile çakışma önlemi)
using A2AAgentTask = global::A2A.AgentTask;
using A2AAgentMessage = global::A2A.AgentMessage;
using A2AMessageRole = global::A2A.MessageRole;
using A2ATextPart = global::A2A.TextPart;
using A2AArtifact = global::A2A.Artifact;
using A2APart = global::A2A.Part;

namespace AgentEcosystem.Agents;

/// <summary>
/// Araştırma Orkestratörü — Tüm sistemi koordine eden merkezi bileşen.
/// 
/// Bu sınıf üç protokolü bir arada kullanır:
/// 
/// 1. ADK (Agent Development Kit) Pattern:
///    - SequentialAgent ile ajanları sıralı çalıştırır
///    - AgentContext ile durum yönetimi yapar
///    - AgentEvent ile olay tabanlı iletişim sağlar
/// 
/// 2. A2A (Agent-to-Agent) Protokolü:
///    - Ajanlar arası görev gönderme (tasks/send)
///    - Agent Card ile keşif
///    - Task lifecycle yönetimi
/// 
/// 3. MCP (Model Context Protocol):
///    - Web arama aracı (ResearcherAgent)
///    - Dosya kaydetme aracı (AnalysisAgent)
///    - Veritabanı aracı (AnalysisAgent)
/// 
/// Akış Şeması:
/// ┌─────────────┐     ┌──────────────────┐     ┌──────────────┐
/// │  Kullanıcı   │────▶│ ResearcherAgent   │────▶│ AnalysisAgent │
/// │  Sorgusu     │     │ (MCP:WebSearch)   │ A2A │ (MCP:File+DB) │
/// └─────────────┘     └──────────────────┘     └──────────────┘
///                              │                        │
///                              ▼                        ▼
///                     ┌──────────────┐         ┌──────────────┐
///                     │ Ham Veriler   │         │ Final Rapor   │
///                     │ (State)       │         │ (Dosya + DB)  │
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

        // A2A görev işleyicilerini kaydet
        RegisterA2AHandlers();
    }

    /// <summary>
    /// A2A görev işleyicilerini kaydeder.
    /// Her ajan kendi A2A handler'ını sunucuya kaydeder.
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

            // Ham verileri task mesajlarından context state'e aktar
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

        _logger.LogInformation("[Orchestrator] A2A görev işleyicileri kaydedildi.");
    }

    /// <summary>
    /// Tam araştırma pipeline'ını çalıştırır.
    /// ADK SequentialAgent pattern'ı + A2A iletişimi + MCP araçları.
    /// </summary>
    public async Task<ResearchResultDto> ExecuteResearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var sessionId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "[Orchestrator] ===== Araştırma başlıyor =====\n" +
            "  Oturum: {SessionId}\n  Sorgu: '{Query}'",
            sessionId, query);

        try
        {
            // ───────────────────────────────────────────────
            // YÖNTEM 1: ADK SequentialAgent Pattern
            // ───────────────────────────────────────────────
            // İki ajanı sıralı çalıştır. İlk ajanın çıktısı
            // (state üzerinden) ikinci ajanın girdisi olur.

            var pipeline = new SequentialAgent
            {
                Name = "ResearchPipeline",
                Description = "Araştırma → Analiz sıralı pipeline'ı"
            };

            pipeline.AddSubAgent(_researcherAgent);
            pipeline.AddSubAgent(_analysisAgent);

            var context = new AgentContext
            {
                SessionId = sessionId,
                UserQuery = query
            };

            _logger.LogInformation("[Orchestrator] ADK SequentialAgent pipeline başlatılıyor...");
            var finalEvent = await pipeline.RunAsync(context, cancellationToken);

            stopwatch.Stop();

            // ───────────────────────────────────────────────
            // SONUÇ DTO'SUNU OLUŞTUR
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
                "[Orchestrator] ===== Araştırma tamamlandı =====\n" +
                "  Oturum: {SessionId}\n  Süre: {ElapsedMs}ms\n  Durum: {Status}",
                sessionId, stopwatch.ElapsedMilliseconds, result.Status);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "[Orchestrator] Araştırma pipeline hatası: {SessionId}", sessionId);

            return new ResearchResultDto
            {
                SessionId = sessionId,
                Query = query,
                Status = "Failed",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                AnalysisResult = $"Araştırma sırasında hata oluştu: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// A2A protokolü üzerinden araştırma çalıştırır.
    /// Ajanlar arasında görev gönderme ve sonuç alma.
    /// </summary>
    public async Task<ResearchResultDto> ExecuteResearchViaA2AAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var sessionId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "[Orchestrator:A2A] Araştırma başlıyor (A2A Mode)\n" +
            "  Oturum: {SessionId}\n  Sorgu: '{Query}'",
            sessionId, query);

        try
        {
            // ───────────────────────────────────────────────
            // ADIM 1: Araştırmacı Ajan'a A2A Task gönder
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

            _logger.LogInformation("[Orchestrator:A2A] → Araştırmacı Ajan'a görev gönderiliyor...");
            var researchResult = await _a2aServer.HandleTaskAsync(
                "researcher", researchTask, cancellationToken);

            var researchReport = ExtractTextFromArtifacts(researchResult);

            // ───────────────────────────────────────────────
            // ADIM 2: Analiz Ajanı'na A2A Task gönder
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

            _logger.LogInformation("[Orchestrator:A2A] → Analiz Ajanı'na görev gönderiliyor...");
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
            _logger.LogError(ex, "[Orchestrator:A2A] A2A pipeline hatası: {SessionId}", sessionId);

            return new ResearchResultDto
            {
                SessionId = sessionId,
                Query = query,
                Status = "Failed",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                AnalysisResult = $"A2A araştırma hatası: {ex.Message}"
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
