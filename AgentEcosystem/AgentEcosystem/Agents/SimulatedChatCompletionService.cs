using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgentEcosystem.Agents;

/// <summary>
/// Simüle edilmiş IChatCompletionService — Semantic Kernel uyumlu.
/// 
/// API key olmadan demo/geliştirme modu sağlar.
/// Gerçek bir LLM yerine akıllı şablon yanıtlar üretir.
/// 
/// Üretim ortamında bu sınıf kullanılmaz; Semantic Kernel
/// Azure OpenAI veya OpenAI connector'ları yapılandırılır.
/// </summary>
public class SimulatedChatCompletionService : IChatCompletionService
{
    public IReadOnlyDictionary<string, object?> Attributes { get; } =
        new Dictionary<string, object?>
        {
            ["ModelId"] = "simulated-gpt",
            ["Provider"] = "SimulatedChatCompletionService"
        };

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var (isAnalysis, userQuery) = ExtractContext(chatHistory);

        string responseText = isAnalysis
            ? GenerateAnalysisResponse(userQuery)
            : GenerateResearchResponse(userQuery);

        IReadOnlyList<ChatMessageContent> result = new List<ChatMessageContent>
        {
            new(AuthorRole.Assistant, responseText)
        };

        return Task.FromResult(result);
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var result = await GetChatMessageContentsAsync(
            chatHistory, executionSettings, kernel, cancellationToken);
        var content = result.FirstOrDefault()?.Content ?? "";

        // Streaming simülasyonu: cümle cümle gönder
        foreach (var chunk in content.Split('\n'))
        {
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, chunk + "\n");
        }
    }

    private static (bool isAnalysis, string userQuery) ExtractContext(ChatHistory chatHistory)
    {
        var isAnalysis = false;
        var userQuery = "";

        foreach (var message in chatHistory)
        {
            if (message.Role == AuthorRole.System)
            {
                var systemText = message.Content ?? "";
                if (systemText.Contains("analiz", StringComparison.OrdinalIgnoreCase))
                    isAnalysis = true;
            }
            if (message.Role == AuthorRole.User)
            {
                userQuery = message.Content ?? "";
            }
        }

        return (isAnalysis, userQuery);
    }

    private static string GenerateResearchResponse(string query)
    {
        return $"""
            # Araştırma Raporu: {ExtractTopic(query)}

            ## Ana Bulgular

            1. **Güncel Durum**: Konu hakkında kapsamlı araştırma yapılmıştır. 
               Son gelişmelere göre önemli değişiklikler gözlemlenmiştir.

            2. **Teknik Detaylar**: Bu alandaki en son teknik gelişmeler, 
               performans iyileştirmeleri ve yeni özellikler raporlanmıştır.

            3. **Topluluk Görüşleri**: Geliştiriciler topluluğu genel olarak 
               pozitif geri bildirimler paylaşmıştır.

            4. **Karşılaştırma**: Alternatif çözümlerle karşılaştırıldığında 
               belirli avantajlar ve dezavantajlar tespit edilmiştir.

            5. **Gelecek Perspektifi**: Önümüzdeki dönemde daha fazla 
               geliştirme ve iyileştirme beklenmektedir.

            ## Kaynaklar
            - docs.example.com - Kapsamlı Rehber
            - blog.example.com - Son Gelişmeler
            - official.example.com - Resmi Dokümantasyon
            - forum.example.com - Topluluk Tartışmaları

            ## Not
            Bu rapor simüle edilmiş bir AI araştırmacı tarafından üretilmiştir.
            Gerçek sonuçlar için Azure OpenAI veya OpenAI API key'i yapılandırınız.
            """;
    }

    private static string GenerateAnalysisResponse(string query)
    {
        return $"""
            # {ExtractTopic(query)} — Analiz Raporu

            ## Yönetici Özeti
            Bu araştırma, "{ExtractTopic(query)}" konusu hakkında kapsamlı bir analiz sunmaktadır.
            Toplanan veriler yapılandırılmış bir formatta analiz edilmiş ve özetlenmiştir.

            ## Detaylı Analiz

            ### Mevcut Durum
            Araştırma verileri incelendiğinde, konunun aktif olarak geliştirildiği 
            ve topluluğun büyük ilgi gösterdiği görülmektedir.

            ### Güçlü Yönler
            - Kapsamlı dokümantasyon ve topluluk desteği
            - Aktif geliştirme ve sürekli iyileştirme
            - Geniş kullanım alanı ve ekosistem

            ### Dikkat Edilmesi Gerekenler
            - Bazı breaking change'ler için hazırlık yapılmalı
            - Performans etkilerini değerlendirmek önemli
            - Alternatif çözümlerle karşılaştırma yapılmalı

            ## Kaynaklar ve Referanslar
            1. Resmi Dokümantasyon
            2. Blog Yazıları ve Rehberler
            3. Topluluk Tartışmaları
            4. Performans Benchmark'ları

            ## Sonuç ve Değerlendirme
            Genel olarak, araştırılan konu aktif gelişim halindedir ve 
            profesyonel kullanım için uygun görülmektedir.

            ---
            *Bu rapor AI Agent Ecosystem (Demo Modu — Semantic Kernel) tarafından üretilmiştir.*
            *Araştırmacı Ajan → Analiz Ajanı pipeline'ı ile oluşturulmuştur.*
            """;
    }

    private static string ExtractTopic(string query)
    {
        if (string.IsNullOrEmpty(query)) return "Genel Araştırma";
        if (query.Contains("Araştırma Konusu:"))
        {
            var start = query.IndexOf("Araştırma Konusu:") + "Araştırma Konusu:".Length;
            var end = query.IndexOf('\n', start);
            if (end == -1) end = Math.Min(start + 100, query.Length);
            return query[start..end].Trim();
        }
        return query.Length > 100 ? query[..100] + "..." : query;
    }
}
