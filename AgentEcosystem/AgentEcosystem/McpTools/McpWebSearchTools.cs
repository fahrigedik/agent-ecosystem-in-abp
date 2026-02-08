using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace AgentEcosystem.McpTools;

/// <summary>
/// MCP Web Arama Araçları — Model Context Protocol SDK ile entegre.
/// 
/// Araştırmacı Ajan'ın web'de arama yapmasını sağlayan MCP araçları.
/// Gerçek bir arama API'si (Bing, Google) entegre edilebilir.
/// Şu an simüle edilmiş sonuçlar döner (demo amaçlı).
/// 
/// Üretim ortamında Bing Search API veya Google Custom Search 
/// API key'i ile gerçek arama yapılabilir.
/// </summary>
[McpServerToolType]
public class McpWebSearchTools
{
    private readonly ILogger<McpWebSearchTools> _logger;
    private readonly HttpClient _httpClient;

    public McpWebSearchTools(
        ILogger<McpWebSearchTools> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("WebSearch");
    }

    /// <summary>
    /// Web'de arama yapar ve sonuçları döner.
    /// </summary>
    [McpServerTool(Name = "web_search")]
    [Description("Web'de arama yapar. Araştırma konusuyla ilgili güncel bilgileri toplar. Sonuçlar başlık, URL, snippet ve kaynak bilgisi içerir.")]
    public Task<string> SearchAsync(
        [Description("Arama sorgusu (örn: 'Python 3.13 yenilikleri')")] string query)
    {
        _logger.LogInformation("[MCP:WebSearch] Arama yapılıyor: '{Query}'", query);

        var results = GenerateSearchResults(query);

        var output = $"Web Arama Sonuçları: '{query}'\n" +
                     $"Arama Zamanı: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\n" +
                     $"Bulunan Sonuç: {results.Count}\n\n" +
                     string.Join("\n\n", results.Select((r, i) =>
                         $"{i + 1}. {r.Title}\n   URL: {r.Url}\n   Kaynak: {r.Source}\n   Özet: {r.Snippet}"));

        _logger.LogInformation("[MCP:WebSearch] {Count} sonuç bulundu: '{Query}'",
            results.Count, query);

        return Task.FromResult(output);
    }

    /// <summary>
    /// Belirtilen URL'nin içeriğini çeker.
    /// </summary>
    [McpServerTool(Name = "fetch_url_content")]
    [Description("Belirtilen URL'nin içeriğini çeker. Web sayfasının metin içeriğini döner.")]
    public async Task<string> FetchUrlContentAsync(
        [Description("İçeriği çekilecek URL")] string url)
    {
        try
        {
            _logger.LogInformation("[MCP:WebSearch] URL içeriği çekiliyor: {Url}", url);
            var content = await _httpClient.GetStringAsync(url);

            // İçeriği makul bir boyutta döndür
            if (content.Length > 5000)
                content = content[..5000] + "\n\n[...içerik kısaltıldı...]";

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MCP:WebSearch] URL içeriği çekme hatası: {Url}", url);
            return $"URL içeriği çekilemedi ({url}): {ex.Message}";
        }
    }

    /// <summary>
    /// Demo amaçlı simüle edilmiş arama sonuçları üretir.
    /// Gerçek projede burada Bing Search API veya Google Custom Search kullanılır.
    /// </summary>
    private List<SearchResult> GenerateSearchResults(string query)
    {
        return new List<SearchResult>
        {
            new()
            {
                Title = $"{query} - Kapsamlı Rehber ve Analiz",
                Url = $"https://docs.example.com/{Uri.EscapeDataString(query)}",
                Snippet = $"Bu kapsamlı rehber, {query} konusunu tüm yönleriyle ele almaktadır. " +
                          "En güncel bilgiler ve profesyonel analizler bu kaynakta bulunmaktadır.",
                Source = "docs.example.com"
            },
            new()
            {
                Title = $"{query} - Son Gelişmeler 2026",
                Url = $"https://blog.example.com/{Uri.EscapeDataString(query)}-2026",
                Snippet = $"2026 yılında {query} alanındaki en son gelişmeler, yenilikler ve " +
                          "değişiklikler hakkında detaylı bilgi.",
                Source = "blog.example.com"
            },
            new()
            {
                Title = $"{query} - Resmi Dokümantasyon",
                Url = $"https://official.example.com/{Uri.EscapeDataString(query)}",
                Snippet = $"Resmi {query} dokümantasyonu. Kurulum, yapılandırma ve " +
                          "kullanım kılavuzu bu kaynakta yer almaktadır.",
                Source = "official.example.com"
            },
            new()
            {
                Title = $"{query} - Topluluk Tartışmaları ve Görüşler",
                Url = $"https://forum.example.com/topic/{Uri.EscapeDataString(query)}",
                Snippet = $"Geliştiricilerin {query} hakkındaki deneyimleri, karşılaştıkları " +
                          "sorunlar ve çözüm önerileri.",
                Source = "forum.example.com"
            },
            new()
            {
                Title = $"{query} - Karşılaştırmalı Analiz ve Benchmark",
                Url = $"https://analysis.example.com/{Uri.EscapeDataString(query)}",
                Snippet = $"{query} konusunda detaylı karşılaştırma, performans testleri " +
                          "ve benchmark sonuçları.",
                Source = "analysis.example.com"
            }
        };
    }

    private class SearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }
}
