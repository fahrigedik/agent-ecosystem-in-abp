using System;
using System.Collections.Generic;

namespace AgentEcosystem.Agents.Core;

/// <summary>
/// ADK'nın Event kavramının .NET uyarlaması.
/// Oturum sırasında meydana gelen olayları temsil eder.
/// </summary>
public class AgentEvent
{
    /// <summary>
    /// Olayı oluşturan ajanın adı.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Olay durumu: working, completed, failed.
    /// </summary>
    public string Status { get; set; } = "working";

    /// <summary>
    /// Olay içeriği — ajanın ürettiği metin.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Olayla ilişkili eylemler (escalate, transfer, state updates).
    /// </summary>
    public EventActions? Actions { get; set; }

    /// <summary>
    /// Olayın oluşturulma zamanı.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// ADK'nın EventActions kavramının .NET uyarlaması.
/// Olayla birlikte gerçekleştirilecek eylemleri tanımlar.
/// </summary>
public class EventActions
{
    /// <summary>
    /// true ise akış yukarı ajana (ebeveyn ajana) yükseltilir.
    /// Loop ajanlarında döngüyü sonlandırmak için kullanılır.
    /// </summary>
    public bool Escalate { get; set; }

    /// <summary>
    /// Belirtilen ajana transfer. LLM-driven delegation için kullanılır.
    /// </summary>
    public string? TransferToAgent { get; set; }

    /// <summary>
    /// State'e uygulanacak güncellemeler.
    /// </summary>
    public Dictionary<string, object>? StateUpdates { get; set; }
}
