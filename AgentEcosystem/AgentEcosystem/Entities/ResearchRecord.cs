using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace AgentEcosystem.Entities;

/// <summary>
/// Araştırma kaydı entity'si.
/// Her araştırma sorgusu ve sonucu bu tabloda saklanır.
/// ABP'nin FullAuditedAggregateRoot'unu kullanarak
/// oluşturma/güncelleme/silme audit bilgilerini otomatik tutar.
/// </summary>
public class ResearchRecord : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Kullanıcının sorduğu araştırma sorusu.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Araştırmacı Ajan tarafından toplanan ham veriler.
    /// Web araması sonuçları, kaynak metinleri vb.
    /// </summary>
    public string RawData { get; set; } = string.Empty;

    /// <summary>
    /// Analiz Ajanı tarafından üretilen analiz edilmiş sonuç.
    /// Yapılandırılmış, özetlenmiş final çıktı.
    /// </summary>
    public string AnalyzedResult { get; set; } = string.Empty;

    /// <summary>
    /// Araştırmada kullanılan kaynakların JSON listesi.
    /// </summary>
    public string Sources { get; set; } = string.Empty;

    /// <summary>
    /// Araştırma durumu.
    /// </summary>
    public ResearchStatus Status { get; set; } = ResearchStatus.Pending;

    /// <summary>
    /// Araştırmanın tamamlandığı zaman.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Araştırmayı başlatan oturum kimliği.
    /// A2A protokolünde task tracking için kullanılır.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// İşlem süresi (milisaniye).
    /// </summary>
    public long? ProcessingTimeMs { get; set; }
}
