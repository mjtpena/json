using Microsoft.JSInterop;

namespace JsonBlazer.Services;

public class ClipboardService
{
    private readonly IJSRuntime _jsRuntime;
    
    public ClipboardService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    
    public async Task<bool> CopyToClipboardAsync(string text)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
            return true;
        }
        catch (JSException)
        {
            // Fallback for older browsers or insecure contexts
            try
            {
                await _jsRuntime.InvokeVoidAsync("copyToClipboardFallback", text);
                return true;
            }
            catch
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<string?> ReadFromClipboardAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("navigator.clipboard.readText");
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<bool> IsClipboardSupportedAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("isClipboardSupported");
        }
        catch
        {
            return false;
        }
    }
}