# .NET Core'da Agent Ekosistemi: A2A + MCP + ADK ile Uçtan Uca Entegrasyon

> **Yazar:** Fahri Gedik  
> **Tarih:** Haziran 2025  
> **Teknolojiler:** .NET 10, ABP Framework 10.0.2, OpenAI GPT, A2A, MCP, ADK  
> **NuGet Paketleri:** `A2A 0.3.3-preview`, `ModelContextProtocol 0.8.0-preview.1`, `Microsoft.Extensions.AI 10.2.0`, `OpenAI 2.8.0`

---

## İçindekiler

1. [Giriş: Neden Çoklu Ajan?](#1-giriş-neden-çoklu-ajan)
2. [Üç Protokol, Bir Ekosistem](#2-üç-protokol-bir-ekosistem)
   - [MCP — Model Context Protocol](#21-mcp--model-context-protocol)
   - [A2A — Agent-to-Agent Protocol](#22-a2a--agent-to-agent-protocol)
   - [ADK — Agent Development Kit](#23-adk--agent-development-kit)
3. [Mimari Genel Bakış](#3-mimari-genel-bakış)
4. [Proje Yapısı](#4-proje-yapısı)
5. [Uygulama Detayları](#5-uygulama-detayları)
   - [5.1 ADK Core — Ajan Altyapısı](#51-adk-core--ajan-altyapısı)
   - [5.2 MCP Tools — Araç Katmanı](#52-mcp-tools--araç-katmanı)
   - [5.3 A2A Server — Ajan İletişimi](#53-a2a-server--ajan-iletişimi)
   - [5.4 GPT Entegrasyonu](#54-gpt-entegrasyonu)
   - [5.5 Orkestratör — Her Şeyi Birleştiren](#55-orkestratör--her-şeyi-birleştiren)
6. [DI (Dependency Injection) Yapılandırması](#6-di-dependency-injection-yapılandırması)
7. [Veri Akış Şeması](#7-veri-akış-şeması)
8. [API Endpoint'leri](#8-api-endpointleri)
9. [Kurulum ve Çalıştırma](#9-kurulum-ve-çalıştırma)
10. [Sonuç](#10-sonuç)

---

## 1. Giriş: Neden Çoklu Ajan?

Yapay zeka uygulamalarında tek bir monolitik LLM çağrısı, basit sorular için yeterli olsa da karmaşık iş akışlarında yetersiz kalır. **Çoklu ajan mimarileri** bu sorunu çözer:

| Problem | Tek LLM Çağrısı | Çoklu Ajan |
|---------|-----------------|------------|
| Uzun araştırmalar | Sınırlı context window | Her ajan kendi alanında uzman |
| Dış kaynak erişimi | LLM doğrudan erişemez | MCP araçları ile erişim |
| İş akışı yönetimi | Tek seferde tüm iş | ADK ile aşamalı pipeline |
| Ajan keşfi ve iletişimi | Hardcoded bağımlılıklar | A2A ile dinamik keşif |

Bu projede bir **"Çoklu Ajan Araştırma Asistanı"** oluşturuyoruz. İki ajan birlikte çalışarak kullanıcının araştırma sorgusunu alır, web'de arama yapar, sonuçları analiz eder ve yapılandırılmış bir rapor üretir:

```
Kullanıcı: "Python 3.13 yenilikleri nedir?"
    │
    ▼
ResearcherAgent (MCP:WebSearch + GPT) → ham araştırma raporu
    │
    ▼ (ADK State / A2A Task)
AnalysisAgent (GPT + MCP:File + MCP:DB) → yapılandırılmış analiz
    │
    ▼
Final Rapor (Markdown dosya + Veritabanı kaydı)
```

---

## 2. Üç Protokol, Bir Ekosistem

Bu projede üç farklı protokolü bir arada kullanıyoruz. Her birinin çözdüğü problem farklıdır ve **birbirlerini tamamlarlar**:

```
┌──────────────────────────────────────────────────────────┐
│                    AGENT ECOSYSTEM                        │
│                                                          │
│  ┌──────────┐    ┌──────────┐    ┌──────────────────┐   │
│  │   ADK     │    │   A2A    │    │      MCP         │   │
│  │ Orkestra  │◄──►│ İletişim │◄──►│   Araç Erişimi   │   │
│  │ Pipeline  │    │ Protokol │    │   (DB, File,     │   │
│  │ Yönetimi  │    │          │    │    Web Search)    │   │
│  └──────────┘    └──────────┘    └──────────────────┘   │
│       │                                    │             │
│       ▼                                    ▼             │
│  ┌──────────┐                      ┌──────────────┐     │
│  │   GPT    │                      │  ABP / EF    │     │
│  │  (LLM)   │                      │  Core / SQL  │     │
│  └──────────┘                      └──────────────┘     │
└──────────────────────────────────────────────────────────┘
```

### 2.1 MCP — Model Context Protocol

**MCP** (Model Context Protocol), Anthropic tarafından geliştirilen açık bir protokoldür. Amacı: **LLM'lerin dış dünya ile etkileşim kurmasını standardize etmek**.

LLM'ler metin üretir ama veritabanına yazamaz, web'de arama yapamaz, dosya oluşturamaz. MCP bu boşluğu dolduran **"USB-C for AI"** olarak tanımlanır:

```
┌─────────────┐     MCP Protokolü     ┌──────────────────┐
│             │ ◄──────────────────► │   MCP Server      │
│   LLM /     │   tools/list          │   ┌────────────┐ │
│   Ajan      │   tools/call          │   │ web_search │ │
│             │   resources/read      │   │ save_file  │ │
│             │                       │   │ query_db   │ │
└─────────────┘                       │   └────────────┘ │
                                      └──────────────────┘
```

**Temel kavramlar:**

| Kavram | Açıklama | Projemizdeki Karşılık |
|--------|----------|----------------------|
| **Tool** | LLM'in çağırabileceği fonksiyon | `web_search`, `save_research_to_database`, `save_research_to_file` |
| **Resource** | Salt okunur veri kaynağı | Araştırma dosyaları |
| **Prompt** | Hazır prompt şablonu | (Bu projede kullanılmadı) |
| **Server** | Araçları sunan sunucu | `McpWebSearchTools`, `McpDatabaseTools`, `McpFileSystemTools` |

**NuGet paketi:**
```xml
<PackageReference Include="ModelContextProtocol" Version="0.8.0-preview.1" />
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.8.0-preview.1" />
```

> Detaylı MCP öğreticisi için bkz: [02-mcp-rehberi.md](02-mcp-rehberi.md)

---

### 2.2 A2A — Agent-to-Agent Protocol

**A2A** (Agent-to-Agent), Google tarafından önerilen ve Linux Foundation bünyesinde geliştirilen açık bir protokoldür. Amacı: **Farklı ajanların birbirlerini keşfetmesi ve görev alışverişi yapması**.

MCP, ajan ile araçlar arasındaki iletişimi çözerken, A2A **ajan ile ajan** arasındaki iletişimi çözer:

```
┌──────────────┐     A2A Protokolü      ┌──────────────┐
│ Araştırmacı  │ ◄────────────────────► │   Analiz     │
│    Ajan      │                        │    Ajanı     │
│              │   1. Agent Card keşfi  │              │
│              │   2. tasks/send        │              │
│              │   3. Artifact dönüşü   │              │
└──────────────┘                        └──────────────┘
```

**A2A Protokol Akışı:**
1. **Keşif:** `GET /.well-known/agent.json` → Agent Card alır
2. **Görev Gönderme:** `POST /tasks/send` → AgentTask gönderir
3. **Sonuç Alma:** AgentTask → Artifacts ile sonuç döner

**NuGet paketi:**
```xml
<PackageReference Include="A2A" Version="0.3.3-preview" />
<PackageReference Include="A2A.AspNetCore" Version="0.3.3-preview" />
```

> Detaylı A2A öğreticisi için bkz: [03-a2a-rehberi.md](03-a2a-rehberi.md)

---

### 2.3 ADK — Agent Development Kit

**ADK** (Agent Development Kit), Google'ın ajan geliştirme çatısıdır. Python'da `google-adk` paketi olarak sunulur; .NET'te henüz resmi SDK yoktur. Bu projede **ADK'nın kavramsal yapısını .NET'e uyarladık**.

**ADK kavram eşleştirmesi:**

| ADK (Python) | .NET Karşılığı | Açıklama |
|-------------|----------------|----------|
| `BaseAgent` | `BaseAgent` | Tüm ajanların temel sınıfı |
| `SequentialAgent` | `SequentialAgent` | Ajanları sıralı çalıştırır |
| `ParallelAgent` | `ParallelAgent` | Ajanları paralel çalıştırır |
| `LlmAgent` | `ResearcherAgent` / `AnalysisAgent` | LLM kullanan ajan |
| `InvocationContext` | `AgentContext` | Çalışma bağlamı (state, events) |
| `Event` | `AgentEvent` | Olay (author, status, content) |
| `session.state` | `AgentContext.State` | Paylaşımlı durum deposu |

> Detaylı ADK öğreticisi için bkz: [04-adk-rehberi.md](04-adk-rehberi.md)

---

## 3. Mimari Genel Bakış

```
                        ┌─────────────────────┐
                        │   Kullanıcı / API    │
                        │  POST /api/app/      │
                        │  research/execute     │
                        └──────────┬──────────┘
                                   │
                        ┌──────────▼──────────┐
                        │ ResearchAppService   │
                        │ (ABP Auto API)       │
                        └──────────┬──────────┘
                                   │
                    ┌──────────────▼──────────────┐
                    │    ResearchOrchestrator      │
                    │  ┌────────────────────────┐ │
                    │  │  ADK SequentialAgent    │ │
                    │  │  ┌──────┐  ┌────────┐  │ │
                    │  │  │Agent1│→ │Agent2  │  │ │
                    │  │  └──┬───┘  └───┬────┘  │ │
                    │  └─────┼──────────┼───────┘ │
                    └────────┼──────────┼─────────┘
                             │          │
              ┌──────────────▼──┐   ┌───▼──────────────┐
              │ ResearcherAgent │   │  AnalysisAgent    │
              │ ┌─────────────┐ │   │ ┌──────────────┐ │
              │ │  IChatClient│ │   │ │  IChatClient  │ │
              │ │  (GPT)      │ │   │ │  (GPT)        │ │
              │ └──────┬──────┘ │   │ └──────┬────────┘ │
              │        │        │   │        │          │
              │ ┌──────▼──────┐ │   │ ┌──────▼────────┐ │
              │ │MCP:WebSearch│ │   │ │MCP:FileSystem │ │
              │ └─────────────┘ │   │ │MCP:Database   │ │
              └─────────────────┘   │ └───────────────┘ │
                                    └───────────────────┘
```

**Katmanlar:**
1. **API Layer** — ABP AppService → otomatik REST controller
2. **Orchestration Layer** — ResearchOrchestrator, ADK SequentialAgent
3. **Agent Layer** — ResearcherAgent, AnalysisAgent (GPT + MCP)
4. **Communication Layer** — A2A Server (görev yönetimi)
5. **Tool Layer** — MCP araçları (WebSearch, FileSystem, Database)
6. **Data Layer** — EF Core, ABP Repository pattern, ResearchRecord entity

---

## 4. Proje Yapısı

```
AgentEcosystem/
├── Agents/
│   ├── Core/                    # ADK çekirdeği
│   │   ├── BaseAgent.cs         # Temel ajan sınıfı
│   │   ├── SequentialAgent.cs   # Sıralı çalıştırıcı
│   │   ├── ParallelAgent.cs     # Paralel çalıştırıcı
│   │   ├── AgentContext.cs      # Çalışma bağlamı (state)
│   │   └── AgentEvent.cs        # Olay modeli
│   ├── ResearcherAgent.cs       # GPT + MCP:WebSearch ajanı
│   ├── AnalysisAgent.cs         # GPT + MCP:File+DB ajanı
│   ├── ResearchOrchestrator.cs  # Merkezi orkestratör (ADK+A2A+MCP)
│   └── SimulatedChatClient.cs   # Demo IChatClient (API key yoksa)
│
├── A2A/
│   └── A2AServer.cs             # A2A görev sunucusu (resmi SDK)
│
├── McpTools/
│   ├── McpWebSearchTools.cs     # [McpServerToolType] Web arama
│   ├── McpFileSystemTools.cs    # [McpServerToolType] Dosya işlemleri
│   └── McpDatabaseTools.cs      # [McpServerToolType] DB işlemleri
│
├── Entities/
│   ├── ResearchRecord.cs        # Araştırma entity'si (FullAuditedAggregateRoot)
│   └── ResearchStatus.cs        # Durum enum'u (Pending→Completed)
│
├── Services/
│   ├── ResearchAppService.cs    # API endpoint'leri (ABP Auto API)
│   └── Dtos/
│       └── ResearchDtos.cs      # Request/Response DTO'ları
│
├── Data/
│   └── AgentEcosystemDbContext.cs  # EF Core DbContext + ResearchRecords
│
└── AgentEcosystemModule.cs      # ABP DI yapılandırması
```

---

## 5. Uygulama Detayları

### 5.1 ADK Core — Ajan Altyapısı

ADK'nın .NET uyarlamasının temelini oluşturan 5 sınıf:

#### BaseAgent — Her Şeyin Temeli

```csharp
public abstract class BaseAgent
{
    public string Name { get; set; }
    public string Description { get; set; }
    public BaseAgent? ParentAgent { get; set; }
    public List<BaseAgent> SubAgents { get; set; } = new();

    // Her ajan bu metodu implement eder
    public abstract Task<AgentEvent> RunAsync(
        AgentContext context, CancellationToken ct = default);

    // Hiyerarşide ajan bulma
    public BaseAgent? FindAgent(string name);
    
    // Alt ajan ekleme (parent ilişkisi kurulur)
    public void AddSubAgent(BaseAgent agent);
}
```

Neden abstract? Her ajan kendi iş mantığını `RunAsync` içinde tanımlar. `BaseAgent` ortak davranışları (hiyerarşi, keşif) sunar.

#### AgentContext — Paylaşımlı Durum Deposu

```csharp
public class AgentContext
{
    public string SessionId { get; set; }
    public string UserQuery { get; set; }
    public Dictionary<string, object> State { get; set; } = new();
    public List<AgentEvent> Events { get; set; } = new();

    public T? GetState<T>(string key);
    public void SetState(string key, object value);
    public bool HasState(string key);
}
```

State mekanizması ADK'daki `session.state`'in karşılığıdır. Ajanlar bu depo üzerinden veri paylaşır:

```
ResearcherAgent → state["research_report"] = "..."   (yazar)
AnalysisAgent   → state["research_report"]            (okur)
```

#### SequentialAgent — Pipeline Pattern

```csharp
public class SequentialAgent : BaseAgent
{
    public override async Task<AgentEvent> RunAsync(AgentContext context, ...)
    {
        foreach (var subAgent in SubAgents)
        {
            var event = await subAgent.RunAsync(context);
            
            // State güncellemelerini uygula
            if (event.Actions?.StateUpdates != null)
                foreach (var (key, value) in event.Actions.StateUpdates)
                    context.SetState(key, value);
            
            // Escalate: Erken çıkış
            if (event.Actions?.Escalate == true) break;
            
            // Transfer: Başka ajana yönlendirme
            if (!string.IsNullOrEmpty(event.Actions?.TransferToAgent))
            {
                var target = FindAgent(event.Actions.TransferToAgent);
                if (target != null) await target.RunAsync(context);
            }
        }
    }
}
```

Önemli özellikler:
- **State propagation**: Her ajanın state güncellemeleri bir sonrakine aktarılır
- **Escalate**: Alt ajan "işim bitti" diyebilir, pipeline erken sonlanır
- **Transfer**: Bir ajan, işi başka bir ajana devredebilir (LLM-driven routing)

---

### 5.2 MCP Tools — Araç Katmanı

MCP araçları, ajanların dış dünyayla etkileşimini sağlar. Üç araç sınıfı tanımladık:

#### McpWebSearchTools — Web Araması

```csharp
[McpServerToolType]
public class McpWebSearchTools
{
    [McpServerTool(Name = "web_search")]
    [Description("Web'de arama yapar. Sonuçlar başlık, URL ve snippet içerir.")]
    public Task<string> SearchAsync(
        [Description("Arama sorgusu")] string query)
    {
        // Web araması yap, sonuçları formatla
        var results = GenerateSearchResults(query);
        return Task.FromResult(FormatResults(results));
    }

    [McpServerTool(Name = "fetch_url_content")]
    [Description("URL içeriğini çeker.")]
    public async Task<string> FetchUrlContentAsync(
        [Description("URL")] string url)
    {
        return await _httpClient.GetStringAsync(url);
    }
}
```

> **Demo modu:** Şu an simüle edilmiş sonuçlar dönülüyor. Üretim ortamında Bing Search API veya Google Custom Search API key'i ile gerçek arama yapılabilir.

#### McpDatabaseTools — Veritabanı İşlemleri

```csharp
[McpServerToolType]
public class McpDatabaseTools
{
    private readonly IRepository<ResearchRecord, Guid> _repository;

    [McpServerTool(Name = "save_research_to_database")]
    [Description("Araştırma sonucunu veritabanına kaydeder.")]
    public async Task<string> SaveResearchAsync(
        [Description("Sorgu")] string query,
        [Description("Ham veri")] string rawData,
        [Description("Analiz sonucu")] string analyzedResult,
        [Description("Kaynaklar")] string sources)
    {
        var record = new ResearchRecord { Query = query, RawData = rawData, ... };
        var saved = await _repository.InsertAsync(record, autoSave: true);
        return $"Kaydedildi. ID: {saved.Id}";
    }

    [McpServerTool(Name = "search_past_research")]
    public async Task<string> SearchPastResearchAsync(
        [Description("Anahtar kelime")] string keyword);

    [McpServerTool(Name = "get_recent_research")]
    public async Task<string> GetRecentResearchAsync(
        [Description("Adet")] int count = 5);
}
```

ABP'nin **Repository pattern** kullandığımıza dikkat edin.

#### McpFileSystemTools — Dosya İşlemleri

```csharp
[McpServerToolType]
public class McpFileSystemTools
{
    [McpServerTool(Name = "save_research_to_file")]
    public async Task<string> SaveResearchToFileAsync(
        [Description("Dosya adı")] string fileName,
        [Description("İçerik")] string content);

    [McpServerTool(Name = "read_research_file")]
    public async Task<string> ReadResearchFileAsync(
        [Description("Dosya adı")] string fileName);

    [McpServerTool(Name = "list_research_files")]
    public Task<string> ListResearchFilesAsync();
}
```

---

### 5.3 A2A Server — Ajan İletişimi

A2A sunucusu, ajanlar arası görev yönlendirmesini yönetir. Resmi `A2A` NuGet paketindeki tipleri kullanır.

**Namespace çakışması sorunu:** Projemizin namespace'i `AgentEcosystem.A2A`, NuGet paketinin namespace'i de `A2A`. Çözüm olarak **using alias** kullanıyoruz:

```csharp
using A2AAgentCard = global::A2A.AgentCard;
using A2AAgentTask = global::A2A.AgentTask;
using A2ATaskState = global::A2A.TaskState;
using A2AAgentMessage = global::A2A.AgentMessage;
using A2AMessageRole = global::A2A.MessageRole;
using A2ATextPart = global::A2A.TextPart;
using A2AArtifact = global::A2A.Artifact;
```

```csharp
public class A2AServer
{
    private readonly Dictionary<string, Func<AgentTask, CancellationToken, Task<AgentTask>>> 
        _taskHandlers = new();

    // Handler kaydetme
    public void RegisterTaskHandler(string agentId, 
        Func<AgentTask, CancellationToken, Task<AgentTask>> handler);

    // Görev yönlendirme (tasks/send karşılığı)
    public async Task<AgentTask> HandleTaskAsync(
        string agentId, AgentTask task, CancellationToken ct);

    // Agent Card oluşturma (/.well-known/agent.json karşılığı)
    public static AgentCard CreateResearcherAgentCard();
    public static AgentCard CreateAnalysisAgentCard();
}
```

---

### 5.4 GPT Entegrasyonu

GPT entegrasyonu `Microsoft.Extensions.AI` abstraction'ı üzerinden yapılır:

```csharp
// IChatClient — evrensel LLM arayüzü
services.AddSingleton<IChatClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["OpenAI:ApiKey"];

    if (!string.IsNullOrEmpty(apiKey) && apiKey != "YOUR_OPENAI_API_KEY_HERE")
    {
        // Gerçek OpenAI bağlantısı
        var client = new OpenAIClient(apiKey);
        var model = config["OpenAI:Model"] ?? "gpt-4o";
        return client.GetChatClient(model).AsIChatClient();
    }
    
    // API key yoksa demo mod
    return new SimulatedChatClient();
});
```

**Neden `IChatClient`?** Bu arayüz `Microsoft.Extensions.AI`'dan gelir ve **tüm LLM sağlayıcılarını** birleştirir:
- OpenAI GPT → `OpenAIClient.GetChatClient().AsIChatClient()`
- Azure OpenAI → Aynı arayüz
- Ollama → Aynı arayüz

LLM değiştirmek sadece DI kaydını değiştirmek kadar kolaydır.

---

### 5.5 Orkestratör — Her Şeyi Birleştiren

`ResearchOrchestrator`, tüm protokolleri bir araya getirir ve iki farklı mod sunar:

#### Mod 1: ADK SequentialAgent Pattern

```csharp
public async Task<ResearchResultDto> ExecuteResearchAsync(string query)
{
    var pipeline = new SequentialAgent
    {
        Name = "ResearchPipeline",
        Description = "Araştırma → Analiz pipeline'ı"
    };
    
    pipeline.AddSubAgent(_researcherAgent);  // 1. adım
    pipeline.AddSubAgent(_analysisAgent);     // 2. adım

    var context = new AgentContext
    {
        SessionId = Guid.NewGuid().ToString(),
        UserQuery = query
    };

    var result = await pipeline.RunAsync(context);
    return MapToDto(result, context);
}
```

#### Mod 2: A2A Protokolü

```csharp
public async Task<ResearchResultDto> ExecuteResearchViaA2AAsync(string query)
{
    // 1. Araştırmacı Ajan'a A2A Task gönder
    var researchTask = new AgentTask
    {
        Id = Guid.NewGuid().ToString(),
        ContextId = sessionId,
        History = new List<AgentMessage>
        {
            new() { Role = MessageRole.User, Parts = new() { new TextPart { Text = query } } }
        }
    };
    
    var researchResult = await _a2aServer.HandleTaskAsync("researcher", researchTask);
    
    // 2. Analiz Ajanı'na A2A Task gönder (araştırma sonucuyla)
    var analysisTask = new AgentTask
    {
        History = new() {
            new() { Role = MessageRole.User, Parts = ... },
            new() { Role = MessageRole.Agent, Parts = ... }  // Araştırma sonucu
        }
    };
    
    var analysisResult = await _a2aServer.HandleTaskAsync("analyst", analysisTask);
    return MapToDto(analysisResult);
}
```

**İki mod arasındaki fark:**
- **Sequential (ADK)**: State üzerinden veri paylaşımı, tek process içinde, basit ve hızlı
- **A2A**: Mesaj tabanlı iletişim, protokol standartlarına uygun, potansiyel olarak farklı servislerde çalışabilir

---

## 6. DI (Dependency Injection) Yapılandırması

Tüm bileşenler `AgentEcosystemModule.ConfigureAgentEcosystem()` metodunda kaydedilir:

```csharp
private void ConfigureAgentEcosystem(ServiceConfigurationContext context)
{
    var services = context.Services;
    var config = services.GetConfiguration();

    // ─── 1. IChatClient (GPT veya Simülasyon) ───
    services.AddSingleton<IChatClient>(sp => {
        var apiKey = config["OpenAI:ApiKey"];
        if (IsValidApiKey(apiKey))
        {
            var client = new OpenAIClient(apiKey);
            return client.GetChatClient(config["OpenAI:Model"] ?? "gpt-4o")
                         .AsIChatClient();
        }
        return new SimulatedChatClient();
    });

    // ─── 2. HttpClient (Web arama için) ───
    services.AddHttpClient("WebSearch", client => {
        client.DefaultRequestHeaders.Add("User-Agent", "AgentEcosystem/1.0");
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    // ─── 3. MCP Araçları ───
    services.AddTransient<McpWebSearchTools>();
    services.AddTransient<McpFileSystemTools>();
    services.AddTransient<McpDatabaseTools>();

    // ─── 4. A2A Server ───
    services.AddSingleton<A2AServer>();

    // ─── 5. Ajanlar ───
    services.AddTransient<ResearcherAgent>();
    services.AddTransient<AnalysisAgent>();

    // ─── 6. Orkestratör ───
    services.AddTransient<ResearchOrchestrator>();
}
```

---

## 7. Veri Akış Şeması

```
Kullanıcı: "Python 3.13 yenilikleri nedir?"
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ ResearchOrchestrator.ExecuteResearchAsync("Python 3.13...")  │
│                                                              │
│ ADK SequentialAgent Pipeline:                                │
│                                                              │
│  ┌───────────[ ResearcherAgent ]─────────────┐              │
│  │                                            │              │
│  │  1. MCP:WebSearch("Python 3.13...")         │              │
│  │     → 5 arama sonucu döner                 │              │
│  │                                            │              │
│  │  2. IChatClient.GetResponseAsync(           │              │
│  │       system: "Sen araştırmacısın...",      │              │
│  │       user: sonuçlar + soru)                │              │
│  │     → Araştırma raporu                     │              │
│  │                                            │              │
│  │  3. context.SetState("research_report", r)  │              │
│  │     context.SetState("search_results", s)   │              │
│  └────────────────┬───────────────────────────┘              │
│                   │ State aktarımı                            │
│  ┌────────────────▼──[ AnalysisAgent ]────────┐              │
│  │                                            │              │
│  │  1. context.GetState("research_report")     │              │
│  │                                            │              │
│  │  2. IChatClient.GetResponseAsync(           │              │
│  │       system: "Sen analiz ajanısın...",     │              │
│  │       user: rapor + ham veri)               │              │
│  │     → Yapılandırılmış Markdown rapor       │              │
│  │                                            │              │
│  │  3. MCP:FileSystem.SaveAsync(dosyaAdı, r)  │              │
│  │     MCP:Database.SaveAsync(query, veri, r)  │              │
│  │                                            │              │
│  │  4. EventActions.Escalate = true            │              │
│  └────────────────────────────────────────────┘              │
│                                                              │
│ → ResearchResultDto döner                                    │
└──────────────────────────────────────────────────────────────┘
```

---

## 8. API Endpoint'leri

ABP'nin **Auto API Controller** özelliği sayesinde `ResearchAppService` otomatik olarak REST endpoint'lerine dönüşür:

| HTTP | Endpoint | Açıklama |
|------|----------|----------|
| `POST` | `/api/app/research/execute` | Yeni araştırma başlat |
| `GET` | `/api/app/research/history` | Geçmiş araştırmaları listele |
| `GET` | `/api/app/research/{id}` | Belirli araştırmayı getir |
| `GET` | `/api/app/research/agent-cards` | A2A Agent Card'larını getir |

#### Örnek İstek — Sequential Mod

```bash
curl -X POST https://localhost:44331/api/app/research/execute \
  -H "Content-Type: application/json" \
  -d '{"query": "Python 3.13 yenilikleri", "mode": "sequential"}'
```

#### Örnek İstek — A2A Mod

```bash
curl -X POST https://localhost:44331/api/app/research/execute \
  -H "Content-Type: application/json" \
  -d '{"query": "Python 3.13 yenilikleri", "mode": "a2a"}'
```

#### Örnek Yanıt

```json
{
  "sessionId": "abc-123-def",
  "query": "Python 3.13 yenilikleri",
  "rawSearchResults": "Web Arama Sonuçları: 'Python 3.13...'",
  "researchReport": "## Araştırma Raporu\n...",
  "analysisResult": "# Python 3.13 Yenilikleri\n## Yönetici Özeti\n...",
  "savedFileName": "python-3.13-yenilikleri-20250610-143022.md",
  "status": "Completed",
  "processingTimeMs": 3450,
  "agentEvents": [
    {
      "agent": "ResearcherAgent",
      "status": "completed",
      "timestamp": "2025-06-10T14:30:19Z",
      "contentPreview": "## Araştırma Raporu..."
    },
    {
      "agent": "AnalysisAgent",
      "status": "completed",
      "timestamp": "2025-06-10T14:30:22Z",
      "contentPreview": "# Python 3.13 Yenilikleri..."
    }
  ]
}
```

---

## 9. Kurulum ve Çalıştırma

### Ön Gereksinimler

- .NET 10 SDK
- SQL Server (LocalDB yeterli)
- OpenAI API anahtarı (isteğe bağlı — yoksa demo mod çalışır)

### Adımlar

```bash
# 1. Bağımlılıkları geri yükle
dotnet restore

# 2. Veritabanını oluştur ve migrate et
dotnet run -- --migrate-database

# 3. (İsteğe bağlı) OpenAI API key'i ayarla
# appsettings.json → "OpenAI" → "ApiKey" değerini güncelleyin

# 4. Uygulamayı çalıştır
dotnet run
```

### appsettings.json Yapılandırması

```json
{
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY_HERE",
    "Model": "gpt-4o"
  }
}
```

> **Demo Modu:** API anahtarı girilmezse `SimulatedChatClient` devreye girer ve GPT yerine simüle edilmiş yanıtlar üretir. Projeyi test etmek için OpenAI hesabına gerek yoktur.

### Kullanılan NuGet Paketleri

| Paket | Versiyon | Amaç |
|-------|---------|------|
| `OpenAI` | 2.8.0 | Resmi OpenAI C# SDK |
| `Microsoft.Extensions.AI` | 10.2.0 | IChatClient abstraction |
| `Microsoft.Extensions.AI.OpenAI` | 10.2.0-preview | OpenAI → IChatClient adapter |
| `ModelContextProtocol` | 0.8.0-preview.1 | MCP SDK (Anthropic resmi) |
| `ModelContextProtocol.AspNetCore` | 0.8.0-preview.1 | MCP ASP.NET Core entegrasyonu |
| `A2A` | 0.3.3-preview | A2A SDK (Google/Linux Foundation) |
| `A2A.AspNetCore` | 0.3.3-preview | A2A ASP.NET Core entegrasyonu |

---

## 10. Sonuç

Bu projede üç farklı ajan protokolünü tek bir .NET uygulamasında birleştirdik:

| Protokol | Çözdüğü Problem | Soru |
|----------|-----------------|------|
| **MCP** | LLM ↔ Araç iletişimi | "Ajan ne **yapabilir**?" |
| **A2A** | Ajan ↔ Ajan iletişimi | "Ajanlar nasıl **konuşur**?" |
| **ADK** | Ajan orkestrasyon | "Ajanlar nasıl **organize edilir**?" |

Bu üç protokol birbirini **tamamlar**, birbirine rakip değildir.

**Öğrenilen dersler:**
1. **MCP** araçları, LLM'lerin dış dünyayla etkileşimini standartlaştırır — `[McpServerToolType]` ve `[McpServerTool]` attribute'ları ile araçlar kolayca tanımlanır
2. **A2A** protokolü, ajanların birbirini keşfetmesini (Agent Card) ve görev alışverişi yapmasını (AgentTask) sağlar
3. **ADK** pattern'ları (Sequential, Parallel, State), karmaşık iş akışlarını yönetmeyi kolaylaştırır
4. **IChatClient** abstraction'ı, LLM sağlayıcısından bağımsız kod yazmayı mümkün kılar
5. **ABP Framework** ile entegrasyon, DI, Repository pattern ve Auto API Controller avantajları sunar

---

**Detaylı Rehberler:**
- [MCP Derinlemesine Rehber →](02-mcp-rehberi.md)
- [A2A Derinlemesine Rehber →](03-a2a-rehberi.md)
- [ADK Derinlemesine Rehber →](04-adk-rehberi.md)

---

*Bu makale, Agent Ecosystem projesinin dokümantasyonu olarak hazırlanmıştır.*
