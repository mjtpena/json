using Microsoft.JSInterop;

namespace JsonBlazer.Services;

public class MonacoEditorService
{
    private readonly IJSRuntime _jsRuntime;
    
    public MonacoEditorService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    
    public async Task InitializeEditorAsync(string containerId, string language = "json", string theme = "vs-dark")
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("initializeMonacoEditor", containerId, language, theme);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize Monaco Editor: {ex.Message}");
        }
    }
    
    public async Task SetEditorValueAsync(string containerId, string value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("setMonacoEditorValue", containerId, value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to set Monaco Editor value: {ex.Message}");
        }
    }
    
    public async Task<string?> GetEditorValueAsync(string containerId)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("getMonacoEditorValue", containerId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get Monaco Editor value: {ex.Message}");
            return null;
        }
    }
    
    public async Task SetEditorThemeAsync(string theme = "vs-dark")
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("setMonacoEditorTheme", theme);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to set Monaco Editor theme: {ex.Message}");
        }
    }
    
    public async Task FormatDocumentAsync(string containerId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("formatMonacoDocument", containerId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to format Monaco document: {ex.Message}");
        }
    }
    
    public async Task SetEditorLanguageAsync(string containerId, string language)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("setMonacoEditorLanguage", containerId, language);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to set Monaco Editor language: {ex.Message}");
        }
    }
}