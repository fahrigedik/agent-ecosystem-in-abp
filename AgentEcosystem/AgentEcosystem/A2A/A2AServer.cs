using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// A2A NuGet paketi (global::A2A) ile proje namespace'i (AgentEcosystem.A2A) çakışır.
// Bu yüzden NuGet paketindeki tiplere global:: ile erişiyoruz.
using A2AAgentCard = global::A2A.AgentCard;
using A2AAgentTask = global::A2A.AgentTask;
using A2AAgentTaskStatus = global::A2A.AgentTaskStatus;
using A2AAgentMessage = global::A2A.AgentMessage;
using A2AAgentCapabilities = global::A2A.AgentCapabilities;
using A2AAgentSkill = global::A2A.AgentSkill;
using A2ATaskState = global::A2A.TaskState;
using A2AMessageRole = global::A2A.MessageRole;
using A2ATextPart = global::A2A.TextPart;
using A2AArtifact = global::A2A.Artifact;
using A2APart = global::A2A.Part;

namespace AgentEcosystem.A2A;

/// <summary>
/// A2A Sunucu — Agent-to-Agent protokolünün sunucu tarafı.
/// 
/// Google'ın A2A protokolü, ajanların birbirlerini keşfetmesini (Agent Card),
/// görev göndermesini (Task) ve sonuç almasını sağlar.
/// 
/// Bu sınıf, A2A sunucusuna gelen görevleri yöneten merkezi bileşendir.
/// Her ajan kendi Agent Card'ını yayınlar ve görev işleyicisini kaydeder.
/// 
/// A2A Protokol Akışı:
/// 1. İstemci → GET /.well-known/agent.json → Agent Card alır
/// 2. İstemci → POST /tasks/send → Görev gönderir  
/// 3. Sunucu → Görevi işler → Sonuç döner (artifacts)
/// </summary>
public class A2AServer
{
    private readonly ILogger<A2AServer> _logger;
    private readonly Dictionary<string, Func<A2AAgentTask, CancellationToken, Task<A2AAgentTask>>> _taskHandlers = new();

    public A2AServer(ILogger<A2AServer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Görev işleyicisi kaydeder. Her ajan kendi handler'ını kaydeder.
    /// </summary>
    public void RegisterTaskHandler(
        string agentId,
        Func<A2AAgentTask, CancellationToken, Task<A2AAgentTask>> handler)
    {
        _taskHandlers[agentId] = handler;
        _logger.LogInformation("[A2A:Server] Görev işleyicisi kaydedildi: {AgentId}", agentId);
    }

    /// <summary>
    /// Gelen görevi uygun işleyiciye yönlendirir.
    /// A2A protokolünün tasks/send endpoint'inin karşılığı.
    /// </summary>
    public async Task<A2AAgentTask> HandleTaskAsync(
        string agentId,
        A2AAgentTask task,
        CancellationToken cancellationToken = default)
    {
        if (!_taskHandlers.TryGetValue(agentId, out var handler))
        {
            _logger.LogWarning("[A2A:Server] İşleyici bulunamadı: {AgentId}", agentId);
            task.Status = new A2AAgentTaskStatus
            {
                State = A2ATaskState.Failed,
                Timestamp = DateTimeOffset.UtcNow,
                Message = new A2AAgentMessage
                {
                    Role = A2AMessageRole.Agent,
                    MessageId = Guid.NewGuid().ToString(),
                    Parts = new List<A2APart> { new A2ATextPart { Text = $"Ajan bulunamadı: {agentId}" } }
                }
            };
            return task;
        }

        _logger.LogInformation("[A2A:Server] Görev işleniyor: {TaskId} → {AgentId}",
            task.Id, agentId);

        try
        {
            task.Status = new A2AAgentTaskStatus
            {
                State = A2ATaskState.Working,
                Timestamp = DateTimeOffset.UtcNow
            };

            var result = await handler(task, cancellationToken);

            result.Status = new A2AAgentTaskStatus
            {
                State = A2ATaskState.Completed,
                Timestamp = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("[A2A:Server] Görev tamamlandı: {TaskId}", task.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A2A:Server] Görev başarısız: {TaskId}", task.Id);
            task.Status = new A2AAgentTaskStatus
            {
                State = A2ATaskState.Failed,
                Timestamp = DateTimeOffset.UtcNow,
                Message = new A2AAgentMessage
                {
                    Role = A2AMessageRole.Agent,
                    MessageId = Guid.NewGuid().ToString(),
                    Parts = new List<A2APart> { new A2ATextPart { Text = $"Hata: {ex.Message}" } }
                }
            };
            return task;
        }
    }

    /// <summary>
    /// Araştırmacı Ajan için Agent Card oluşturur.
    /// A2A protokolünde /.well-known/agent.json endpoint'inden sunulur.
    /// </summary>
    public static A2AAgentCard CreateResearcherAgentCard() => new()
    {
        Name = "Araştırmacı Ajan",
        Description = "Web'de arama yaparak bilgi toplayan araştırma ajanı.",
        Url = "https://localhost:44331/a2a/researcher",
        Version = "1.0.0",
        Capabilities = new A2AAgentCapabilities
        {
            Streaming = false,
            PushNotifications = false
        },
        Skills = new List<A2AAgentSkill>
        {
            new()
            {
                Id = "web-research",
                Name = "Web Araştırması",
                Description = "Belirtilen konuda web'de arama yapar ve ham veri toplar.",
                Tags = new List<string> { "research", "web-search", "data-collection" }
            }
        }
    };

    /// <summary>
    /// Analiz Ajanı için Agent Card oluşturur.
    /// </summary>
    public static A2AAgentCard CreateAnalysisAgentCard() => new()
    {
        Name = "Analiz Ajanı",
        Description = "Ham verileri analiz ederek yapılandırılmış sonuçlar üreten analiz ajanı.",
        Url = "https://localhost:44331/a2a/analyst",
        Version = "1.0.0",
        Capabilities = new A2AAgentCapabilities
        {
            Streaming = false,
            PushNotifications = false
        },
        Skills = new List<A2AAgentSkill>
        {
            new()
            {
                Id = "data-analysis",
                Name = "Veri Analizi",
                Description = "Ham araştırma verilerini analiz eder, özetler ve yapılandırılmış formatta sunar.",
                Tags = new List<string> { "analysis", "summarization", "structuring" }
            }
        }
    };
}
