using Microsoft.JSInterop;
using System.Text.Json;

namespace JsonBlazer.Services;

public class ThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isDarkMode = false;
    
    public event Action<bool>? ThemeChanged;
    
    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    
    public bool IsDarkMode => _isDarkMode;
    
    public async Task InitializeAsync()
    {
        try
        {
            var savedTheme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "json-tool-theme");
            if (!string.IsNullOrEmpty(savedTheme))
            {
                _isDarkMode = savedTheme == "dark";
            }
            else
            {
                // Check system preference
                _isDarkMode = await _jsRuntime.InvokeAsync<bool>("window.matchMedia", "(prefers-color-scheme: dark)").ConfigureAwait(false);
            }
            
            await ApplyThemeAsync();
        }
        catch
        {
            // Fallback to light mode if localStorage is not available
            _isDarkMode = false;
        }
    }
    
    public async Task ToggleThemeAsync()
    {
        _isDarkMode = !_isDarkMode;
        await SaveThemeAsync();
        await ApplyThemeAsync();
        ThemeChanged?.Invoke(_isDarkMode);
    }
    
    public async Task SetThemeAsync(bool isDark)
    {
        if (_isDarkMode != isDark)
        {
            _isDarkMode = isDark;
            await SaveThemeAsync();
            await ApplyThemeAsync();
            ThemeChanged?.Invoke(_isDarkMode);
        }
    }
    
    private async Task SaveThemeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "json-tool-theme", _isDarkMode ? "dark" : "light");
        }
        catch
        {
            // Handle localStorage not available
        }
    }
    
    private async Task ApplyThemeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("setTheme", _isDarkMode);
        }
        catch
        {
            // Handle JS interop not available
        }
    }
    
    public string GetThemeClass() => _isDarkMode ? "dark-theme" : "light-theme";
    
    public string GetBackgroundClass() => _isDarkMode ? "dark-bg" : "light-bg";
    
    public string GetTextClass() => _isDarkMode ? "dark-text" : "light-text";
    
    public string GetCardClass() => _isDarkMode ? "dark-card" : "light-card";
}