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
/// Araştırma Application Service — ABP Auto API Controller ile REST endpoint olur.
/// 
/// ABP'nin ConventionalControllers özelliği sayesinde bu sınıf
/// otomatik olarak /api/app/research endpoint'ine dönüşür.
/// 
/// Endpoint'ler:
///   POST /api/app/research/execute    → Yeni araştırma başlat
///   GET  /api/app/research/history    → Geçmiş araştırmaları listele
///   GET  /api/app/research/{id}       → Belirli araştırmayı getir
///   GET  /api/app/research/agent-cards → A2A Agent Card'larını getir
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
    /// Yeni araştırma başlatır.
    /// İki mod destekler:
    /// - "sequential": ADK SequentialAgent pattern (varsayılan)
    /// - "a2a": A2A protokolü üzerinden ajan iletişimi
    /// </summary>
    public async Task<ResearchResultDto> ExecuteAsync(ResearchRequestDto input)
    {
        _logger.LogInformation(
            "[API] Araştırma talebi alındı: '{Query}' (Mod: {Mode})",
            input.Query, input.Mode);

        if (string.IsNullOrWhiteSpace(input.Query))
            throw new Volo.Abp.UserFriendlyException("Araştırma sorgusu boş olamaz.");

        ResearchResultDto result;

        if (input.Mode?.ToLowerInvariant() == "a2a")
        {
            // A2A protokolü ile çalıştır
            result = await _orchestrator.ExecuteResearchViaA2AAsync(input.Query);
        }
        else
        {
            // ADK SequentialAgent pattern ile çalıştır (varsayılan)
            result = await _orchestrator.ExecuteResearchAsync(input.Query);
        }

        _logger.LogInformation(
            "[API] Araştırma tamamlandı: '{Query}' - {Status} ({ElapsedMs}ms)",
            input.Query, result.Status, result.ProcessingTimeMs);

        return result;
    }

    /// <summary>
    /// Geçmiş araştırmaları listeler.
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
    /// Belirli bir araştırma kaydını getirir.
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
    /// A2A Agent Card'larını döner.
    /// A2A protokolünde /.well-known/agent.json endpoint'inden sunulur.
    /// İstemciler bu card'ları kullanarak ajanları keşfeder.
    /// </summary>
    public Task<object> GetAgentCardsAsync()
    {
        var cards = new
        {
            ResearcherAgent = new
            {
                Name = "Araştırmacı Ajan",
                Description = "Web'de arama yaparak bilgi toplayan araştırma ajanı.",
                Url = "https://localhost:44331/a2a/researcher",
                Version = "1.0.0",
                Skills = new[]
                {
                    new { Id = "web-research", Name = "Web Araştırması", Tags = new[] { "research", "web-search" } }
                }
            },
            AnalysisAgent = new
            {
                Name = "Analiz Ajanı",
                Description = "Ham verileri analiz ederek yapılandırılmış sonuçlar üreten analiz ajanı.",
                Url = "https://localhost:44331/a2a/analyst",
                Version = "1.0.0",
                Skills = new[]
                {
                    new { Id = "data-analysis", Name = "Veri Analizi", Tags = new[] { "analysis", "summarization" } }
                }
            }
        };

        return Task.FromResult<object>(cards);
    }
}
