using Microsoft.JSInterop;

namespace JsonBlazer.Services;

public class NotificationService
{
    private readonly IJSRuntime _jsRuntime;
    
    public NotificationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    
    public async Task ShowSuccessAsync(string message, int duration = 3000)
    {
        await ShowNotificationAsync(message, "success", duration);
    }
    
    public async Task ShowErrorAsync(string message, int duration = 5000)
    {
        await ShowNotificationAsync(message, "error", duration);
    }
    
    public async Task ShowInfoAsync(string message, int duration = 3000)
    {
        await ShowNotificationAsync(message, "info", duration);
    }
    
    public async Task ShowWarningAsync(string message, int duration = 4000)
    {
        await ShowNotificationAsync(message, "warning", duration);
    }
    
    private async Task ShowNotificationAsync(string message, string type, int duration)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("showNotification", message, type, duration);
        }
        catch
        {
            // Fallback - could log to console or use alternative notification method
            Console.WriteLine($"[{type.ToUpper()}] {message}");
        }
    }
}

public enum NotificationType
{
    Success,
    Error,
    Info,
    Warning
}