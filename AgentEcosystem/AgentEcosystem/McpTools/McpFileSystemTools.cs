using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace AgentEcosystem.McpTools;

/// <summary>
/// MCP Dosya Sistemi Araçları — Model Context Protocol SDK ile entegre.
/// 
/// Araştırma sonuçlarını dosya sistemine kaydetmek ve okumak için
/// MCP araçları sağlar. LLM'ler bu araçları kullanarak
/// araştırma raporlarını dosya olarak kaydedebilir.
/// </summary>
[McpServerToolType]
public class McpFileSystemTools
{
    private readonly ILogger<McpFileSystemTools> _logger;
    private readonly string _basePath;

    public McpFileSystemTools(ILogger<McpFileSystemTools> logger)
    {
        _logger = logger;
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "ResearchResults");
        Directory.CreateDirectory(_basePath);
    }

    /// <summary>
    /// Araştırma sonucunu dosya olarak kaydeder.
    /// </summary>
    [McpServerTool(Name = "save_research_to_file")]
    [Description("Araştırma sonucunu belirtilen dosya adıyla kaydeder. Markdown formatında kaydetmek önerilir.")]
    public async Task<string> SaveResearchToFileAsync(
        [Description("Dosya adı (örn: python-yenilikleri.md)")] string fileName,
        [Description("Dosya içeriği (Markdown formatı önerilir)")] string content)
    {
        try
        {
            var safeName = SanitizeFileName(fileName);
            var filePath = Path.Combine(_basePath, safeName);
            
            // Dosya zaten varsa yeni isim oluştur
            if (File.Exists(filePath))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(safeName);
                var ext = Path.GetExtension(safeName);
                safeName = $"{nameWithoutExt}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{ext}";
                filePath = Path.Combine(_basePath, safeName);
            }

            await File.WriteAllTextAsync(filePath, content);
            
            _logger.LogInformation("[MCP:FileSystem] Dosya kaydedildi: {FilePath}", filePath);
            return $"Dosya başarıyla kaydedildi: {safeName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MCP:FileSystem] Dosya kaydetme hatası: {FileName}", fileName);
            return $"Dosya kaydedilemedi: {ex.Message}";
        }
    }

    /// <summary>
    /// Kaydedilmiş araştırma dosyasını okur.
    /// </summary>
    [McpServerTool(Name = "read_research_file")]
    [Description("Daha önce kaydedilmiş bir araştırma dosyasını okur.")]
    public async Task<string> ReadResearchFileAsync(
        [Description("Okunacak dosya adı")] string fileName)
    {
        try
        {
            var filePath = Path.Combine(_basePath, SanitizeFileName(fileName));
            if (!File.Exists(filePath))
                return $"Dosya bulunamadı: {fileName}";

            var content = await File.ReadAllTextAsync(filePath);
            _logger.LogInformation("[MCP:FileSystem] Dosya okundu: {FilePath}", filePath);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MCP:FileSystem] Dosya okuma hatası: {FileName}", fileName);
            return $"Dosya okunamadı: {ex.Message}";
        }
    }

    /// <summary>
    /// Kayıtlı araştırma dosyalarını listeler.
    /// </summary>
    [McpServerTool(Name = "list_research_files")]
    [Description("ResearchResults klasöründeki tüm araştırma dosyalarını listeler.")]
    public Task<string> ListResearchFilesAsync()
    {
        try
        {
            var files = Directory.GetFiles(_basePath);
            if (files.Length == 0)
                return Task.FromResult("Henüz kayıtlı araştırma dosyası bulunmuyor.");

            var fileInfos = files.Select(f =>
            {
                var info = new FileInfo(f);
                return $"  - {info.Name} ({info.Length / 1024.0:F1} KB, {info.LastWriteTimeUtc:yyyy-MM-dd HH:mm})";
            });

            _logger.LogInformation("[MCP:FileSystem] {Count} dosya listelendi", files.Length);
            return Task.FromResult($"Kayıtlı dosyalar ({files.Length} adet):\n{string.Join("\n", fileInfos)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MCP:FileSystem] Dosya listeleme hatası");
            return Task.FromResult($"Dosya listeleme hatası: {ex.Message}");
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c, '_');
        return fileName;
    }
}
