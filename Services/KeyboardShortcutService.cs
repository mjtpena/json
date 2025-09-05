using Microsoft.JSInterop;

namespace JsonBlazer.Services;

public class KeyboardShortcutService : IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Dictionary<string, Func<Task>> _shortcuts = new();
    private DotNetObjectReference<KeyboardShortcutService>? _objectRef;
    private bool _isInitialized = false;

    public KeyboardShortcutService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        _objectRef = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("initializeKeyboardShortcuts", _objectRef);
        _isInitialized = true;
    }

    public void RegisterShortcut(string keys, Func<Task> callback)
    {
        var normalizedKeys = NormalizeShortcut(keys);
        _shortcuts[normalizedKeys] = callback;
    }

    public void RegisterShortcut(string keys, Action callback)
    {
        RegisterShortcut(keys, () =>
        {
            callback();
            return Task.CompletedTask;
        });
    }

    [JSInvokable]
    public async Task HandleShortcut(string keys)
    {
        var normalizedKeys = NormalizeShortcut(keys);
        if (_shortcuts.TryGetValue(normalizedKeys, out var callback))
        {
            try
            {
                await callback();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing keyboard shortcut {keys}: {ex.Message}");
            }
        }
    }

    private string NormalizeShortcut(string keys)
    {
        // Normalize shortcut strings to handle variations
        return keys.ToLowerInvariant()
                  .Replace("command", "ctrl") // Mac compatibility
                  .Replace("cmd", "ctrl")
                  .Replace(" ", "")
                  .Replace("++", "+");
    }

    public void RegisterDefaultShortcuts()
    {
        // Common shortcuts that will be handled by individual pages
        var shortcuts = new Dictionary<string, string>
        {
            { "ctrl+enter", "Format JSON" },
            { "ctrl+shift+m", "Minify JSON" },
            { "ctrl+d", "Duplicate Line" },
            { "ctrl+shift+v", "Validate JSON" },
            { "ctrl+shift+c", "Copy to Clipboard" },
            { "ctrl+shift+f", "Format and Copy" },
            { "ctrl+shift+d", "Compare JSON" },
            { "ctrl+shift+g", "Generate Mock Data" },
            { "ctrl+shift+q", "Execute Query" },
            { "ctrl+shift+t", "Transform Data" },
            { "escape", "Close Dialog/Modal" },
            { "ctrl+/", "Toggle Help" },
            { "ctrl+k", "Open Command Palette" },
            { "alt+1", "Navigate to Formatter" },
            { "alt+2", "Navigate to Validator" },
            { "alt+3", "Navigate to Converter" },
            { "alt+4", "Navigate to Diff" },
            { "alt+5", "Navigate to Generator" },
            { "alt+6", "Navigate to Query" },
            { "alt+7", "Navigate to Documentation" },
            { "alt+8", "Navigate to API Test" },
            { "f1", "Show Help" },
            { "f11", "Toggle Fullscreen" }
        };
    }

    public List<ShortcutInfo> GetShortcutList()
    {
        return new List<ShortcutInfo>
        {
            new("Ctrl+Enter", "Format JSON", "Formats the current JSON with proper indentation"),
            new("Ctrl+Shift+M", "Minify JSON", "Removes whitespace and creates compact JSON"),
            new("Ctrl+Shift+V", "Validate JSON", "Validates JSON syntax and structure"),
            new("Ctrl+Shift+C", "Copy to Clipboard", "Copies formatted JSON to clipboard"),
            new("Ctrl+Shift+F", "Format & Copy", "Formats JSON and copies to clipboard"),
            new("Ctrl+D", "Duplicate Line", "Duplicates the current line or selection"),
            new("Ctrl+Shift+D", "Compare JSON", "Opens JSON comparison tool"),
            new("Ctrl+Shift+G", "Generate Data", "Opens mock data generator"),
            new("Ctrl+Shift+Q", "Execute Query", "Runs JSONPath query"),
            new("Ctrl+K", "Command Palette", "Opens quick command palette"),
            new("Alt+1-8", "Navigate", "Quick navigation between tools"),
            new("F1", "Help", "Shows keyboard shortcuts help"),
            new("F11", "Fullscreen", "Toggles fullscreen mode"),
            new("Escape", "Close", "Closes dialogs and modals"),
            new("Ctrl+/", "Toggle Help", "Shows/hides help overlay")
        };
    }

    public void Dispose()
    {
        _objectRef?.Dispose();
    }
}

public class ShortcutInfo
{
    public string Keys { get; set; }
    public string Action { get; set; }
    public string Description { get; set; }

    public ShortcutInfo(string keys, string action, string description)
    {
        Keys = keys;
        Action = action;
        Description = description;
    }
}