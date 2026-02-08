using System;
using System.Collections.Generic;

namespace AgentEcosystem.Agents.Core;

/// <summary>
/// ADK'nın InvocationContext kavramının .NET uyarlaması.
/// Ajan çalışma bağlamını taşır — oturum, durum ve olay geçmişi.
/// </summary>
public class AgentContext
{
    /// <summary>
    /// Benzersiz oturum kimliği.
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Kullanıcının orijinal sorgusu.
    /// </summary>
    public string UserQuery { get; set; } = string.Empty;

    /// <summary>
    /// Paylaşımlı durum deposu. Ajanlar arası veri paylaşımı için kullanılır.
    /// ADK'daki session.state'in karşılığı.
    /// </summary>
    public Dictionary<string, object> State { get; set; } = new();

    /// <summary>
    /// Oturum boyunca oluşan olayların listesi.
    /// </summary>
    public List<AgentEvent> Events { get; set; } = new();

    /// <summary>
    /// State'ten tipli değer okur.
    /// </summary>
    public T? GetState<T>(string key)
    {
        return State.TryGetValue(key, out var value) && value is T typed
            ? typed
            : default;
    }

    /// <summary>
    /// State'e değer yazar.
    /// </summary>
    public void SetState(string key, object value)
    {
        State[key] = value;
    }

    /// <summary>
    /// State'te belirtilen anahtar var mı kontrol eder.
    /// </summary>
    public bool HasState(string key)
    {
        return State.ContainsKey(key);
    }
}
