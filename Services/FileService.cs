using Microsoft.JSInterop;
using System.Text.Json;

namespace JsonBlazer.Services;

public class FileService
{
    private readonly IJSRuntime _jsRuntime;
    
    public FileService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    
    public async Task DownloadJsonFileAsync(string fileName, object jsonData)
    {
        var json = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });
        await DownloadTextFileAsync(fileName.EndsWith(".json") ? fileName : $"{fileName}.json", "application/json", json);
    }
    
    public async Task DownloadTextFileAsync(string fileName, string contentType, string content)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("downloadFile", fileName, contentType, content);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to download file: {ex.Message}", ex);
        }
    }
    
    public async Task<FileReadResult?> ReadFileAsTextAsync(IJSObjectReference inputElement)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<FileReadResult>("readFileAsText", inputElement);
            return result;
        }
        catch (JSException ex)
        {
            throw new InvalidOperationException($"Failed to read file: {ex.Message}", ex);
        }
    }
    
    public string GetFileExtensionFromName(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension;
    }
    
    public bool IsJsonFile(string fileName)
    {
        var extension = GetFileExtensionFromName(fileName);
        return extension == ".json";
    }
    
    public bool IsTextFile(string fileName)
    {
        var extension = GetFileExtensionFromName(fileName);
        var textExtensions = new[] { ".txt", ".json", ".xml", ".csv", ".yaml", ".yml", ".md" };
        return textExtensions.Contains(extension);
    }
    
    public async Task<T?> ParseJsonFromFile<T>(string jsonContent) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(jsonContent);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

public class FileReadResult
{
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long Size { get; set; }
    public long LastModified { get; set; }
}