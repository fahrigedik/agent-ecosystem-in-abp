using System;
using System.Collections.Generic;

namespace AgentEcosystem.Services.Dtos;

/// <summary>
/// Araştırma isteği DTO'su.
/// API'ye gelen araştırma talebinin veri modeli.
/// </summary>
public class ResearchRequestDto
{
    /// <summary>
    /// Araştırma sorgusu.
    /// Örn: "Python'un son sürümündeki yenilikler nedir?"
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Çalışma modu: "sequential" (ADK) veya "a2a" (A2A protokolü).
    /// Varsayılan: sequential
    /// </summary>
    public string Mode { get; set; } = "sequential";
}

/// <summary>
/// Araştırma sonucu DTO'su.
/// Tüm pipeline çıktısını içerir.
/// </summary>
public class ResearchResultDto
{
    /// <summary>Oturum kimliği.</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Orijinal araştırma sorgusu.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>Ham web arama sonuçları.</summary>
    public string RawSearchResults { get; set; } = string.Empty;

    /// <summary>Araştırmacı Ajan'ın ürettiği ham araştırma raporu.</summary>
    public string ResearchReport { get; set; } = string.Empty;

    /// <summary>Analiz Ajanı'nın ürettiği final analiz raporu.</summary>
    public string AnalysisResult { get; set; } = string.Empty;

    /// <summary>Raporun kaydedildiği dosya adı.</summary>
    public string SavedFileName { get; set; } = string.Empty;

    /// <summary>İşlem durumu: Completed, Failed.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Toplam işlem süresi (ms).</summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>Pipeline sırasında oluşan ajan olayları.</summary>
    public List<AgentEventDto> AgentEvents { get; set; } = new();
}

/// <summary>
/// Ajan olay DTO'su.
/// Pipeline'daki her ajanın ürettiği olayları temsil eder.
/// </summary>
public class AgentEventDto
{
    /// <summary>Olayı oluşturan ajan adı.</summary>
    public string Agent { get; set; } = string.Empty;

    /// <summary>Olay durumu: working, completed, failed.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Olay zamanı.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>İçerik önizlemesi (max 200 karakter).</summary>
    public string ContentPreview { get; set; } = string.Empty;
}

/// <summary>
/// Araştırma listesi DTO'su (geçmiş araştırmalar).
/// </summary>
public class ResearchSummaryDto
{
    public Guid Id { get; set; }
    public string Query { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public long? ProcessingTimeMs { get; set; }
    public string ResultPreview { get; set; } = string.Empty;
}
