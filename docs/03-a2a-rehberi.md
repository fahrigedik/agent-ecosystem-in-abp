# A2A (Agent-to-Agent Protocol) — Derinlemesine Rehber

> Bu rehber, A2A protokolünün ne olduğunu, temel kavramlarını, SDK tiplerini ve projemizdeki kullanımını detaylı şekilde anlatır.

---

## İçindekiler

1. [A2A Nedir?](#1-a2a-nedir)
2. [A2A vs MCP — Fark Nedir?](#2-a2a-vs-mcp--fark-nedir)
3. [A2A Protokol Akışı](#3-a2a-protokol-akışı)
4. [A2A SDK Tipleri (.NET)](#4-a2a-sdk-tipleri-net)
5. [Agent Card — Ajanın Kimlik Kartı](#5-agent-card--ajanın-kimlik-kartı)
6. [AgentTask — Görev Yaşam Döngüsü](#6-agenttask--görev-yaşam-döngüsü)
7. [Projemizdeki A2A Uygulaması](#7-projemizdeki-a2a-uygulaması)
8. [Namespace Çakışması ve Çözümü](#8-namespace-çakışması-ve-çözümü)
9. [A2A ile Dağıtık Ajan Mimarisi](#9-a2a-ile-dağıtık-ajan-mimarisi)
10. [Kaynaklar](#10-kaynaklar)

---

## 1. A2A Nedir?

**A2A (Agent-to-Agent)**, Google tarafından 2024'te önerilen ve artık Linux Foundation bünyesinde geliştirilen açık bir protokoldür. Amacı: **Farklı ajanların birbirlerini keşfetmesi, birbirleriyle iletişim kurması ve görev alışverişi yapması**.

### Problemi Anlayalım

Bir şirketin farklı departmanlarında farklı AI ajanları çalıştığını düşünün:
- **HR Ajanı**: Çalışan bilgilerini yönetir
- **Finans Ajanı**: Bütçe ve harcamaları kontrol eder
- **IT Ajanı**: Teknik destekle ilgilenir

Bu ajanların birbirleriyle konuşması gerekiyor:
- HR Ajanı → Finans Ajanı: "Bu çalışanın maaş bilgisi?"
- IT Ajanı → HR Ajanı: "Yeni çalışanın bilgisayar ihtiyacı?"
- Finans Ajanı → IT Ajanı: "Yazılım lisansı maliyeti?"

**A2A olmadan**: Her ajan çifti için özel entegrasyon kodu yazılır (N×N problem)  
**A2A ile**: Tek standart protokol, her ajan otomatik olarak diğerleriyle konuşabilir

```
A2A Olmadan (N×N):              A2A ile (N×1):
HR ←→ Finans                    HR ──┐
HR ←→ IT                        Finans ──┼── A2A Protokolü
Finans ←→ IT                    IT ──┘
(3 özel entegrasyon)            (3 standart endpoint)
```

---

## 2. A2A vs MCP — Fark Nedir?

Bu soruyu en çok duyarsınız. İkisi **farklı problemleri** çözer:

| Özellik | MCP | A2A |
|---------|-----|-----|
| **Taraflar** | LLM ↔ Araç | Ajan ↔ Ajan |
| **Amaç** | LLM'e yetenek kazandırma | Ajanlar arası iletişim |
| **Keşif** | tools/list | Agent Card (/.well-known/agent.json) |
| **İletişim** | tools/call (fonksiyon çağrısı) | tasks/send (görev gönderme) |
| **Sonuç** | Text/Object dönüşü | Artifact (çıktı parçası) |
| **Kim geliştirdi?** | Anthropic | Google / Linux Foundation |
| **Analoji** | "LLM'in eli" | "Ajanların telefonu" |

```
┌─────────────────────────────────────────────────┐
│              Ajan Ekosistemi                     │
│                                                  │
│  ┌──────────┐   A2A   ┌──────────┐              │
│  │  Ajan 1  │ ◄─────► │  Ajan 2  │              │
│  │          │         │          │              │
│  │  MCP ↕   │         │  MCP ↕   │              │
│  │┌────────┐│         │┌────────┐│              │
│  ││ Araç 1 ││         ││ Araç A ││              │
│  ││ Araç 2 ││         ││ Araç B ││              │
│  │└────────┘│         │└────────┘│              │
│  └──────────┘         └──────────┘              │
│                                                  │
│  MCP = "Ne yapabilir?" (araçlar)                │
│  A2A = "Nasıl konuşur?" (iletişim)             │
└─────────────────────────────────────────────────┘
```

**Birlikte kullanılırlar**: 
- MCP → Ajanın kendi yeteneklerini tanımlar (hangi araçları kullanabilir)
- A2A → Ajanların birbirleriyle nasıl iletişim kurduğunu tanımlar

---

## 3. A2A Protokol Akışı

A2A'nın üç temel adımı vardır:

### Adım 1: Keşif (Discovery)

Bir ajan, diğer ajanları keşfetmek için `/.well-known/agent.json` endpoint'ine HTTP GET isteği gönderir:

```
GET https://analyst-agent.example.com/.well-known/agent.json

Response:
{
  "name": "Analiz Ajanı",
  "description": "Ham verileri analiz eder",
  "url": "https://analyst-agent.example.com",
  "version": "1.0.0",
  "capabilities": {
    "streaming": false,
    "pushNotifications": false
  },
  "skills": [
    {
      "id": "data-analysis",
      "name": "Veri Analizi",
      "description": "Ham verileri analiz edip rapor üretir",
      "tags": ["analysis", "summarization"]
    }
  ]
}
```

Bu **Agent Card** sayesinde:
- Ajanın ne yaptığını öğrenirsiniz
- Hangi becerilere sahip olduğunu görürsünüz
- İletişim URL'sini alırsınız

### Adım 2: Görev Gönderme (Task Execution)

Keşiften sonra, ajana görev gönderilir:

```
POST https://analyst-agent.example.com/tasks/send

Request:
{
  "id": "task-001",
  "contextId": "session-abc",
  "history": [
    {
      "role": "user",
      "messageId": "msg-001",
      "parts": [
        { "type": "text", "text": "Bu verileri analiz et: ..." }
      ]
    }
  ]
}
```

### Adım 3: Sonuç Alma (Result)

Ajan görevi tamamlayınca sonuç döner:

```json
{
  "id": "task-001",
  "contextId": "session-abc",
  "status": {
    "state": "completed",
    "timestamp": "2025-06-10T14:30:00Z"
  },
  "artifacts": [
    {
      "artifactId": "art-001",
      "name": "analysis_report",
      "parts": [
        { "type": "text", "text": "# Analiz Raporu\n..." }
      ]
    }
  ]
}
```

### Tam Akış Diyagramı

```
İstemci Ajan                            Hedef Ajan
     │                                       │
     │  GET /.well-known/agent.json          │
     │ ─────────────────────────────────────► │
     │  ◄─── Agent Card (yetenekler)         │
     │                                       │
     │  POST /tasks/send                     │
     │  { id, history: [...] }               │
     │ ─────────────────────────────────────► │
     │                                       │ Görev işleniyor...
     │                                       │  └─ MCP araçlarını kullan
     │                                       │  └─ LLM ile analiz et
     │  ◄─── { status: completed,            │
     │         artifacts: [...] }             │
     │                                       │
```

---

## 4. A2A SDK Tipleri (.NET)

`A2A` NuGet paketi (v0.3.3-preview) aşağıdaki tipleri sağlar. Tümü `A2A` namespace'indedir:

### 4.1 AgentCard

Ajanın kimlik kartı — kim olduğunu, ne yaptığını ve yeteneklerini tanımlar.

```csharp
namespace A2A;

public sealed class AgentCard
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public string Version { get; set; }
    public string ProtocolVersion { get; set; }
    public AgentCapabilities Capabilities { get; set; }
    public List<string> DefaultInputModes { get; set; }
    public List<string> DefaultOutputModes { get; set; }
    public List<AgentSkill> Skills { get; set; }
    public string PreferredTransport { get; set; }
}

public class AgentCapabilities
{
    public bool Streaming { get; set; }
    public bool PushNotifications { get; set; }
    public bool StateTransitionHistory { get; set; }
}

public class AgentSkill
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Tags { get; set; }
}
```

### 4.2 AgentTask

Ajanlar arası görev birimi — bir iş parçacığını ve yaşam döngüsünü temsil eder.

```csharp
public sealed class AgentTask : A2AResponse
{
    public string Id { get; set; }
    public string ContextId { get; set; }
    public AgentTaskStatus Status { get; set; }
    public List<Artifact> Artifacts { get; set; }
    public List<AgentMessage> History { get; set; }
}
```

### 4.3 AgentTaskStatus

Görevin anlık durumunu temsil eden yapı.

```csharp
public struct AgentTaskStatus
{
    public TaskState State { get; set; }
    public AgentMessage? Message { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public enum TaskState
{
    Submitted,        // Gönderildi, henüz işlenmedi
    Working,          // İşleniyor
    InputRequired,    // Ek bilgi gerekli
    Completed,        // Tamamlandı
    Canceled,         // İptal edildi
    Failed,           // Başarısız
    Rejected,         // Reddedildi
    AuthRequired,     // Yetkilendirme gerekli
    Unknown           // Bilinmiyor
}
```

**Görev Yaşam Döngüsü:**
```
Submitted → Working → Completed
                   → Failed
                   → Canceled
                   → InputRequired → Working → Completed
```

### 4.4 AgentMessage

Ajanlar arası mesaj — görev geçmişini oluşturur.

```csharp
public sealed class AgentMessage : A2AResponse
{
    public MessageRole Role { get; set; }
    public List<Part> Parts { get; set; }
    public string MessageId { get; set; }
    public string TaskId { get; set; }
    public string ContextId { get; set; }
}

public enum MessageRole
{
    User,    // Görevi gönderen taraf
    Agent    // Görevi işleyen ajan
}
```

### 4.5 Part ve TextPart

Mesaj parçaları — metin, dosya, veri vb. içerebilir.

```csharp
public abstract class Part { }

public class TextPart : Part
{
    public string Text { get; set; }
}

// Gelecekte: FilePart, DataPart, ImagePart vb.
```

### 4.6 Artifact

Görev çıktısı — ajanın ürettiği sonuç parçaları.

```csharp
public class Artifact
{
    public string ArtifactId { get; set; }  // Benzersiz kimlik (zorunlu)
    public string Name { get; set; }
    public string Description { get; set; }
    public List<Part> Parts { get; set; }
}
```

### 4.7 Tip İlişkileri Diyagramı

```
AgentCard           AgentTask              AgentMessage
├── Name            ├── Id                 ├── Role (User/Agent)
├── Description     ├── ContextId          ├── Parts[]
├── Url             ├── Status             │   └── TextPart
├── Version         │   ├── State          │       └── Text
├── Capabilities    │   ├── Message?       ├── MessageId
│   ├── Streaming   │   └── Timestamp      ├── TaskId
│   └── PushNotif   ├── Artifacts[]        └── ContextId
└── Skills[]        │   ├── ArtifactId
    ├── Id          │   ├── Name
    ├── Name        │   └── Parts[]
    └── Tags[]      └── History[]
                        └── AgentMessage[]
```

---

## 5. Agent Card — Ajanın Kimlik Kartı

### Projemizdeki Agent Card'lar

#### Araştırmacı Ajan

```csharp
public static AgentCard CreateResearcherAgentCard() => new()
{
    Name = "Araştırmacı Ajan",
    Description = "Web'de arama yaparak bilgi toplayan araştırma ajanı.",
    Url = "https://localhost:44331/a2a/researcher",
    Version = "1.0.0",
    Capabilities = new AgentCapabilities
    {
        Streaming = false,
        PushNotifications = false
    },
    Skills = new List<AgentSkill>
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
```

#### Analiz Ajanı

```csharp
public static AgentCard CreateAnalysisAgentCard() => new()
{
    Name = "Analiz Ajanı",
    Description = "Ham verileri analiz ederek yapılandırılmış sonuçlar üreten analiz ajanı.",
    Url = "https://localhost:44331/a2a/analyst",
    Version = "1.0.0",
    Capabilities = new AgentCapabilities
    {
        Streaming = false,
        PushNotifications = false
    },
    Skills = new List<AgentSkill>
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
```

### Agent Card Tasarım İlkeleri

1. **Açık isimler**: Ajanın ne yaptığını bir cümlede anlatın
2. **Detaylı beceriler**: Her skill'in açıklaması LLM tarafından okunabilir olmalı
3. **Doğru tag'ler**: Ajanı keşif sırasında filtrelemek için kullanılır
4. **Versiyon yönetimi**: Ajanın API versiyonunu belirtin

---

## 6. AgentTask — Görev Yaşam Döngüsü

### Görev Durumları (TaskState)

```
┌───────────┐
│ Submitted │──────┐
└───────────┘      │
                   ▼
             ┌──────────┐
             │  Working  │──────┬──────────┬───────────┐
             └──────────┘      │          │           │
                   │           ▼          ▼           ▼
                   │     ┌──────────┐ ┌────────┐ ┌──────────┐
                   │     │Completed │ │ Failed │ │ Canceled │
                   │     └──────────┘ └────────┘ └──────────┘
                   │
                   ▼
          ┌───────────────┐
          │InputRequired  │
          │(ek bilgi iste)│
          └───────┬───────┘
                  │ Kullanıcı yanıtı
                  ▼
             ┌──────────┐
             │  Working  │ → ...
             └──────────┘
```

### Görev Oluşturma

```csharp
var task = new AgentTask
{
    Id = Guid.NewGuid().ToString(),       // Benzersiz görev kimliği
    ContextId = sessionId,                 // Oturum/bağlam kimliği
    History = new List<AgentMessage>        // Mesaj geçmişi
    {
        new()
        {
            Role = MessageRole.User,       // Görevi kim gönderdi
            MessageId = Guid.NewGuid().ToString(),
            Parts = new List<Part>         // Mesaj parçaları
            {
                new TextPart { Text = "Python 3.13 yenilikleri nedir?" }
            }
        }
    }
};
```

### Görev Sonucu

```csharp
// Görev tamamlandığında:
task.Status = new AgentTaskStatus
{
    State = TaskState.Completed,
    Timestamp = DateTimeOffset.UtcNow
};

task.Artifacts = new List<Artifact>
{
    new()
    {
        ArtifactId = Guid.NewGuid().ToString(),  // Zorunlu!
        Name = "research_report",
        Parts = new List<Part>
        {
            new TextPart { Text = "# Analiz Raporu\n..." }
        }
    }
};
```

---

## 7. Projemizdeki A2A Uygulaması

### A2AServer — Merkezi Görev Yönlendirici

```csharp
public class A2AServer
{
    // Ajan ID → Handler eşlemesi
    private readonly Dictionary<string, 
        Func<AgentTask, CancellationToken, Task<AgentTask>>> _taskHandlers;

    // Handler kaydetme
    public void RegisterTaskHandler(string agentId, 
        Func<AgentTask, CancellationToken, Task<AgentTask>> handler)
    {
        _taskHandlers[agentId] = handler;
    }

    // Görev yönlendirme
    public async Task<AgentTask> HandleTaskAsync(
        string agentId, AgentTask task, CancellationToken ct)
    {
        if (!_taskHandlers.TryGetValue(agentId, out var handler))
        {
            task.Status = new AgentTaskStatus 
            { 
                State = TaskState.Failed,
                Message = new AgentMessage 
                {
                    Role = MessageRole.Agent,
                    Parts = new() { new TextPart { Text = $"Ajan bulunamadı: {agentId}" } }
                }
            };
            return task;
        }

        task.Status = new AgentTaskStatus { State = TaskState.Working };
        var result = await handler(task, ct);
        result.Status = new AgentTaskStatus { State = TaskState.Completed };
        
        return result;
    }
}
```

### Orkestratörde Handler Kaydı

```csharp
// ResearchOrchestrator constructor'ında:
_a2aServer.RegisterTaskHandler("researcher", async (task, ct) =>
{
    var context = new AgentContext
    {
        UserQuery = ExtractQueryFromTask(task)  // Task mesajından sorguyu çıkar
    };

    var result = await _researcherAgent.RunAsync(context, ct);

    // Sonucu A2A Artifact olarak ekle
    task.Artifacts = new List<Artifact>
    {
        new()
        {
            ArtifactId = Guid.NewGuid().ToString(),
            Name = "research_report",
            Parts = new List<Part>
            {
                new TextPart { Text = result.Content }
            }
        }
    };

    return task;
});

_a2aServer.RegisterTaskHandler("analyst", async (task, ct) =>
{
    // Benzer şekilde...
});
```

### A2A Modunda Araştırma Akışı

```csharp
public async Task<ResearchResultDto> ExecuteResearchViaA2AAsync(string query)
{
    // ADIM 1: Araştırmacı'ya görev gönder
    var researchTask = new AgentTask
    {
        Id = Guid.NewGuid().ToString(),
        ContextId = sessionId,
        History = new()
        {
            new() { Role = MessageRole.User, Parts = new() { new TextPart { Text = query } } }
        }
    };
    
    var researchResult = await _a2aServer.HandleTaskAsync("researcher", researchTask);
    var researchReport = ExtractTextFromArtifacts(researchResult);

    // ADIM 2: Analiz'e araştırma sonucuyla birlikte görev gönder
    var analysisTask = new AgentTask
    {
        Id = Guid.NewGuid().ToString(),
        ContextId = sessionId,
        History = new()
        {
            // User mesajı: orijinal sorgu
            new() { Role = MessageRole.User, Parts = new() { new TextPart { Text = query } } },
            // Agent mesajı: araştırma sonucu (önceki ajanın çıktısı)
            new() { Role = MessageRole.Agent, Parts = new() { new TextPart { Text = researchReport } } }
        }
    };
    
    var analysisResult = await _a2aServer.HandleTaskAsync("analyst", analysisTask);
    // ...
}
```

---

## 8. Namespace Çakışması ve Çözümü

### Problem

Projemizin namespace yapısı:
- `AgentEcosystem.A2A` → A2AServer sınıfımız burada
- `A2A` → NuGet paketinin tipleri burada (AgentCard, AgentTask...)

C# derleyicisi `AgentEcosystem.A2A` namespace'indeyken `A2A.AgentCard` yazdığımızda, önce `AgentEcosystem.A2A` altında `AgentCard`'ı arar ve bulamaz.

### Çözüm: Global Using Alias

```csharp
// Dosyanın başında:
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

// Artık:
// A2AAgentCard  → global::A2A.AgentCard (NuGet paketi)
// AgentCard     → AgentEcosystem.A2A.AgentCard (varsa, yerel tip)
```

`global::` prefix'i, C#'ın namespace arama hiyerarşisini bypass eder ve doğrudan **global namespace kökünden** aramaya başlar.

### Alternatif Çözümler

1. **Proje namespace'ini değiştirin**: `AgentEcosystem.A2A` → `AgentEcosystem.AgentProtocol`
2. **NuGet paketine alias verin**: `.csproj`'de `<Aliases>A2ASdk</Aliases>`
3. **Global usings kullanın**: `GlobalUsings.cs`'e ekleyin

---

## 9. A2A ile Dağıtık Ajan Mimarisi

### Mevcut Durum: In-Process

Şu an tüm ajanlar aynı process içinde çalışıyor. A2A sunucusuna doğrudan method çağrısı yapılıyor.

```
┌──────────────────────────────────────┐
│           Tek Process                │
│                                      │
│  Orchestrator → A2AServer            │
│       ↓              ↓               │
│  ResearcherAgent  AnalysisAgent      │
└──────────────────────────────────────┘
```

### Gelecek: Dağıtık Ajanlar

A2A'nın asıl gücü **dağıtık** senaryolarda ortaya çıkar:

```
┌─────────────────┐    HTTP     ┌─────────────────┐
│  Service A       │ ◄────────► │  Service B       │
│                  │  A2A       │                  │
│ ResearcherAgent  │            │ AnalysisAgent    │
│ (Port: 5001)     │            │ (Port: 5002)     │
│                  │            │                  │
│ Agent Card:      │            │ Agent Card:      │
│ /well-known/     │            │ /well-known/     │
│ agent.json       │            │ agent.json       │
└─────────────────┘            └─────────────────┘
```

### ASP.NET Core Endpoint'leri

A2A.AspNetCore paketi ile HTTP endpoint'leri kolayca eklenebilir:

```csharp
// Program.cs
app.MapA2AEndpoints(options =>
{
    options.AgentCard = A2AServer.CreateResearcherAgentCard();
    options.TaskHandler = async (task, ct) =>
    {
        // Görev işle
        return processedTask;
    };
});
```

Bu, otomatik olarak şu endpoint'leri oluşturur:
- `GET /.well-known/agent.json` → Agent Card
- `POST /tasks/send` → Görev gönderme
- `GET /tasks/{id}` → Görev durumu
- `POST /tasks/{id}/cancel` → Görev iptali

---

## 10. Kaynaklar

- [A2A Spesifikasyonu — Google](https://google.github.io/A2A/)
- [A2A GitHub Repository](https://github.com/google/A2A)
- [A2A .NET SDK](https://github.com/a2aprotocol/a2a-dotnet)
- [NuGet: A2A](https://www.nuget.org/packages/A2A)
- [A2A Linux Foundation Duyurusu](https://www.linuxfoundation.org/press/linux-foundation-launches-open-source-a2a-protocol)

---

*Bu rehber, Agent Ecosystem projesinin A2A öğretici dokümantasyonudur.*  
*Ana makale: [01-makale-agent-ekosistemi.md](01-makale-agent-ekosistemi.md)*
