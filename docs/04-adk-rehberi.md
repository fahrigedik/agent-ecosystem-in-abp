# ADK (Agent Development Kit) — Derinlemesine Rehber

> Bu rehber, ADK'nın kavramsal yapısını, .NET uyarlamasını ve projemizdeki kullanımını detaylı şekilde anlatır.

---

## İçindekiler

1. [ADK Nedir?](#1-adk-nedir)
2. [ADK'nın Temel Kavramları](#2-adknın-temel-kavramları)
3. [ADK .NET Uyarlaması — Sınıf Yapısı](#3-adk-net-uyarlaması--sınıf-yapısı)
4. [BaseAgent — Her Şeyin Temeli](#4-baseagent--her-şeyin-temeli)
5. [AgentContext — Paylaşımlı Durum](#5-agentcontext--paylaşımlı-durum)
6. [AgentEvent ve EventActions](#6-agentevent-ve-eventactions)
7. [SequentialAgent — Pipeline Pattern](#7-sequentialagent--pipeline-pattern)
8. [ParallelAgent — Eşzamanlı Çalışma](#8-parallelagent--eşzamanlı-çalışma)
9. [LlmAgent Uygulaması (ResearcherAgent & AnalysisAgent)](#9-llmagent-uygulaması)
10. [Orkestrasyon Desenleri](#10-orkestrasyon-desenleri)
11. [ADK vs Diğer Çatılar](#11-adk-vs-diğer-çatılar)
12. [Kaynaklar](#12-kaynaklar)

---

## 1. ADK Nedir?

**ADK (Agent Development Kit)**, Google'ın ajan uygulamaları geliştirmek için sunduğu bir çatıdır. Python'da `google-adk` paketi olarak mevcuttur; .NET'te henüz resmi bir SDK **yoktur**.

Bu projede **ADK'nın kavramsal çerçevesini .NET'e uyarladık**. Python ADK'daki temel sınıfları ve pattern'ları C#'a çevirdik.

### ADK Ne Sağlar?

ADK, ajan uygulamalarında şu sorunları çözer:

1. **Ajan organizasyonu**: Birden fazla ajanı hiyerarşik yapıda düzenle
2. **İş akışı yönetimi**: Ajanları sıralı veya paralel çalıştır
3. **Durum yönetimi**: Ajanlar arası veri paylaşımını yönet
4. **Olay takibi**: Her ajanın ne yaptığını izle
5. **Kontrol akışı**: Escalate, transfer, erken çıkış

### ADK Olmadan vs ADK ile

```
ADK Olmadan:                        ADK ile:
if (task == "research")             var pipeline = new SequentialAgent();
{                                   pipeline.AddSubAgent(researcherAgent);
    var result1 = await             pipeline.AddSubAgent(analysisAgent);
        researcher.Run(query);      
    var result2 = await             var result = await pipeline.RunAsync(context);
        analyst.Run(result1);       // State otomatik aktarılır
    // Manuel state yönetimi        // Olay takibi otomatik
    // Manuel hata yönetimi         // Hata yönetimi dahili
    // Manuel olay takibi           
}
```

---

## 2. ADK'nın Temel Kavramları

### Kavram Haritası

```
┌─────────────────────────────────────────────────┐
│                    ADK                           │
│                                                  │
│  ┌──────────────────────────────────────────┐   │
│  │             Agent Hierarchy              │   │
│  │                                          │   │
│  │  SequentialAgent (Pipeline)              │   │
│  │  ├── ResearcherAgent (LlmAgent)          │   │
│  │  └── AnalysisAgent  (LlmAgent)           │   │
│  │                                          │   │
│  │  ParallelAgent (Concurrent)              │   │
│  │  ├── WebSearchAgent                      │   │
│  │  └── NewsSearchAgent                     │   │
│  └──────────────────────────────────────────┘   │
│                                                  │
│  ┌─────────────┐  ┌──────────┐  ┌──────────┐   │
│  │AgentContext  │  │AgentEvent│  │  State   │   │
│  │(Bağlam)     │  │(Olay)    │  │(Depo)    │   │
│  └─────────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────┘
```

### Python ADK → .NET Eşleştirmesi

| Python ADK | .NET Uygulaması | Dosya |
|------------|----------------|-------|
| `google.adk.agents.BaseAgent` | `BaseAgent` | `Agents/Core/BaseAgent.cs` |
| `google.adk.agents.SequentialAgent` | `SequentialAgent` | `Agents/Core/SequentialAgent.cs` |
| `google.adk.agents.ParallelAgent` | `ParallelAgent` | `Agents/Core/ParallelAgent.cs` |
| `google.adk.agents.LlmAgent` | `ResearcherAgent`, `AnalysisAgent` | `Agents/*.cs` |
| `google.adk.events.Event` | `AgentEvent` | `Agents/Core/AgentEvent.cs` |
| `google.adk.sessions.InvocationContext` | `AgentContext` | `Agents/Core/AgentContext.cs` |
| `session.state` | `AgentContext.State` | (Dictionary<string, object>) |
| `EventActions` | `EventActions` | `Agents/Core/AgentEvent.cs` |

---

## 3. ADK .NET Uyarlaması — Sınıf Yapısı

### Sınıf Diyagramı

```
                ┌────────────────┐
                │   BaseAgent    │ (abstract)
                │                │
                │ + Name         │
                │ + Description  │
                │ + ParentAgent  │
                │ + SubAgents    │
                │                │
                │ + RunAsync()   │ (abstract)
                │ + FindAgent()  │
                │ + AddSubAgent()│
                └───────┬────────┘
                        │
          ┌─────────────┼─────────────┐
          │             │             │
 ┌────────▼───────┐ ┌──▼──────────┐ ┌▼────────────────┐
 │SequentialAgent │ │ParallelAgent│ │ ResearcherAgent  │
 │                │ │             │ │ AnalysisAgent    │
 │ Sıralı çalış. │ │ Paralel çal.│ │ (LlmAgent impl.) │
 └────────────────┘ └─────────────┘ └─────────────────┘

 ┌────────────────┐  ┌────────────────┐
 │  AgentContext   │  │  AgentEvent    │
 │                │  │                │
 │ + SessionId    │  │ + Author       │
 │ + UserQuery    │  │ + Status       │
 │ + State (dict) │  │ + Content      │
 │ + Events (list)│  │ + Actions      │
 │                │  │   + Escalate   │
 │ + GetState<T>()│  │   + Transfer   │
 │ + SetState()   │  │   + StateUpd.  │
 └────────────────┘  └────────────────┘
```

---

## 4. BaseAgent — Her Şeyin Temeli

### Tanım

```csharp
public abstract class BaseAgent
{
    /// <summary>Ajanın benzersiz adı.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Ajanın açıklaması (LLM routing için kullanılabilir).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Ebeveyn ajan (hiyerarşi için).</summary>
    public BaseAgent? ParentAgent { get; set; }

    /// <summary>Alt ajanlar listesi.</summary>
    public List<BaseAgent> SubAgents { get; set; } = new();

    /// <summary>
    /// Ajanı çalıştırır — her ajan bu metodu implement eder.
    /// Python ADK'daki _run_async_impl'in karşılığı.
    /// </summary>
    public abstract Task<AgentEvent> RunAsync(
        AgentContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hiyerarşide isimle ajan bulur (recursive).
    /// </summary>
    public BaseAgent? FindAgent(string name);

    /// <summary>
    /// Alt ajan ekler ve ebeveyn ilişkisini kurar.
    /// Bir ajanın sadece bir ebeveyni olabilir.
    /// </summary>
    public void AddSubAgent(BaseAgent agent);
}
```

### Neden Abstract?

`BaseAgent` abstract'tır çünkü her ajan türü farklı davranır:
- `SequentialAgent` → Alt ajanları sırayla çalıştırır
- `ParallelAgent` → Alt ajanları aynı anda çalıştırır
- `ResearcherAgent` → GPT ile web araştırması yapar
- `AnalysisAgent` → GPT ile analiz yapar

Ortak olan:
- İsim ve açıklama
- Hiyerarşi yönetimi (parent/child)
- Ajan keşfi (FindAgent)

### Hiyerarşi Örneği

```csharp
var root = new SequentialAgent { Name = "Root" };

var researcher = new ResearcherAgent(...) { Name = "Researcher" };
var analyst = new AnalysisAgent(...) { Name = "Analyst" };

// Alt ajan ekle — parent otomatik ayarlanır
root.AddSubAgent(researcher);
root.AddSubAgent(analyst);

// Hiyerarşi:
// Root (SequentialAgent)
// ├── Researcher (ResearcherAgent) [ParentAgent = Root]
// └── Analyst (AnalysisAgent)      [ParentAgent = Root]

// Ajan bulma:
var found = root.FindAgent("Analyst");  // AnalysisAgent döner
```

---

## 5. AgentContext — Paylaşımlı Durum

### Tanım

```csharp
public class AgentContext
{
    /// <summary>Benzersiz oturum kimliği.</summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Kullanıcının orijinal sorgusu.</summary>
    public string UserQuery { get; set; } = string.Empty;

    /// <summary>
    /// Paylaşımlı durum deposu — ajanlar arası veri paylaşımı.
    /// Python ADK'daki session.state'in karşılığı.
    /// </summary>
    public Dictionary<string, object> State { get; set; } = new();

    /// <summary>Oturum boyunca oluşan olayların listesi.</summary>
    public List<AgentEvent> Events { get; set; } = new();

    /// <summary>State'ten tipli değer okur.</summary>
    public T? GetState<T>(string key);

    /// <summary>State'e değer yazar.</summary>
    public void SetState(string key, object value);

    /// <summary>Key var mı kontrol eder.</summary>
    public bool HasState(string key);
}
```

### State Mekanizması Derinlemesine

State, ADK'nın en güçlü özelliklerinden biridir. Ajanlar arası veri paylaşımını **bağlam nesnesini taşıyarak** sağlar:

```
Pipeline Başlangıcı:
  context.State = {} (boş)

ResearcherAgent çalışır:
  context.State = {
    "search_results": "Web sonuçları...",
    "research_report": "Araştırma raporu...",
    "research_query": "Python 3.13",
    "research_status": "completed"
  }

AnalysisAgent çalışır (State'i okur):
  var report = context.GetState<string>("research_report");  // "Araştırma raporu..."
  
  // Kendi sonuçlarını ekler:
  context.State = {
    "search_results": "...",
    "research_report": "...",
    "research_query": "...",
    "research_status": "completed",
    "analysis_result": "Final rapor...",      // ← Yeni
    "analysis_file": "python-report.md",       // ← Yeni
    "analysis_status": "completed"             // ← Yeni
  }
```

### State Convention'ları

| Key Pattern | Açıklama | Örnek |
|-------------|----------|-------|
| `{ajan}_result` | Ajanın ana çıktısı | `analysis_result` |
| `{ajan}_status` | Ajanın durumu | `research_status` |
| `{ajan}_query` | Ajanın aldığı sorgu | `research_query` |
| `{kaynak}_data` | Dış kaynak verisi | `search_results` |

### Tipli Okuma

```csharp
// Güvenli tipli okuma — null döner eğer key yoksa veya tip uyumsuzsa
var report = context.GetState<string>("research_report");  // string | null
var count = context.GetState<int>("result_count");          // int | null (0)
var list = context.GetState<List<string>>("sources");       // List<string> | null

// Kontrol
if (context.HasState("research_report"))
{
    var report = context.GetState<string>("research_report")!;
    // ...
}
```

---

## 6. AgentEvent ve EventActions

### AgentEvent

Her ajan `RunAsync`'ten bir `AgentEvent` döner. Bu olay:
- Kim tarafından üretildi (Author)
- İşlem durumu (Status: working, completed, failed)
- Üretilen içerik (Content)
- Yapılacak eylemler (Actions)

```csharp
public class AgentEvent
{
    public string Author { get; set; }     // Ajan adı
    public string Status { get; set; }     // "working" | "completed" | "failed"
    public string Content { get; set; }    // Üretilen metin
    public EventActions? Actions { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

### EventActions — Kontrol Akışı

```csharp
public class EventActions
{
    /// <summary>
    /// true → Pipeline'ı sonlandır (üst ajana bildir).
    /// AnalysisAgent bunu kullanır: "İşim bitti, pipeline tamamlandı."
    /// </summary>
    public bool Escalate { get; set; }

    /// <summary>
    /// Başka bir ajana transfer et.
    /// Örn: "Bu soruyu ben cevaplayamam, ExpertAgent'a sor."
    /// </summary>
    public string? TransferToAgent { get; set; }

    /// <summary>
    /// State güncellemeleri.
    /// SequentialAgent bu listeyi alıp context.State'e uygular.
    /// </summary>
    public Dictionary<string, object>? StateUpdates { get; set; }
}
```

### Kullanım Örneği

```csharp
// ResearcherAgent'ın dönüş değeri:
return new AgentEvent
{
    Author = "ResearcherAgent",
    Status = "completed",
    Content = researchReport,
    Actions = new EventActions
    {
        StateUpdates = new Dictionary<string, object>
        {
            ["search_results"] = searchResults,
            ["research_report"] = researchReport,
            ["research_query"] = query
        }
    }
};

// AnalysisAgent'ın dönüş değeri:
return new AgentEvent
{
    Author = "AnalysisAgent",
    Status = "completed",
    Content = analysisResult,
    Actions = new EventActions
    {
        StateUpdates = new Dictionary<string, object>
        {
            ["analysis_result"] = analysisResult,
            ["analysis_file"] = fileName
        },
        Escalate = true  // Pipeline tamamlandı!
    }
};
```

---

## 7. SequentialAgent — Pipeline Pattern

### Nasıl Çalışır?

SequentialAgent, alt ajanları **tanımlı sırada ardışık** olarak çalıştırır:

```
SequentialAgent.RunAsync(context)
│
├── SubAgents[0].RunAsync(context)  → AgentEvent
│   ├── State güncellemelerini uygula
│   ├── Escalate? → Erken çıkış
│   └── Transfer? → Hedef ajana git
│
├── SubAgents[1].RunAsync(context)  → AgentEvent
│   ├── State güncellemelerini uygula
│   └── ...
│
└── Final AgentEvent döner
```

### Implementasyon Detayı

```csharp
public class SequentialAgent : BaseAgent
{
    public override async Task<AgentEvent> RunAsync(
        AgentContext context, CancellationToken ct)
    {
        AgentEvent lastEvent = new() { Author = Name, Status = "working" };

        foreach (var subAgent in SubAgents)
        {
            ct.ThrowIfCancellationRequested();

            // 1. Alt ajanı çalıştır
            lastEvent = await subAgent.RunAsync(context, ct);
            context.Events.Add(lastEvent);

            // 2. State güncellemeleri
            if (lastEvent.Actions?.StateUpdates != null)
            {
                foreach (var (key, value) in lastEvent.Actions.StateUpdates)
                    context.SetState(key, value);
            }

            // 3. Escalate — pipeline'ı sonlandır
            if (lastEvent.Actions?.Escalate == true)
                break;

            // 4. Transfer — başka ajana yönlendir
            if (!string.IsNullOrEmpty(lastEvent.Actions?.TransferToAgent))
            {
                var target = FindAgent(lastEvent.Actions.TransferToAgent);
                if (target != null)
                {
                    lastEvent = await target.RunAsync(context, ct);
                    context.Events.Add(lastEvent);
                }
            }
        }

        return new AgentEvent
        {
            Author = Name,
            Status = "completed",
            Content = lastEvent.Content
        };
    }
}
```

### State Propagation Mekanizması

Her ajanın `Actions.StateUpdates` döndürdüğü değerler, `SequentialAgent` tarafından otomatik olarak `context.State`'e yazılır. Bu sayede bir sonraki ajan bu verilere erişebilir:

```
Sıra: ResearcherAgent → AnalysisAgent

1. ResearcherAgent.RunAsync(context)
   Dönüş: Actions.StateUpdates = { "research_report": "..." }
   
   SequentialAgent: context.SetState("research_report", "...")
   
2. AnalysisAgent.RunAsync(context)
   İçeride: context.GetState<string>("research_report")  ← Bunu okur!
```

---

## 8. ParallelAgent — Eşzamanlı Çalışma

### Nasıl Çalışır?

ParallelAgent, alt ajanları **aynı anda** çalıştırır (`Task.WhenAll`):

```
ParallelAgent.RunAsync(context)
│
├── Task.WhenAll(
│   SubAgents[0].RunAsync(context),  ← Parallel
│   SubAgents[1].RunAsync(context),  ← Parallel
│   SubAgents[2].RunAsync(context)   ← Parallel
│   )
│
├── Her sonucun State güncellemelerini uygula
│
└── Tüm sonuçları birleştirip döner
```

### Implementasyon

```csharp
public class ParallelAgent : BaseAgent
{
    public override async Task<AgentEvent> RunAsync(
        AgentContext context, CancellationToken ct)
    {
        // Tüm alt ajanları paralel çalıştır
        var tasks = SubAgents.Select(agent => agent.RunAsync(context, ct));
        var results = await Task.WhenAll(tasks);

        // Her sonucu events'e ekle ve state'i güncelle
        foreach (var result in results)
        {
            context.Events.Add(result);
            
            if (result.Actions?.StateUpdates != null)
                foreach (var (key, value) in result.Actions.StateUpdates)
                    context.SetState(key, value);
        }

        // Sonuçları birleştir
        return new AgentEvent
        {
            Author = Name,
            Status = "completed",
            Content = string.Join("\n\n---\n\n",
                results.Select(r => $"[{r.Author}]: {r.Content}"))
        };
    }
}
```

### Paralel Kullanım Senaryosu

```csharp
// Birden fazla kaynaktan aynı anda araştırma yap
var multiSearch = new ParallelAgent { Name = "MultiSearch" };

multiSearch.AddSubAgent(new WebSearchAgent { Name = "Web" });
multiSearch.AddSubAgent(new NewsSearchAgent { Name = "News" });
multiSearch.AddSubAgent(new AcademicSearchAgent { Name = "Academic" });

var result = await multiSearch.RunAsync(context);
// 3 kaynak aynı anda aranır, sonuçlar birleştirilir
```

### ⚠️ State Çakışması Uyarısı

Paralel ajanlar aynı `AgentContext`'i paylaşır. Aynı state key'ine yazarlarsa **son yazan kazanır** (race condition):

```
❌ Kötü: Her iki ajan da "result" key'ine yazıyor
  Agent1 → state["result"] = "A"
  Agent2 → state["result"] = "B"  // A'yı ezer!

✅ İyi: Her ajan kendi key'ine yazıyor
  Agent1 → state["web_result"] = "A"
  Agent2 → state["news_result"] = "B"
```

---

## 9. LlmAgent Uygulaması

### ResearcherAgent — Araştırmacı

```csharp
public class ResearcherAgent : BaseAgent
{
    private readonly IChatClient _chatClient;      // GPT bağlantısı
    private readonly McpWebSearchTools _webSearch;  // MCP araç
    
    public override async Task<AgentEvent> RunAsync(AgentContext context, ...)
    {
        // ADIM 1: MCP aracını kullan
        var searchResults = await _webSearch.SearchAsync(context.UserQuery);

        // ADIM 2: GPT'ye gönder
        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "Sen uzman bir araştırmacı ajansın..."),
            new(ChatRole.User, $"Konu: {context.UserQuery}\nSonuçlar: {searchResults}")
        };
        
        var response = await _chatClient.GetResponseAsync(chatMessages);
        var report = response.Text;

        // ADIM 3: State'e yaz
        context.SetState("research_report", report);
        context.SetState("search_results", searchResults);

        // ADIM 4: Event döner
        return new AgentEvent
        {
            Author = Name,
            Status = "completed",
            Content = report,
            Actions = new EventActions
            {
                StateUpdates = new()
                {
                    ["research_report"] = report,
                    ["search_results"] = searchResults
                }
            }
        };
    }
}
```

### AnalysisAgent — Analist

```csharp
public class AnalysisAgent : BaseAgent
{
    private readonly IChatClient _chatClient;
    private readonly McpFileSystemTools _fileTools;
    private readonly McpDatabaseTools _dbTools;
    
    public override async Task<AgentEvent> RunAsync(AgentContext context, ...)
    {
        // ADIM 1: State'ten oku (Araştırmacı'nın çıktıları)
        var report = context.GetState<string>("research_report");
        var rawData = context.GetState<string>("search_results");

        // ADIM 2: GPT ile analiz et
        var response = await _chatClient.GetResponseAsync(
            new List<ChatMessage>
            {
                new(ChatRole.System, "Sen analiz ajanısın..."),
                new(ChatRole.User, $"Rapor: {report}\nVeri: {rawData}")
            });
        var analysis = response.Text;

        // ADIM 3: MCP araçlarıyla kaydet
        await _fileTools.SaveResearchToFileAsync(fileName, analysis);
        await _dbTools.SaveResearchAsync(query, rawData, analysis, "web");

        // ADIM 4: Escalate ile pipeline'ı tamamla
        return new AgentEvent
        {
            Author = Name,
            Status = "completed",
            Content = analysis,
            Actions = new EventActions
            {
                StateUpdates = new() { ["analysis_result"] = analysis },
                Escalate = true  // Pipeline tamam!
            }
        };
    }
}
```

### ADK + MCP + GPT Entegrasyonu

Her LlmAgent üç teknolojinin kesişiminde çalışır:

```
┌──────────────────────────────────┐
│         LlmAgent                 │
│                                  │
│  ┌─── ADK ──┐                   │
│  │ BaseAgent │                   │
│  │ RunAsync  │                   │
│  │ State I/O │                   │
│  └───────────┘                   │
│                                  │
│  ┌─── GPT ──────────────────┐   │
│  │ IChatClient               │   │
│  │ System Prompt + User Msg  │   │
│  │ → Metin üretimi           │   │
│  └───────────────────────────┘   │
│                                  │
│  ┌─── MCP Araçlar ──────────┐   │
│  │ web_search (giriş)       │   │
│  │ save_file  (çıkış)       │   │
│  │ save_db    (çıkış)       │   │
│  └───────────────────────────┘   │
│                                  │
└──────────────────────────────────┘
```

---

## 10. Orkestrasyon Desenleri

### Desen 1: Sıralı Pipeline (Bu Projede)

```csharp
var pipeline = new SequentialAgent { Name = "Research" };
pipeline.AddSubAgent(researcherAgent);
pipeline.AddSubAgent(analysisAgent);
await pipeline.RunAsync(context);
```

**Kullanım:** Bir ajanın çıktısı bir sonrakinin girdisi olduğunda.

### Desen 2: Paralel Toplama

```csharp
var gather = new ParallelAgent { Name = "Gather" };
gather.AddSubAgent(webSearchAgent);
gather.AddSubAgent(newsSearchAgent);
gather.AddSubAgent(academicSearchAgent);
await gather.RunAsync(context);
// Tüm sonuçlar state'te
```

**Kullanım:** Birden fazla bağımsız kaynaktan aynı anda veri toplarken.

### Desen 3: Fan-Out / Fan-In

```csharp
var root = new SequentialAgent { Name = "FanOutFanIn" };

// Fan-Out: Paralel veri toplama
var gather = new ParallelAgent { Name = "Gather" };
gather.AddSubAgent(webAgent);
gather.AddSubAgent(newsAgent);

// Fan-In: Toplanan verileri analiz et
root.AddSubAgent(gather);        // Paralel toplama
root.AddSubAgent(analysisAgent); // Sıralı analiz

await root.RunAsync(context);
```

```
                ┌── WebAgent ──┐
Sequential ──► Parallel ──────► AnalysisAgent
                └── NewsAgent ─┘
```

### Desen 4: Koşullu Yönlendirme (Transfer)

```csharp
public class RouterAgent : BaseAgent
{
    public override async Task<AgentEvent> RunAsync(AgentContext context, ...)
    {
        var query = context.UserQuery;
        
        // LLM'e sor: Bu sorgu hangi ajana gitmeli?
        var response = await _chatClient.GetResponseAsync(...);
        
        return new AgentEvent
        {
            Author = Name,
            Status = "completed",
            Actions = new EventActions
            {
                TransferToAgent = response.Text switch
                {
                    var t when t.Contains("araştırma") => "ResearcherAgent",
                    var t when t.Contains("analiz") => "AnalysisAgent",
                    _ => "DefaultAgent"
                }
            }
        };
    }
}
```

### Desen 5: Döngü (Loop)

```csharp
public class LoopAgent : BaseAgent
{
    public int MaxIterations { get; set; } = 5;
    
    public override async Task<AgentEvent> RunAsync(AgentContext context, ...)
    {
        for (int i = 0; i < MaxIterations; i++)
        {
            foreach (var sub in SubAgents)
            {
                var event = await sub.RunAsync(context);
                
                // Çıkış koşulu
                if (event.Actions?.Escalate == true)
                    return event;
            }
        }
        
        return new AgentEvent { Author = Name, Status = "completed" };
    }
}

// Kullanım: 
// "Araştırma yeterli kaliteye ulaşana kadar tekrarla"
var loop = new LoopAgent { MaxIterations = 3 };
loop.AddSubAgent(researcherAgent);
loop.AddSubAgent(qualityCheckerAgent);  // Kalite kontrolü yapar, yeterliyse Escalate=true
```

---

## 11. ADK vs Diğer Çatılar

| Özellik | ADK (.NET uyarlama) | LangChain | Semantic Kernel | AutoGen |
|---------|---------------------|-----------|-----------------|---------|
| **Dil** | C# (özel impl.) | Python/JS | C# / Python | Python |
| **Ajan modeli** | BaseAgent hiyerarşisi | Chain/Agent | Planner + Plugin | Multi-agent chat |
| **Pipeline** | SequentialAgent | Chain | Plan execution | ChatAutoGen |
| **Paralel** | ParallelAgent | - | - | GroupChat |
| **State** | AgentContext.State | Memory | KernelArguments | ConversableAgent |
| **A2A desteği** | Evet (entegre) | Hayır | Hayır | Hayır |
| **MCP desteği** | Evet (entegre) | Kısıtlı | Hayır | Hayır |

ADK'nın benzersiz avantajları:
- **A2A ile doğal entegrasyon** — ajanlar arası iletişim protokol düzeyinde
- **MCP ile doğal entegrasyon** — araçlar standart olarak tanımlanır
- **Basit hiyerarşi** — anlaşılması ve genişletilmesi kolay
- **State-based iletişim** — ajanlar arası veri paylaşımı sezgisel

---

## 12. Kaynaklar

- [Google ADK Python](https://google.github.io/adk-docs/)
- [Google ADK GitHub](https://github.com/google/adk-python)
- [Agent Design Patterns](https://www.anthropic.com/engineering/building-effective-agents)
- [Multi-Agent Systems — Overview](https://arxiv.org/abs/2402.01680)

---

*Bu rehber, Agent Ecosystem projesinin ADK öğretici dokümantasyonudur.*  
*Ana makale: [01-makale-agent-ekosistemi.md](01-makale-agent-ekosistemi.md)*
