# .NET ile A2A, MCP ve ADK Kullanarak Çoklu Ajan AI Sistemi Geliştirmek

> Google'ın A2A & ADK protokolleri ile Anthropic'in MCP protokolünü bir araya getirerek .NET 10 üzerinde çalışan, üretime hazır bir Çoklu Ajan Araştırma Asistanı nasıl geliştirdik?

---

## Giriş

AI dünyası hızla evriliyor. Artık tek bir LLM çağrısının ötesine geçtik ve **çoklu ajan sistemleri** çağına giriyoruz — karmaşık problemleri çözmek için uzman AI ajanlarının bir takım gibi birlikte çalıştığı sistemler.

Ancak ortada kritik sorular var: **Ajanlar birbirleriyle nasıl konuşacak? Onlara araçları nasıl vereceğiz? Hepsini nasıl yöneteceğiz?**

Bu soruları yanıtlamak için üç açık protokol ortaya çıktı:

- **MCP (Model Context Protocol)** — Anthropic tarafından geliştirildi — "AI için USB-C"
- **A2A (Agent-to-Agent Protocol)** — Google tarafından geliştirildi — "Ajanlar arası telefon hattı"
- **ADK (Agent Development Kit)** — Google tarafından geliştirildi — "Ajan takımının organizasyon şeması"

Bu makalede her bir protokolü açıklayacak, birbirlerini nasıl tamamladıklarını gösterecek ve gerçek projemiz olan **.NET ile geliştirilmiş Çoklu Ajan Araştırma Asistanı**'nı anlatacağız.

---

## Problem: Neden Tek Ajan Yetmiyor?

AI'ya şunu sorduğunuzu düşünün: *"En son AI ajan framework'lerini araştır ve kapsamlı bir analiz raporu hazırla."*

Tek bir LLM çağrısı şunları yapardı:
- ❌ Arama sonuçlarını uydurur (gerçekte web'de arama yapamaz)
- ❌ Yüzeysel bir analiz üretir (yapılandırılmış bir araştırma hattı yok)
- ❌ Adımlar arasında bağlamı kaybeder (durum yönetimi yok)
- ❌ Sonuçları hiçbir yere kaydedemez (araç erişimi yok)

Gerçekte ihtiyacınız olan şey bir **uzman ekibi**:

1. Web'de arama yapıp ham veri toplayan bir **Araştırmacı**
2. Bu veriyi yapılandırılmış bir rapora dönüştüren bir **Analist**
3. Ajanların gerçek dünyayla etkileşim kurmasını sağlayan **Araçlar** (web, veritabanı, dosya sistemi)
4. Her şeyi koordine eden bir **Orkestratör**

Tam olarak bunu inşa ettik.

<!-- 
📸 GÖRSEL 2: Tek Ajan vs Çoklu Ajan Karşılaştırması
Sol taraf: Tek Ajan (her şeyi tek başına yapmaya çalışan, bunalmış bir robot)
Sağ taraf: Çoklu Ajan Takımı (Araştırmacı + Analist + Araçlar birlikte çalışıyor)
Pipeline'da bağlantı okları gösterilmeli
Stil: Temiz infografik, iki panelli karşılaştırma
Boyut: 1200x500px
-->

---

## Protokol #1: MCP — Ajanlara Süper Güç Vermek

### MCP Nedir?

Anthropic tarafından oluşturulan **MCP (Model Context Protocol)**, AI modellerini harici araçlara ve veri kaynaklarına bağlamanın standart bir yoludur. Bunu **AI için USB-C** olarak düşünün — her şeyle çalışan evrensel tek bir bağlantı.

MCP öncesi, LLM'inizin web araması yapmasını, veritabanı sorgulamasını ve dosya kaydetmesini istiyorsanız, her yetenek için özel entegrasyon kodu yazmanız gerekiyordu. MCP ile araçları bir kez tanımlarsınız ve MCP uyumlu herhangi bir ajan onları kullanabilir.

<!-- 
📸 GÖRSEL 3: USB-C Analojisi ile MCP
Sol panel: "MCP Öncesi" — Birbirine dolanmış farklı kablolar/konektörler
  (Web arama için özel API, veritabanı için özel API, dosyalar için özel API...)
Sağ panel: "MCP ile" — Her şeyi bağlayan tek temiz bir USB-C kablosu
  (MCP standardı LLM'i Web Arama, Veritabanı, Dosya Sistemine bağlıyor)
Stil: Basit, temiz analoji illüstrasyonu
Boyut: 1200x400px
-->

### MCP Nasıl Çalışır?

MCP basit bir **İstemci-Sunucu mimarisi** izler:

```
┌─────────────────┐         ┌─────────────────┐
│   MCP İstemci   │         │   MCP Sunucu    │
│   (Ajan)        │ ◄─────► │   (Araçlar)     │
│                 │         │                 │
│  "Hangi araçlar │ ──GET── │  "Yapabilecek-  │
│   var?"         │         │   lerim bunlar" │
│                 │         │                 │
│  "X'i ara"      │ ─CALL─► │  *web'de arar*  │
│                 │ ◄─────  │  "İşte          │
│                 │         │   sonuçlar"     │
└─────────────────┘         └─────────────────┘
```

Akış oldukça basit:

1. **Keşif**: Ajan "Hangi araçlarınız var?" diye sorar (`tools/list`)
2. **Çağırma**: Ajan belirli bir aracı çağırır (`tools/call`)
3. **Sonuç**: Araç veriyi ajana geri döner

### Projemizdeki MCP

Üç MCP araç sunucusu inşa ettik:

| MCP Aracı | Amaç | Kullanan |
|-----------|------|----------|
| `web_search` | Tavily API ile web araması yapar | Araştırmacı Ajan |
| `fetch_url_content` | Bir URL'nin içeriğini çeker | Araştırmacı Ajan |
| `save_research_to_file` | Raporları dosya sistemine kaydeder | Analiz Ajanı |
| `save_research_to_database` | Sonuçları SQL Server'a kaydeder | Analiz Ajanı |
| `search_past_research` | Geçmiş araştırmaları sorgular | Analiz Ajanı |

MCP'nin güzelliği şu: ajanların bu araçların dahili olarak *nasıl* çalıştığını bilmesine gerek yoktur. Sadece bir açıklama görürler ve adıyla çağırırlar. Gerisini araç halleder.

<!-- 
📸 GÖRSEL 4: Projemizdeki MCP Araç Mimarisi
Merkezde "MCP Sunucu" kutusu, dışarı doğru üç araç grubu yayılıyor:
  - Web Arama Araçları (küre ikonu): web_search, fetch_url_content
  - Dosya Sistemi Araçları (klasör ikonu): save_research_to_file, read_research_file, list_research_files
  - Veritabanı Araçları (veritabanı ikonu): save_research_to_database, search_past_research, get_recent_research
Her araç grubu veri kaynağına bağlanıyor (İnternet, Dosya Sistemi, SQL Server)
Solda iki ajan MCP Sunucusuna bağlanıyor
Stil: İkonlu mimari diyagram, temiz çizgiler
Boyut: 1200x600px
-->

---

## Protokol #2: A2A — Ajanları Birbirleriyle Konuşturmak

### A2A Nedir?

Başlangıçta Google tarafından önerilen ve şimdi Linux Foundation bünyesinde geliştirilen **A2A (Agent-to-Agent)**, AI ajanlarının **birbirlerini keşfetmesini ve görev alışverişi yapmasını** sağlayan bir protokoldür. MCP ajanlara araç vermekle ilgiliyse, A2A ajanlara iletişim yeteneği vermekle ilgilidir.

Şöyle düşünün:
- **MCP** = "Bu ajan ne *yapabilir*?" (yetenekler)
- **A2A** = "Ajanlar nasıl *konuşur*?" (iletişim)

### Agent Card: Ajanınızın Kartviziti

Her A2A uyumlu ajan bir **Agent Card** yayınlar — kim olduğunu ve ne yapabildiğini anlatan bir JSON dokümanı. AI ajanları için bir kartvizit gibidir:

```json
{
  "name": "Researcher Agent",
  "description": "Kapsamlı araştırma verileri toplamak için web'de arama yapar",
  "url": "https://localhost:44331/a2a/researcher",
  "version": "1.0.0",
  "capabilities": {
    "streaming": false,
    "pushNotifications": false
  },
  "skills": [
    {
      "id": "web-research",
      "name": "Web Research",
      "description": "Verilen konuda web'de arama yaparak ham veri toplar",
      "tags": ["research", "web-search", "data-collection"]
    }
  ]
}
```

Diğer ajanlar bu kartı `/.well-known/agent.json` adresinde keşfedebilir ve anında şunları öğrenebilir:
- Bu ajanın ne yaptığını
- Nerede ulaşılabileceğini
- Hangi becerilere sahip olduğunu

<!-- 
📸 GÖRSEL 5: A2A Ajan Keşif Akışı
Yatay akış olarak üç adım:
  Adım 1: "Keşif" — Ajan A, Ajan B'nin /.well-known/agent.json adresine GET isteği gönderir
  Adım 2: "Görev Gönderme" — Ajan A, POST /tasks/send ile görev gönderir
  Adım 3: "Sonuç" — Ajan B tamamlanmış görevi artifact'larla birlikte döner
İkonlar: büyüteç (keşif), zarf (görev), onay işareti (sonuç)
Adım 1'de Agent Card JSON önizlemesi
Stil: Numaralı dairelerle adım adım akış diyagramı
Boyut: 1200x400px
-->

### A2A Görev Alışverişi Nasıl Çalışır?

Bir ajan başka bir ajanı keşfettikten sonra görev gönderebilir:

```
Orkestratör                          Araştırmacı Ajan
     │                                      │
     │  1. GET /.well-known/agent.json      │
     │ ────────────────────────────────────► │
     │  ◄── Agent Card (beceriler, URL)     │
     │                                      │
     │  2. POST /tasks/send                 │
     │     { "AI framework'leri araştır" }  │
     │ ────────────────────────────────────► │
     │                                      │ 🔍 Web'de aranıyor...
     │                                      │ 📝 Veri toplanıyor...
     │                                      │
     │  3. ◄── { status: "completed",       │
     │           artifacts: [rapor] }        │
     │                                      │
```

Temel kavramlar:

- **Task (Görev)**: Ajanlar arasında gönderilen iş birimi (talimat içeren bir e-posta gibi)
- **Artifact (Çıktı)**: Ajanın ürettiği sonuç (yanıttaki ek dosya gibi)
- **Task State (Görev Durumu)**: `Submitted → Working → Completed/Failed`

### Projemizdeki A2A

Sistemimiz ajanlar arası iletişim için A2A kullanır:

- **Orkestratör**, Agent Card'ları üzerinden her iki ajanı keşfeder
- Bir araştırma görevi **Araştırmacı Ajan**'a gönderir
- Araştırmacı'nın çıktısı (artifact'lar) **Analiz Ajanı**'na girdi olur
- Analiz Ajanı nihai yapılandırılmış raporu üretir

<!-- 
📸 GÖRSEL 6: Projemizdeki A2A İletişimi
Üç aktörle sıra diyagramı:
  - Orkestratör (ortada, daha büyük)
  - Araştırmacı Ajan (solda)
  - Analiz Ajanı (sağda)
Akış:
  1. Orkestratör → Araştırmacı: "Bu konuyu araştır"
  2. Araştırmacı → Orkestratör: Ham araştırma verisini döner
  3. Orkestratör → Analist: "Bu veriyi analiz et" (2. adımdaki araştırmayı iletir)
  4. Analist → Orkestratör: Yapılandırılmış analiz raporunu döner
Her adımda görev durumları gösterimi (Working → Completed)
Stil: UML sıra diyagramı, renkli aktörler
Boyut: 1000x600px
-->

---

## Protokol #3: ADK — Ajan Takımınızı Organize Etmek

### ADK Nedir?

Google tarafından oluşturulan **ADK (Agent Development Kit)**, **birden fazla ajanı organize etme ve yönetme** kalıpları sağlar. "Verimli çalışan bir ajan takımını nasıl kurarsınız?" sorusuna yanıt verir.

ADK size şunları sunar:
- **BaseAgent**: Her ajanın miras aldığı temel sınıf
- **SequentialAgent**: Ajanları sırayla çalıştırır (pipeline)
- **ParallelAgent**: Ajanları eşzamanlı çalıştırır
- **AgentContext**: Pipeline boyunca akan paylaşımlı durum
- **AgentEvent**: Kontrol akışı sinyalleri (yükselt, transfer et, durum güncelle)

> **Not**: ADK'nın resmi SDK'sı yalnızca Python'dadır. Biz temel kalıpları projemiz için .NET'e taşıdık.

### Pipeline Kalıbı

ADK'nın en güçlü kalıbı **Sıralı Pipeline**'dır. Bunu bir fabrikadaki montaj hattı gibi düşünün:

```
┌──────────┐   Durum    ┌──────────┐   Durum    ┌──────────┐
│          │   akar     │          │   akar     │          │
│ Ajan A   │ ────────►  │ Ajan B   │ ────────►  │ Ajan C   │
│          │            │          │            │          │
│ Çıktı    │            │ A'nın    │            │ B'nin    │
│ üretir   │            │ verisini │            │ verisini │
│          │            │ tüketir  │            │ tüketir  │
│          │            │ Çıktı    │            │ Son      │
│          │            │ üretir   │            │ çıktıyı  │
│          │            │          │            │ üretir   │
└──────────┘            └──────────┘            └──────────┘
```

Her ajan:
1. Paylaşımlı **AgentContext**'i alır (önceki ajanlardan gelen durumla birlikte)
2. İşini yapar
3. Durumu günceller
4. Bir sonraki ajana iletir

<!-- 
📸 GÖRSEL 7: ADK Sıralı Pipeline — Fabrika Montaj Hattı Analojisi
Üst: Gerçek dünya analojisi — İstasyonlu fabrika montaj hattı
  İstasyon 1: "Hammaddeler" → İstasyon 2: "İşleme" → İstasyon 3: "Kalite Kontrol" → Son Ürün
Alt: Ajan pipeline'ı eşleşmesi
  Araştırmacı Ajan → Analiz Ajanı → (Çıktı: Yapılandırılmış Rapor)
AgentContext'i ajanlar arasında durum taşıyan konveyör bant olarak göster
Durum her adımda büyür: {sorgu} → {sorgu, araştırma_verisi} → {sorgu, araştırma_verisi, analiz_raporu}
Stil: Montaj hattı metaforlu infografik, açık eşleşme
Boyut: 1200x500px
-->

### AgentContext: Paylaşımlı Bellek

`AgentContext` tüm ajanların okuyabildiği ve yazabildiği paylaşımlı bir beyaz tahta gibidir:

```
AgentContext
┌────────────────────────────────────────────┐
│  UserQuery: "AI ajan framework'leri 2026"  │
│                                            │
│  State:                                    │
│  ├─ researcher_result: "Ham veri..."       │  ← Araştırmacı yazdı
│  ├─ researcher_status: "completed"         │  ← Araştırmacı yazdı
│  ├─ analyst_result: "# Analiz..."          │  ← Analist yazdı
│  └─ analyst_status: "completed"            │  ← Analist yazdı
│                                            │
│  Events:                                   │
│  ├─ [14:30:01] Araştırmacı başladı         │
│  ├─ [14:30:05] Web araması tamamlandı      │
│  ├─ [14:30:06] Araştırmacı tamamladı       │
│  ├─ [14:30:06] Analist başladı             │
│  ├─ [14:30:12] Analiz tamamlandı           │
│  └─ [14:30:12] Pipeline bitti              │
└────────────────────────────────────────────┘
```

Bu kalıp, karmaşık ajanlar arası mesajlaşma ihtiyacını ortadan kaldırır — ajanlar basitçe paylaşımlı bir bağlama okur ve yazar.

### ADK Orkestrasyon Kalıpları

ADK birden fazla orkestrasyon kalıbını destekler:

<!-- 
📸 GÖRSEL 8: ADK Orkestrasyon Kalıpları (4 panelli ızgara)
Panel 1: "Sıralı Pipeline" — A → B → C (doğrusal akış)
Panel 2: "Paralel Yürütme" — A, B, C eşzamanlı çalışır, sonuçlar birleştirilir
Panel 3: "Fan-Out / Fan-In" — Bir girdi A, B, C'ye ayrılır sonra geri birleşir
Panel 4: "Koşullu Yönlendirme" — Koşula göre A veya B'ye yönlendiren karar elması
Her panel etiketli oklarla basit, net bir diyagram olmalı
Stil: 2x2 mini diyagram ızgarası, tutarlı renk şeması
Boyut: 1200x800px
-->

| Kalıp | Açıklama | Kullanım Alanı |
|-------|----------|----------------|
| **Sıralı** | A → B → C | Araştırma → Analiz pipeline'ı |
| **Paralel** | A, B, C eşzamanlı | Aynı anda birden fazla arama |
| **Fan-Out/Fan-In** | Böl → İşle → Birleştir | Dağıtık araştırma |
| **Koşullu Yönlendirme** | If/else ajan seçimi | Sorgu tipine göre yönlendirme |

---

## Üç Protokol Birlikte Nasıl Çalışır?

İşte temel içgörü: **MCP, A2A ve ADK rakip değiller — tam bir ajan sisteminin birbirini tamamlayan katmanlarıdır.**

```
┌─────────────────────────────────────────────────────┐
│                AJAN EKOSİSTEMİ                       │
│                                                      │
│   ┌─── ADK Katmanı (Orkestrasyon) ─────────────┐    │
│   │                                             │    │
│   │   SequentialAgent                           │    │
│   │   ┌──────────┐      ┌──────────┐          │    │
│   │   │Araştır-  │ ───► │ Analist  │           │    │
│   │   │macı Ajan │      │ Ajan     │           │    │
│   │   └────┬─────┘      └────┬─────┘          │    │
│   │        │                  │                 │    │
│   └────────┼──────────────────┼─────────────────┘    │
│            │                  │                       │
│   ┌────────┼── A2A Katmanı (İletişim) ─────────┐    │
│   │        │                  │                 │    │
│   │   Agent Card         Agent Card            │    │
│   │   Görev Alışverişi   Görev Alışverişi      │    │
│   │        │                  │                 │    │
│   └────────┼──────────────────┼─────────────────┘    │
│            │                  │                       │
│   ┌────────┼── MCP Katmanı (Araçlar) ──────────┐    │
│   │        │                  │                 │    │
│   │   ┌────▼────┐      ┌─────▼─────┐          │    │
│   │   │Web Arama │     │Dosya Kayıt │          │    │
│   │   │URL Çek   │     │DB Kayıt    │          │    │
│   │   └─────────┘     │DB Sorgu    │          │    │
│   │                    └───────────┘          │    │
│   └─────────────────────────────────────────────┘    │
│                                                      │
└─────────────────────────────────────────────────────┘
```

Her protokol farklı bir kaygıyı ele alır:

| Katman | Protokol | Yanıtladığı Soru |
|--------|----------|------------------|
| **Üst** | ADK | "Ajanlar nasıl organize edilir?" |
| **Orta** | A2A | "Ajanlar nasıl iletişim kurar?" |
| **Alt** | MCP | "Ajanlar hangi araçları kullanabilir?" |

<!-- 
📸 GÖRSEL 9: Üç Katmanlı Protokol Yığını (Ana Mimari Diyagramı)
Görsel olarak cilalı 3 katmanlı yığın diyagramı oluşturun:
  Katman 3 (Üst, Mavi): ADK — Orkestrasyon Katmanı
    İçerik: SequentialAgent, Araştırmacı → Analist pipeline'ını yönetiyor
  Katman 2 (Orta, Yeşil): A2A — İletişim Katmanı
    İçerik: Agent Card'lar, ajanlar arası Görev Alışverişi okları
  Katman 1 (Alt, Mor): MCP — Araç Katmanı
    İçerik: Web Arama, Dosya Sistemi, Veritabanı araçları ikonlarıyla

Yığının dışında: Solda sorgu gönderen Kullanıcı/API
Kullanıcıdan → 3 katmandan geçerek → sonuçla Kullanıcıya ok

Bu makalenin EN ÖNEMLİ görseli. Profesyonel ve net olmalı.
Stil: Modern teknik mimari diyagramı, yuvarlatılmış dikdörtgenler, gradient renkler
Boyut: 1200x700px
-->

---

## Projemiz: Çoklu Ajan Araştırma Asistanı

### Kullanılan Teknolojiler

- **.NET 10.0** — En son çalışma zamanı
- **ABP Framework 10.0.2** — Kurumsal .NET uygulama framework'ü
- **Semantic Kernel 1.70.0** — Microsoft'un AI orkestrasyon SDK'sı
- **Azure OpenAI (GPT)** — LLM altyapısı
- **Tavily Search API** — Gerçek zamanlı web araması
- **SQL Server** — Araştırma kalıcılığı
- **MCP SDK** (`ModelContextProtocol` 0.8.0-preview.1)
- **A2A SDK** (`A2A` 0.3.3-preview)

### Mimari Genel Bakış

Sistemimiz bir kullanıcının araştırma sorgusunu çoklu ajan pipeline'ı üzerinden işler:

<!-- 
📸 GÖRSEL 10: Tam Sistem Mimarisi — Uçtan Uca Akış
Detaylı ama temiz bir mimari diyagram oluşturun:

Sol taraf: Kullanıcı Arayüzü (Dashboard)
  ↓ HTTP POST /api/app/research/execute
  
Orta: .NET Uygulaması (ABP Framework)
  ├── ResearchAppService (API Katmanı)
  │     ↓
  ├── ResearchOrchestrator (ADK SequentialAgent)
  │     ├── Mod 1: ADK Sıralı Pipeline
  │     └── Mod 2: A2A Protokol Tabanlı
  │     ↓
  ├── Araştırmacı Ajan (GPT + MCP Araçları)
  │     ├── web_search (Tavily API) → İnternet
  │     └── fetch_url_content → Web Sayfaları
  │     ↓ (durum aktarımı: researcher_result)
  ├── Analiz Ajanı (GPT + MCP Araçları)
  │     ├── save_research_to_file → Dosya Sistemi
  │     └── save_research_to_database → SQL Server
  │     ↓
  └── Nihai Sonuç (ResearchResultDto)
        ↓
Sağ taraf: Dashboard sonuçları gösterir (Araştırma Raporu + Analiz Raporu)

Stil: Profesyonel mimari diyagramı, soldan sağa veya yukarıdan aşağıya akış
Boyut: 1200x800px
-->

### Nasıl Çalışır (Adım Adım)

**Adım 1: Kullanıcı Sorgu Gönderir**

Kullanıcı dashboard'da bir araştırma konusu girer — örneğin, *"En son AI ajan framework'lerini karşılaştır: LangChain, Semantic Kernel ve AutoGen"* — ve bir çalışma modu seçer (ADK Sıralı veya A2A).

**Adım 2: Orkestratör Devreye Girer**

`ResearchOrchestrator` sorguyu alır ve bir `AgentContext` oluşturur. ADK modunda iki alt ajanla bir `SequentialAgent` kurar. A2A modunda görevleri `A2AServer` üzerinden gönderir.

**Adım 3: Araştırmacı Ajan İşe Koyulur**

Araştırmacı Ajan:
- Bağlamdan sorguyu alır
- GPT kullanarak optimal arama sorguları formüle eder
- `web_search` MCP aracını çağırır (Tavily API ile desteklenir)
- Ham araştırma verilerini toplar ve sentezler
- Sonuçları paylaşımlı `AgentContext`'e kaydeder

**Adım 4: Analiz Ajanı Devralır**

Analiz Ajanı:
- `AgentContext`'ten Araştırmacı'nın ham verisini okur
- GPT kullanarak derin analiz yapar
- Bölümlerle yapılandırılmış bir Markdown raporu oluşturur:
  - Yönetici Özeti
  - Temel Bulgular
  - Detaylı Analiz
  - Karşılaştırmalı Değerlendirme
  - Sonuç ve Öneriler
- Raporu hem dosya sistemine hem veritabanına kaydetmek için MCP araçlarını çağırır

**Adım 5: Sonuçlar Döner**

Orkestratör tüm sonuçları toplar ve REST API üzerinden kullanıcıya döner. Dashboard araştırma raporunu, analiz raporunu, ajan olay zaman çizelgesini ve ham veriyi görüntüler.

<!-- 
📸 GÖRSEL 11: Adım Adım Pipeline Akışı (Görsel Zaman Çizelgesi)
5 adımlı yatay zaman çizelgesi/pipeline oluşturun:

Adım 1: 🔍 "Kullanıcı Sorgusu" 
  → "AI ajan framework'lerini karşılaştır"

Adım 2: 🎯 "Orkestratör"
  → Pipeline oluşturur, mod seçer

Adım 3: 🌐 "Araştırmacı Ajan" 
  → GPT + web_search MCP aracı
  → Çıktı: Ham araştırma verisi (veri kartı olarak)

Adım 4: 📊 "Analiz Ajanı"
  → GPT + dosya_kayıt + db_kayıt MCP araçları
  → Çıktı: Yapılandırılmış rapor (rapor kartı olarak)

Adım 5: ✅ "Sonuç"
  → Dashboard tamamlanmış araştırmayı gösterir

Adımları oklarla bağlayın, adımlar arasında akan durumu gösterin
Stil: Modern süreç akışı, numaralı daireler, zengin ikonlar
Boyut: 1200x400px
-->

### İki Çalışma Modu

Sistemimiz hem ADK hem de A2A yaklaşımlarını gösteren iki çalışma modunu destekler:

#### Mod 1: ADK Sıralı Pipeline

Ajanlar bir `SequentialAgent` olarak organize edilir. Durum `AgentContext` aracılığıyla pipeline boyunca otomatik olarak akar. Bu süreç-içi bir yaklaşımdır — hızlı ve basit.

```
SequentialAgent
├── Adım 1: ResearcherAgent.RunAsync(context)
│   └── Yazar: context.State["researcher_result"] = hamVeri
│
├── Adım 2: AnalysisAgent.RunAsync(context)
│   └── Okur: context.State["researcher_result"]
│   └── Yazar: context.State["analyst_result"] = rapor
│
└── Dönüş: Context'ten toplanmış sonuçlar
```

#### Mod 2: A2A Protokol Tabanlı

Ajanlar A2A protokolü üzerinden iletişim kurar. Orkestratör, `A2AServer` aracılığıyla her ajana `AgentTask` nesneleri gönderir. Her ajanın keşif için kendi `AgentCard`'ı vardır.

```
Orkestratör
├── Adım 1: a2aServer.HandleTaskAsync("researcher", task)
│   └── Döner: Artifact'lı AgentTask
│
├── Adım 2: a2aServer.HandleTaskAsync("analyst", task)
│   └── Girdi Araştırmacı'nın artifact'larını içerir
│   └── Döner: Nihai Artifact'lı AgentTask
│
└── Dönüş: Artifact'lardan çıkarılan sonuçlar
```

<!-- 
📸 GÖRSEL 12: İki Çalışma Modu — Yan Yana Karşılaştırma
Sol panel: "ADK Sıralı Mod"
  - Her iki ajanı saran SequentialAgent gösterilmeli
  - AgentContext paylaşımlı durum nesnesi olarak akıyor (aktarılan bir pano gibi)
  - Etiket: "Süreç-İçi, Paylaşımlı Bellek"
  
Sağ panel: "A2A Protokol Modu"
  - Ortada A2AServer gösterilmeli
  - Araştırmacı ve Analist ayrı servisler olarak
  - AgentTask nesneleri mesaj olarak gönderiliyor (zarflar gibi)
  - Her ajanın yanında Agent Card'lar
  - Etiket: "Protokol Tabanlı, Mesaj İletimi"

Her iki panel aynı girdi/çıktıyı gösterir ama farklı iç mekanikler
Stil: İki panelli karşılaştırma diyagramı, görsel olarak farklı modlar
Boyut: 1200x500px
-->

### Dashboard

Kullanıcı arayüzü tam bir araştırma deneyimi sunar:

- Sistem açıklaması ve protokol rozetleriyle **Hero Bölümü**
- Dört bileşeni gösteren **Mimari Kartlar** (Araştırmacı, Analist, MCP Araçları, Orkestratör)
- Sorgu girişi ve mod seçimli **Araştırma Formu**
- Yürütmenin her aşamasını takip eden **Canlı Pipeline Durumu**
- **Sekmeli Sonuç** görünümü: Araştırma Raporu, Analiz Raporu, Ham Veri, Ajan Olayları
- Geçmiş sorgular ve sonuçlarıyla **Araştırma Geçmişi** tablosu

<!-- 
📸 GÖRSEL 13: Dashboard Ekran Görüntüsü
Çalışan dashboard'un gerçek ekran görüntüsünü alın veya şunları gösteren bir mockup oluşturun:
- "Multi-Agent Research Assistant" başlıklı header
- Bir sırada dört mimari kart (Researcher, Analyst, MCP Tools, Orchestrator)
- Örnek bir sorgu doldurulmuş araştırma formu
- Örnek bir analiz raporu gösteren sekmeli sonuçlar bölümü
- Birkaç girişli geçmiş tablosu
Stil: Gerçek ekran görüntüsü veya yüksek kaliteli mockup
Boyut: 1200x900px (tam sayfa yakalama)
-->

---

## Neden ABP Framework?

.NET uygulama temelimiz olarak ABP Framework'ü seçtik. İşte bir AI ajan projesi için neden doğal bir uyum olduğu:

| ABP Özelliği | Nasıl Kullandık |
|--------------|----------------|
| **Otomatik API Controller'lar** | `ResearchAppService` otomatik olarak REST API endpoint'lerine dönüşür |
| **Dependency Injection** | Ajanların, araçların, orkestratörün, Semantic Kernel'in temiz kaydı |
| **Repository Kalıbı** | MCP araçlarında `IRepository<ResearchRecord>` ile veritabanı işlemleri |
| **Modül Sistemi** | Tüm ajan ekosistemi yapılandırması `AgentEcosystemModule`'da kapsüllenir |
| **Entity Framework Core** | Code-first migration'larla araştırma kaydı kalıcılığı |
| **Dahili Kimlik Doğrulama** | Ajan endpoint'lerini güvence altına almak için OpenIddict |
| **Sağlık Kontrolleri** | Ajan ekosistemi sağlığını izleme |

ABP'nin tek katmanlı şablonu mükemmel bir .NET temeli sağladı — odaklı bir AI projesi için gereksiz karmaşıklık olmadan tüm kurumsal özellikler. Bununla birlikte, ajan mimarisi (MCP, A2A, ADK) framework bağımsızdır ve herhangi bir .NET uygulamasıyla çalışır.

---

## Temel Çıkarımlar

### 1. Protokoller Tamamlayıcıdır, Rakip Değil

MCP, A2A ve ADK farklı problemleri çözer. Birlikte kullanmak tam bir ajan sistemi oluşturur:
- **MCP**: Araç erişimini standartlaştır
- **A2A**: Ajanlar arası iletişimi standartlaştır
- **ADK**: Ajan orkestrasyonunu standartlaştır

### 2. Basit Başla, Sonra Ölçekle

Projemiz her şeyi tek bir süreçte çalıştırıyor (süreç-içi A2A). Ancak A2A protokolünü kullandığımız için her ajan daha sonra kendi mikro servisine çıkarılabilir — temel mantığı değiştirmeden.

### 3. Paylaşımlı Durum > Mesaj İletimi (Basit Durumlar İçin)

ADK'nın paylaşımlı durum ile `AgentContext`'i, süreç-içi senaryolarda A2A mesaj iletiminden daha basit ve hızlıdır. Ajanların ayrı servisler olarak çalışması gerektiğinde A2A kullanın.

### 4. MCP Asıl Oyun Değiştirici

Araçları bir kez tanımlayıp herhangi bir ajanın kullanmasını sağlama yeteneği — otomatik keşif ve yapılandırılmış çağrılarla — büyük miktarda kalıp kodu ortadan kaldırır.

### 5. LLM Soyutlaması Kritik

Semantic Kernel'in `IChatCompletionService`'ini kullanmak, ajan koduna dokunmadan Azure OpenAI, OpenAI, Ollama veya herhangi bir sağlayıcı arasında geçiş yapmanızı sağlar.

<!-- 
📸 GÖRSEL 14: Temel Çıkarımlar — Görsel Özet
5 çıkarım kartıyla bir infografik oluşturun (ızgara veya liste düzeninde):
  1. 🔗 "Tamamlayıcı Protokoller" — Birbirine kenetlenen üç yapboz parçası (MCP, A2A, ADK)
  2. 📈 "Basit Başla, Sonra Ölçekle" — Küçük kutu → Büyük dağıtık sistem
  3. 📋 "Paylaşımlı Durum Kalıbı" — Pano/beyaz tahta metaforu
  4. 🔌 "MCP Oyun Değiştirici" — Birden fazla araca takılan USB-C
  5. 🔄 "LLM Soyutlaması" — OpenAI/Azure/Ollama logoları arasında geçiş ikonu
Stil: İkon açısından zengin çıkarım kartları, temiz ve modern
Boyut: 1200x600px
-->

---

## Sırada Ne Var?

Bu proje çoklu ajan sisteminin temelini göstermektedir. Gelecekteki iyileştirmeler şunları içerebilir:

- **Streaming yanıtlar** — Ajanlar çalışırken gerçek zamanlı güncellemeler (A2A bunu destekler)
- **Daha fazla uzman ajan** — Kod analizi, çeviri, doğrulama ajanları
- **Dağıtık deployment** — Her ajan HTTP tabanlı A2A ile ayrı bir mikro servis olarak
- **Ajan marketplace** — A2A Agent Card'lar aracılığıyla üçüncü parti ajanları keşfet ve entegre et
- **İnsan-döngüde** — İnsan onay adımları için A2A'nın `InputRequired` durumunu kullanma
- **RAG entegrasyonu** — Vektör veritabanı araması için MCP araçları

---

## Kaynaklar

| Kaynak | Bağlantı |
|--------|----------|
| **MCP Spesifikasyonu** | [modelcontextprotocol.io](https://modelcontextprotocol.io) |
| **A2A Spesifikasyonu** | [google.github.io/A2A](https://google.github.io/A2A) |
| **ADK Dokümantasyonu** | [google.github.io/adk-docs](https://google.github.io/adk-docs) |
| **ABP Framework** | [abp.io](https://abp.io) |
| **Semantic Kernel** | [github.com/microsoft/semantic-kernel](https://github.com/microsoft/semantic-kernel) |
| **MCP .NET SDK** | [NuGet: ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol) |
| **A2A .NET SDK** | [NuGet: A2A](https://www.nuget.org/packages/A2A) |
| **Kaynak Kodumuz** | [GitHub Repository](#) |

---

## Sonuç

Çoklu ajan AI sistemi kurmak artık fütürist bir kavram değil — açık protokoller ve modern framework'lerle bugün gerçekleştirilebilir. Araç erişimi için **MCP**'yi, ajan iletişimi için **A2A**'yı ve orkestrasyon için **ADK**'yı birleştirerek, gerçek dünya çoklu ajan işbirliğini gösteren bir Araştırma Asistanı oluşturduk.

ABP Framework ve .NET mükemmel bir temel olduğunu kanıtladı; tamamen AI ajan mimarisine odaklanmamızı sağlayan kurumsal altyapıyı (DI, repository'ler, otomatik API'ler, modüller) sağladı.

Tek LLM çağrısı çağı sona eriyor. **Ajan ekosistemleri** çağı başladı.

<!-- 
📸 GÖRSEL 15: Kapanış Banner'ı
Görsel olarak etkileyici bir kapanış görseli oluşturun:
- Bağlı ajanlar ağı (düğümler ve kenarlar)
- Üç protokol rozeti: MCP ✓, A2A ✓, ADK ✓
- .NET logosu
- Metin: "Ajan Ekosistemleri Çağı"
Stil: Koyu arka plan, parlayan bağlantılar, fütürist his
Boyut: 1200x400px
-->

---

*Bu makale .NET 10.0, Semantic Kernel 1.70.0, Azure OpenAI ve ABP Framework 10.0.2 ile geliştirilen Agent Ecosystem projesinin bir parçasıdır.*

*Sorularınız varsa veya çoklu ajan mimarilerini tartışmak istiyorsanız, aşağıdaki yorumlarda bize ulaşmaktan çekinmeyin!*
