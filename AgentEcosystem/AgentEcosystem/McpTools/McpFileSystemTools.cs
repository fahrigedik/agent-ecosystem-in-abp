using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace AgentEcosystem.McpTools;

/// <summary>
/// MCP File System Tools — integrated with Model Context Protocol SDK.
/// 
/// Provides MCP tools for saving and reading research results
/// to/from the file system. LLMs can use these tools to
/// save research reports as files.
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
    /// Saves a research result as a file.
    /// </summary>
    [McpServerTool(Name = "save_research_to_file")]
    [Description("Saves a research result with the specified file name. Saving in Markdown format is recommended.")]
    public async Task<string> SaveResearchToFileAsync(
        [Description("File name (e.g., python-innovations.md)")] string fileName,
        [Description("File content (Markdown format recommended)")] string content)
    {
        try
        {
            var safeName = SanitizeFileName(fileName);
            var filePath = Path.Combine(_basePath, safeName);
            
            // If the file already exists, generate a new name
            if (File.Exists(filePath))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(safeName);
                var ext = Path.GetExtension(safeName);
                safeName = $"{nameWithoutExt}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{ext}";
                filePath = Path.Combine(_basePath, safeName);
            }

            await File.WriteAllTextAsync(filePath, content);
            
            _logger.LogInformation("[MCP:FileSystem] File saved: {FilePath}", filePath);
            return $"File saved successfully: {safeName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MCP:FileSystem] Error saving file: {FileName}", fileName);
            return $"Failed to save file: {ex.Message}";
        }
    }

    /// <summary>
    /// Reads a previously saved research file.
    /// </summary>
    [McpServerTool(Name = "read_research_file")]
    [Description("Reads a previously saved research file.")]
    public async Task<string> ReadResearchFileAsync(
        [Description("File name to read")] string fileName)
    {
        try
        {
            var filePath = Path.Combine(_basePath, SanitizeFileName(fileName));
            if (!File.Exists(filePath))
                return $"File not found: {fileName}";

            var content = await File.ReadAllTextAsync(filePath);
            _logger.LogInformation("[MCP:FileSystem] File read: {FilePath}", filePath);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MCP:FileSystem] Error reading file: {FileName}", fileName);
            return $"Failed to read file: {ex.Message}";
        }
    }

    /// <summary>
    /// Lists saved research files.
    /// </summary>
    [McpServerTool(Name = "list_research_files")]
    [Description("Lists all research files in the ResearchResults folder.")]
    public Task<string> ListResearchFilesAsync()
    {
        try
        {
            var files = Directory.GetFiles(_basePath);
            if (files.Length == 0)
                return Task.FromResult("No research files saved yet.");

            var fileInfos = files.Select(f =>
            {
                var info = new FileInfo(f);
                return $"  - {info.Name} ({info.Length / 1024.0:F1} KB, {info.LastWriteTimeUtc:yyyy-MM-dd HH:mm})";
            });

            _logger.LogInformation("[MCP:FileSystem] {Count} files listed", files.Length);
            return Task.FromResult($"Research files ({files.Length} total):\n{string.Join("\n", fileInfos)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MCP:FileSystem] Error listing files");
            return Task.FromResult($"Error listing files: {ex.Message}");
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c, '_');
        return fileName;
    }
}
