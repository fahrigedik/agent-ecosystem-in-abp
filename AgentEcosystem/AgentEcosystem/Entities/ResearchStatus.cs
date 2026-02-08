namespace AgentEcosystem.Entities;

/// <summary>
/// Araştırma durumu enum'ı.
/// Bir araştırmanın yaşam döngüsündeki durumları temsil eder.
/// </summary>
public enum ResearchStatus
{
    /// <summary>Araştırma henüz başlamadı.</summary>
    Pending = 0,

    /// <summary>Araştırmacı Ajan veri topluyor.</summary>
    Researching = 1,

    /// <summary>Analiz Ajanı verileri analiz ediyor.</summary>
    Analyzing = 2,

    /// <summary>Araştırma başarıyla tamamlandı.</summary>
    Completed = 3,

    /// <summary>Araştırma sırasında hata oluştu.</summary>
    Failed = 4
}
