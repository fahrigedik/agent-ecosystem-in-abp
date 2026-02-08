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
/// MCP Veritabanı Araçları — Model Context Protocol SDK ile entegre.
/// 
/// [McpServerToolType] attribute'u ile bu sınıf MCP Server tarafından
/// otomatik olarak keşfedilir ve araç olarak sunulur.
/// 
/// Her metod [McpServerTool] attribute'u ile işaretlenmiştir.
/// MCP istemcileri (LLM'ler dahil) bu araçları doğrudan çağırabilir.
/// 
/// ABP Framework'ün Repository pattern'ını kullanarak
/// araştırma sonuçlarını veritabanında saklar ve sorgular.
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
    /// Araştırma sonucunu veritabanına kaydeder.
    /// </summary>
    [McpServerTool(Name = "save_research_to_database")]
    [Description("Bir araştırma sonucunu veritabanına kaydeder. Query, rawData, analyzedResult ve sources parametrelerini alır.")]
    public async Task<string> SaveResearchAsync(
        [Description("Araştırma sorgusu")] string query,
        [Description("Ham araştırma verileri")] string rawData,
        [Description("Analiz edilmiş sonuç")] string analyzedResult,
        [Description("Kaynaklar (virgülle ayrılmış)")] string sources)
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
                "[MCP:Database] Araştırma kaydedildi: {Id} - {Query}",
                saved.Id, query);

            return $"Araştırma başarıyla kaydedildi. ID: {saved.Id}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MCP:Database] Araştırma kaydetme hatası: {Query}", query);
            return $"Hata: {ex.Message}";
        }
    }

    /// <summary>
    /// Geçmiş araştırmaları anahtar kelimeye göre arar.
    /// </summary>
    [McpServerTool(Name = "search_past_research")]
    [Description("Geçmiş araştırmaları anahtar kelimeye göre arar. Veritabanında sorgu veya sonuç içinde geçen kayıtları döner.")]
    public async Task<string> SearchPastResearchAsync(
        [Description("Aranacak anahtar kelime")] string keyword)
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
                return $"'{keyword}' ile ilgili geçmiş araştırma bulunamadı.";

            var output = results.Select(r =>
                $"[{r.Id}] {r.Query} (Durum: {r.Status}, Tarih: {r.CompletedAt:yyyy-MM-dd HH:mm})");

            _logger.LogInformation(
                "[MCP:Database] {Count} geçmiş araştırma bulundu: '{Keyword}'",
                results.Count, keyword);

            return string.Join("\n", output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MCP:Database] Araştırma arama hatası: {Keyword}", keyword);
            return $"Arama hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Son N araştırmayı getirir.
    /// </summary>
    [McpServerTool(Name = "get_recent_research")]
    [Description("Son N araştırmayı listeler. Varsayılan olarak son 5 araştırmayı getirir.")]
    public async Task<string> GetRecentResearchAsync(
        [Description("Kaç adet araştırma getirileceği (varsayılan: 5)")] int count = 5)
    {
        try
        {
            var queryable = await _repository.GetQueryableAsync();
            var results = queryable
                .OrderByDescending(r => r.CompletedAt)
                .Take(count)
                .ToList();

            if (!results.Any())
                return "Henüz kayıtlı araştırma bulunmuyor.";

            var output = results.Select(r =>
                $"[{r.Id}] {r.Query}\n  Durum: {r.Status} | Tarih: {r.CompletedAt:yyyy-MM-dd HH:mm}\n  Özet: {Truncate(r.AnalyzedResult, 200)}");

            return string.Join("\n---\n", output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MCP:Database] Son araştırmaları listeleme hatası");
            return $"Listeleme hatası: {ex.Message}";
        }
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? string.Empty;
        return text[..maxLength] + "...";
    }
}
