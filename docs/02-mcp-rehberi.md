# MCP (Model Context Protocol) — Derinlemesine Rehber

> Bu rehber, MCP'nin ne olduğunu, nasıl çalıştığını ve projemizde nasıl kullanıldığını detaylı şekilde anlatır.

---

## İçindekiler

1. [MCP Nedir?](#1-mcp-nedir)
2. [MCP Mimarisi](#2-mcp-mimarisi)
3. [MCP Temel Kavramları](#3-mcp-temel-kavramları)
4. [.NET'te MCP — NuGet SDK](#4-nette-mcp--nuget-sdk)
5. [Projemizdeki MCP Araçları](#5-projemizdeki-mcp-araçları)
6. [MCP Tool Tanımlama — Adım Adım](#6-mcp-tool-tanımlama--adım-adım)
7. [MCP Server vs MCP Client](#7-mcp-server-vs-mcp-client)
8. [Gerçek Dünya Kullanım Senaryoları](#8-gerçek-dünya-kullanım-senaryoları)
9. [En İyi Pratikler](#9-en-iyi-pratikler)
10. [Kaynaklar](#10-kaynaklar)

---

## 1. MCP Nedir?

**Model Context Protocol (MCP)**, Anthropic tarafından geliştirilen ve 2024'te açık kaynak olarak duyurulan bir protokoldür. Resmi web sitesi: [modelcontextprotocol.io](https://modelcontextprotocol.io)

### Problemi Anlayalım

LLM'ler (GPT, Claude, Gemini vb.) son derece güçlü metin üretim motorlarıdır. Ancak doğrudan şunları **yapamazlar**:

- ❌ Veritabanına kayıt ekleyemezler
- ❌ Web'de arama yapmazlar
- ❌ Dosya oluşturup kaydedemezler
- ❌ API çağrısı yapamazlar
- ❌ Gerçek zamanlı veri alamazlar

**MCP bu boşluğu doldurur.** LLM'lere "el" verir — dış dünyayla etkileşim kurmalarını sağlar.

### Analoji: USB-C for AI

MCP'yi bir USB-C portu gibi düşünün:
- USB-C öncesi: Her cihazın kendi özel kablosu vardı
- USB-C sonrası: Tek standart, her cihazla çalışır

MCP öncesi: Her LLM entegrasyonu için özel kod yazılıyordu (function calling, tool use)  
MCP sonrası: Tek standart protokol, **her LLM ve her araç** birbiriyle çalışır

```
USB-C Dünyası:                    MCP Dünyası:
┌──────┐      ┌──────┐           ┌──────┐      ┌──────────┐
│Laptop│─USB─C│Ekran │           │ LLM  │─MCP──│WebSearch │
│      │      │      │           │      │      │          │
│      │─USB─C│Disk  │           │      │─MCP──│Database  │
│      │      │      │           │      │      │          │
│      │─USB─C│Şarj  │           │      │─MCP──│FileSystem│
└──────┘      └──────┘           └──────┘      └──────────┘
```

---

## 2. MCP Mimarisi

MCP, **istemci-sunucu** mimarisini kullanır:

```
┌──────────────────────┐         ┌──────────────────────────┐
│     MCP Host         │         │      MCP Server          │
│  (LLM uygulaması)    │   JSON  │  (Araç sağlayıcısı)     │
│                      │   RPC   │                          │
│  ┌────────────────┐  │ ◄─────► │  ┌──────────────────┐   │
│  │  MCP Client    │──┼─────────┼──│  Tool Handler     │   │
│  │                │  │         │  │  ┌──────────┐     │   │
│  │ tools/list     │──┼─────────┼──│  │web_search│     │   │
│  │ tools/call     │──┼─────────┼──│  │save_file │     │   │
│  │                │  │         │  │  │query_db  │     │   │
│  └────────────────┘  │         │  │  └──────────┘     │   │
│                      │         │  └──────────────────┘   │
└──────────────────────┘         └──────────────────────────┘
```

### İletişim Akışı

```
1. KEŞIF (Discovery)
   Client → Server: tools/list
   Client ← Server: [{ name: "web_search", description: "...", params: {...} }, ...]
   
2. ÇAĞIRMA (Invocation)
   Client → Server: tools/call { name: "web_search", arguments: { query: "python" } }
   Client ← Server: { content: [{ type: "text", text: "Sonuçlar..." }] }
   
3. KAYNAK OKUMA (Resource)
   Client → Server: resources/read { uri: "file://research/report.md" }
   Client ← Server: { content: "Rapor içeriği..." }
```

### Transport Seçenekleri

| Transport | Kullanım | Açıklama |
|-----------|----------|----------|
| **stdio** | Yerel araçlar | Aynı makinede, stdin/stdout üzerinden |
| **SSE** | Web tabanlı | HTTP Server-Sent Events ile |
| **Streamable HTTP** | Modern web | HTTP streaming ile (yeni) |

---

## 3. MCP Temel Kavramları

### 3.1 Tool (Araç)

Tool, LLM'in çağırabileceği bir fonksiyondur. Her aracın:
- **Adı** (benzersiz tanımlayıcı)
- **Açıklaması** (LLM'in anlayacağı doğal dil)
- **Parametreleri** (JSON Schema ile tanımlı)
- **Dönüş değeri** (metin veya yapılandırılmış veri)

```
Tool: web_search
├── Açıklama: "Web'de arama yapar"
├── Parametreler:
│   └── query (string, required): "Arama sorgusu"
└── Dönüş: string (arama sonuçları)
```

### 3.2 Resource (Kaynak)

Resource, salt okunur veri kaynağıdır. Dosyalar, veritabanı kayıtları, API yanıtları:

```
Resource: file://research/python-report.md
├── URI: file://research/python-report.md
├── Açıklama: "Python araştırma raporu"
└── İçerik: Markdown metin
```

### 3.3 Prompt (Şablon)

Prompt, hazır metin şablonudur. LLM'e rehberlik eder:

```
Prompt: research-analysis
├── Açıklama: "Araştırma analiz şablonu"
├── Parametreler: topic, depth
└── Şablon: "{{topic}} konusunda {{depth}} düzeyinde analiz yap..."
```

### 3.4 Kavramlar Arası İlişki

```
MCP Server
├── Tools → LLM'in çağırabileceği fonksiyonlar (WRITE)
├── Resources → LLM'in okuyabileceği veriler (READ)
└── Prompts → LLM'e rehberlik eden şablonlar (GUIDE)
```

---

## 4. .NET'te MCP — NuGet SDK

### 4.1 Paketler

```xml
<!-- MCP temel SDK -->
<PackageReference Include="ModelContextProtocol" Version="0.8.0-preview.1" />

<!-- ASP.NET Core entegrasyonu (web uygulamaları için) -->
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.8.0-preview.1" />
```

Bu, Anthropic'in **resmi** C# SDK'dır. GitHub: [modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)

### 4.2 Temel Attribute'lar

SDK iki temel attribute sağlar:

#### `[McpServerToolType]` — Sınıf Seviyesi

Sınıfı MCP araç sınıfı olarak işaretler. MCP Server bu sınıfı otomatik keşfeder.

```csharp
[McpServerToolType]
public class MyTools
{
    // Bu sınıftaki tüm [McpServerTool] metodları MCP aracı olur
}
```

#### `[McpServerTool]` — Metod Seviyesi

Metodun MCP aracı olduğunu bildirir. `Name` parametresi araç adını belirler.

```csharp
[McpServerTool(Name = "my_tool")]
[Description("Bu aracın açıklaması — LLM bunu okur")]
public async Task<string> MyToolAsync(
    [Description("Parametre açıklaması")] string param1)
{
    return "Sonuç";
}
```

#### `[Description]` — Açıklama

`System.ComponentModel.Description` attribute'u, LLM'in aracı ve parametreleri anlamasını sağlar. **Bu çok önemlidir** — LLM, aracı çağırıp çağırmamaya bu açıklamaya bakarak karar verir.

```csharp
// İyi açıklama:
[Description("Veritabanında geçmiş araştırmaları anahtar kelimeye göre arar. " +
             "Sorgu veya sonuç içinde geçen kayıtları döner.")]

// Kötü açıklama:
[Description("Arama yapar")]  // LLM ne aradığını anlamaz
```

### 4.3 MCP Server Kurulumu (ASP.NET Core)

```csharp
// Program.cs veya Module ConfigureServices
builder.Services.AddMcpServer()
    .WithStdioServerTransport()       // stdio transport
    .WithToolsFromAssembly();         // [McpServerToolType] sınıflarını tara

// veya ASP.NET Core HTTP transport
app.MapMcpSse();                     // SSE endpoint'i
```

### 4.4 MCP Client Kullanımı

```csharp
using ModelContextProtocol.Client;

// MCP sunucusuna bağlan
var client = await McpClientFactory.CreateAsync(
    new McpServerConfig { ... });

// Araçları listele
var tools = await client.ListToolsAsync();

// Araç çağır
var result = await client.CallToolAsync(
    "web_search", 
    new { query = "Python 3.13" });
```

---

## 5. Projemizdeki MCP Araçları

Projemizde 3 MCP araç sınıfı tanımladık:

### Araç Kataloğu

| Sınıf | Araçlar | Kullanan Ajan |
|-------|---------|---------------|
| `McpWebSearchTools` | `web_search`, `fetch_url_content` | ResearcherAgent |
| `McpDatabaseTools` | `save_research_to_database`, `search_past_research`, `get_recent_research` | AnalysisAgent |
| `McpFileSystemTools` | `save_research_to_file`, `read_research_file`, `list_research_files` | AnalysisAgent |

### Veri Akışında MCP'nin Yeri

```
ResearcherAgent                        AnalysisAgent
┌────────────────────┐                ┌────────────────────┐
│                    │                │                    │
│  1. GPT sorgusu    │                │  1. State'ten oku  │
│     ↓              │                │     ↓              │
│  2. MCP:web_search │ ←── MCP ───── │  2. GPT analizi    │
│     sonuç döner    │                │     ↓              │
│     ↓              │    State       │  3. MCP:save_file  │
│  3. GPT analizi    │ ──────────────►│     MCP:save_db    │
│     ↓              │                │     sonuç kaydet   │
│  4. State'e yaz    │                │                    │
└────────────────────┘                └────────────────────┘
```

### Araç Detayları

#### web_search

```
Adı:        web_search
Açıklama:   Web'de arama yapar. Araştırma konusuyla ilgili güncel bilgileri toplar.
Parametreler:
  - query (string): Arama sorgusu (örn: 'Python 3.13 yenilikleri')
Dönüş:      Formatlanmış arama sonuçları (başlık, URL, snippet, kaynak)
Durumu:     Demo modda simüle sonuçlar, üretimde Bing/Google API
```

#### save_research_to_database

```
Adı:        save_research_to_database
Açıklama:   Bir araştırma sonucunu veritabanına kaydeder.
Parametreler:
  - query (string): Araştırma sorgusu
  - rawData (string): Ham araştırma verileri
  - analyzedResult (string): Analiz edilmiş sonuç
  - sources (string): Kaynaklar
Dönüş:      "Kaydedildi. ID: {guid}"
Backend:    ABP IRepository<ResearchRecord, Guid>
```

#### save_research_to_file

```
Adı:        save_research_to_file
Açıklama:   Araştırma sonucunu belirtilen dosya adıyla kaydeder.
Parametreler:
  - fileName (string): Dosya adı (örn: python-yenilikleri.md)
  - content (string): Dosya içeriği
Dönüş:      "Dosya başarıyla kaydedildi: {fileName}"
Konum:      ./ResearchResults/ dizini
```

---

## 6. MCP Tool Tanımlama — Adım Adım

Yeni bir MCP aracı eklemek istiyorsanız:

### Adım 1: Sınıfı Oluştur

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace AgentEcosystem.McpTools;

[McpServerToolType]  // ← Bu sınıf MCP araç sınıfıdır
public class McpEmailTools
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<McpEmailTools> _logger;

    public McpEmailTools(IEmailSender emailSender, ILogger<McpEmailTools> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }
}
```

### Adım 2: Araç Metotlarını Ekle

```csharp
[McpServerTool(Name = "send_research_email")]
[Description("Araştırma sonucunu e-posta olarak gönderir. " +
             "Alıcı e-posta adresi ve rapor içeriği gereklidir.")]
public async Task<string> SendResearchEmailAsync(
    [Description("Alıcı e-posta adresi")] string to,
    [Description("E-posta konusu")] string subject,
    [Description("Rapor içeriği (Markdown)")] string body)
{
    try
    {
        await _emailSender.SendAsync(to, subject, body);
        _logger.LogInformation("[MCP:Email] E-posta gönderildi: {To}", to);
        return $"E-posta başarıyla gönderildi: {to}";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[MCP:Email] Gönderim hatası");
        return $"E-posta gönderilemedi: {ex.Message}";
    }
}
```

### Adım 3: DI'a Kaydet

```csharp
// AgentEcosystemModule.cs
services.AddTransient<McpEmailTools>();
```

### Adım 4: Ajandan Kullan

```csharp
public class NotificationAgent : BaseAgent
{
    private readonly McpEmailTools _emailTools;

    public override async Task<AgentEvent> RunAsync(AgentContext context, ...)
    {
        var report = context.GetState<string>("analysis_result");
        var result = await _emailTools.SendResearchEmailAsync(
            "user@example.com", "Araştırma Sonucu", report);
        // ...
    }
}
```

---

## 7. MCP Server vs MCP Client

### Bizim Projede Hangi Rolle Kullanıyoruz?

Bu projede MCP'yi **sunucu tarafında** kullanıyoruz:
- Araçlarımızı `[McpServerToolType]` ile tanımlıyoruz
- Ajanlarımız bu araçları **doğrudan method çağrısıyla** kullanıyor
- Henüz harici bir MCP istemcisine hizmet vermiyoruz

### MCP Server Olarak Çalışmak (Gelişmiş)

Eğer araçlarınızı dış dünyaya sunmak isterseniz:

```csharp
// Program.cs
app.MapMcpSse("/mcp");  // SSE endpoint'i aç

// Artık herhangi bir MCP istemcisi bağlanabilir:
// - Claude Desktop
// - Cursor IDE
// - Visual Studio Code (Copilot)
// - Başka MCP uyumlu araçlar
```

### MCP Client Olarak Çalışmak

Başka bir MCP sunucusunun araçlarını kullanmak için:

```csharp
var client = await McpClientFactory.CreateAsync(
    new McpServerConfig
    {
        Id = "weather-server",
        Name = "Weather MCP Server",
        TransportType = TransportTypes.Sse,
        Location = "http://weather-server:3000/mcp"
    });

var tools = await client.ListToolsAsync();
var weather = await client.CallToolAsync(
    "get_weather", new { city = "Istanbul" });
```

---

## 8. Gerçek Dünya Kullanım Senaryoları

### Senaryo 1: Veritabanı Asistanı

```
MCP Server: database-tools
├── query_table: SQL sorgusu çalıştır
├── describe_schema: Tablo yapısını göster
├── insert_record: Kayıt ekle
└── generate_report: Rapor oluştur
```

### Senaryo 2: DevOps Asistanı

```
MCP Server: devops-tools
├── deploy_service: Servisi deploy et
├── check_health: Sağlık kontrolü yap
├── read_logs: Log dosyalarını oku
└── scale_service: Servisi ölçeklendir
```

### Senaryo 3: Doküman Yönetimi

```
MCP Server: document-tools
├── create_document: Doküman oluştur
├── search_documents: Doküman ara
├── convert_format: Format dönüştür (PDF→MD)
└── summarize_document: Dokümanı özetle
```

---

## 9. En İyi Pratikler

### Araç Tasarımı

1. **Tek sorumluluk**: Her araç tek bir iş yapsın
2. **Açık isimler**: `web_search` ✅, `do_stuff` ❌
3. **Detaylı açıklamalar**: LLM açıklamayı okuyarak karar verir
4. **Parametre doğrulama**: Her parametreyi kontrol edin
5. **Hata yönetimi**: Exception fırlatmak yerine hata mesajı döndürün

### Güvenlik

1. **Yetkilendirme**: Araçlara erişimi ABP permission sistemiyle kontrol edin
2. **Giriş temizleme**: SQL injection, path traversal'a karşı koruyun
3. **Rate limiting**: Araç çağrı sayısını sınırlayın
4. **Audit logging**: Hangi araç, kim tarafından, ne zaman çağrıldı kaydedin

### Performans

1. **Async**: Tüm I/O işlemleri async olmalı
2. **Timeout**: Web isteklerinde timeout kullanın
3. **Cache**: Sık erişilen verileri cache'leyin
4. **Lazy loading**: Kaynakları gerektiğinde yükleyin

---

## 10. Kaynaklar

- [MCP Resmi Dokümantasyon](https://modelcontextprotocol.io/)
- [MCP C# SDK — GitHub](https://github.com/modelcontextprotocol/csharp-sdk)
- [MCP Spesifikasyonu](https://spec.modelcontextprotocol.io/)
- [MCP Mimari Genel Bakış](https://modelcontextprotocol.io/docs/concepts/architecture)
- [NuGet: ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol)

---

*Bu rehber, Agent Ecosystem projesinin MCP öğretici dokümantasyonudur.*  
*Ana makale: [01-makale-agent-ekosistemi.md](01-makale-agent-ekosistemi.md)*
