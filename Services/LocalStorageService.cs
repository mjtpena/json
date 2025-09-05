using Microsoft.JSInterop;
using System.Text.Json;

namespace JsonBlazer.Services;

public class LocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    
    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    
    public async Task<bool> SetItemAsync<T>(string key, T value)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            return await _jsRuntime.InvokeAsync<bool>("localStorageHelper.setItem", key, serializedValue);
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<T?> GetItemAsync<T>(string key) where T : class
    {
        try
        {
            var serializedValue = await _jsRuntime.InvokeAsync<string?>("localStorageHelper.getItem", key);
            if (string.IsNullOrEmpty(serializedValue))
                return null;
                
            return JsonSerializer.Deserialize<T>(serializedValue);
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorageHelper.getItem", key);
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<bool> SetStringAsync(string key, string value)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("localStorageHelper.setItem", key, value);
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<bool> RemoveItemAsync(string key)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("localStorageHelper.removeItem", key);
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<bool> ClearAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("localStorageHelper.clear");
        }
        catch
        {
            return false;
        }
    }
}

// Constants for localStorage keys
public static class LocalStorageKeys
{
    public const string UserPreferences = "jsonblazer_preferences";
    public const string RecentFiles = "jsonblazer_recent_files";
    public const string SavedSnippets = "jsonblazer_snippets";
    public const string ThemeSettings = "jsonblazer_theme";
    public const string EditorSettings = "jsonblazer_editor";
}