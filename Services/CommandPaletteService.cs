using Microsoft.JSInterop;

namespace JsonBlazer.Services;

public class CommandPaletteService : IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Dictionary<string, CommandAction> _commands = new();
    private readonly List<CommandCategory> _categories = new();
    private DotNetObjectReference<CommandPaletteService>? _objectRef;
    private bool _isInitialized = false;

    public CommandPaletteService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        InitializeDefaultCommands();
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        _objectRef = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("initializeCommandPalette", _objectRef);
        _isInitialized = true;
    }

    public async Task ShowPaletteAsync()
    {
        if (!_isInitialized) await InitializeAsync();
        await _jsRuntime.InvokeVoidAsync("showCommandPalette");
    }

    public async Task HidePaletteAsync()
    {
        await _jsRuntime.InvokeVoidAsync("hideCommandPalette");
    }

    public void RegisterCommand(string id, string title, string description, string category, 
        Func<Task> callback, string? icon = null, string[]? aliases = null, string? shortcut = null)
    {
        _commands[id] = new CommandAction
        {
            Id = id,
            Title = title,
            Description = description,
            Category = category,
            Callback = callback,
            Icon = icon,
            Aliases = aliases?.ToList() ?? new List<string>(),
            Shortcut = shortcut
        };

        // Ensure category exists
        if (!_categories.Any(c => c.Name == category))
        {
            _categories.Add(new CommandCategory { Name = category, Order = GetCategoryOrder(category) });
        }
    }

    public void RegisterCommand(string id, string title, string description, string category, 
        Action callback, string? icon = null, string[]? aliases = null, string? shortcut = null)
    {
        RegisterCommand(id, title, description, category, () =>
        {
            callback();
            return Task.CompletedTask;
        }, icon, aliases, shortcut);
    }

    [JSInvokable]
    public async Task ExecuteCommand(string commandId)
    {
        if (_commands.TryGetValue(commandId, out var command))
        {
            try
            {
                await command.Callback();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command {commandId}: {ex.Message}");
                await _jsRuntime.InvokeVoidAsync("showCommandError", commandId, ex.Message);
            }
        }
    }

    [JSInvokable]
    public List<CommandAction> SearchCommands(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _commands.Values.OrderBy(c => GetCategoryOrder(c.Category))
                                  .ThenBy(c => c.Title)
                                  .ToList();
        }

        query = query.ToLowerInvariant();
        var results = new List<(CommandAction command, int score)>();

        foreach (var command in _commands.Values)
        {
            int score = CalculateSearchScore(command, query);
            if (score > 0)
            {
                results.Add((command, score));
            }
        }

        return results.OrderByDescending(r => r.score)
                     .ThenBy(r => GetCategoryOrder(r.command.Category))
                     .Select(r => r.command)
                     .ToList();
    }

    private void InitializeDefaultCommands()
    {
        // JSON Operations
        RegisterCommand("format", "Format JSON", "Format and prettify JSON with proper indentation", 
            "JSON Operations", () => Task.CompletedTask, "format_align_left", new[] { "prettify", "indent" }, "Ctrl+Enter");

        RegisterCommand("minify", "Minify JSON", "Remove whitespace to create compact JSON", 
            "JSON Operations", () => Task.CompletedTask, "compress", new[] { "compact", "minimize" }, "Ctrl+Shift+M");

        RegisterCommand("validate", "Validate JSON", "Check JSON syntax and structure", 
            "JSON Operations", () => Task.CompletedTask, "check_circle", new[] { "check", "verify" }, "Ctrl+Shift+V");

        RegisterCommand("copy", "Copy to Clipboard", "Copy formatted JSON to clipboard", 
            "JSON Operations", () => Task.CompletedTask, "content_copy", new[] { "clipboard" }, "Ctrl+Shift+C");

        // Navigation
        RegisterCommand("nav-formatter", "Go to Formatter", "Navigate to JSON formatter tool", 
            "Navigation", () => Task.CompletedTask, "code", new[] { "format page" }, "Alt+1");

        RegisterCommand("nav-validator", "Go to Validator", "Navigate to JSON validator tool", 
            "Navigation", () => Task.CompletedTask, "verified", new[] { "validate page" }, "Alt+2");

        RegisterCommand("nav-converter", "Go to Converter", "Navigate to JSON converter tool", 
            "Navigation", () => Task.CompletedTask, "swap_horiz", new[] { "convert page" }, "Alt+3");

        RegisterCommand("nav-diff", "Go to Diff Tool", "Navigate to JSON comparison tool", 
            "Navigation", () => Task.CompletedTask, "compare", new[] { "compare page" }, "Alt+4");

        RegisterCommand("nav-generator", "Go to Generator", "Navigate to mock data generator", 
            "Navigation", () => Task.CompletedTask, "auto_fix_high", new[] { "generate page" }, "Alt+5");

        RegisterCommand("nav-query", "Go to Query Tool", "Navigate to JSONPath query tool", 
            "Navigation", () => Task.CompletedTask, "search", new[] { "jsonpath page" }, "Alt+6");

        RegisterCommand("nav-docs", "Go to Documentation", "Navigate to documentation", 
            "Navigation", () => Task.CompletedTask, "description", new[] { "docs page" }, "Alt+7");

        RegisterCommand("nav-api", "Go to API Testing", "Navigate to API testing tool", 
            "Navigation", () => Task.CompletedTask, "api", new[] { "api page" }, "Alt+8");

        // Tools & Utilities
        RegisterCommand("help", "Show Help", "Display keyboard shortcuts and help", 
            "Tools & Utilities", () => Task.CompletedTask, "help", new[] { "shortcuts", "keys" }, "F1");

        RegisterCommand("fullscreen", "Toggle Fullscreen", "Enter or exit fullscreen mode", 
            "Tools & Utilities", () => Task.CompletedTask, "fullscreen", new[] { "maximize" }, "F11");

        RegisterCommand("theme", "Toggle Theme", "Switch between light and dark theme", 
            "Tools & Utilities", () => Task.CompletedTask, "brightness_6", new[] { "dark mode", "light mode" });

        RegisterCommand("clear", "Clear All", "Clear all JSON content", 
            "Tools & Utilities", () => Task.CompletedTask, "clear_all", new[] { "reset", "empty" });

        // Advanced Features  
        RegisterCommand("generate-schema", "Generate Schema", "Create JSON schema from data", 
            "Advanced Features", () => Task.CompletedTask, "schema", new[] { "json schema" });

        RegisterCommand("transform", "Transform Data", "Apply data transformations", 
            "Advanced Features", () => Task.CompletedTask, "transform", new[] { "modify", "change" });

        RegisterCommand("benchmark", "Performance Test", "Run performance benchmarks", 
            "Advanced Features", () => Task.CompletedTask, "speed", new[] { "perf", "test" });

        RegisterCommand("export", "Export Results", "Export to various formats", 
            "Advanced Features", () => Task.CompletedTask, "download", new[] { "save", "file" });
    }

    private int CalculateSearchScore(CommandAction command, string query)
    {
        int score = 0;

        // Exact title match
        if (command.Title.ToLowerInvariant() == query) score += 100;
        
        // Title starts with query
        if (command.Title.ToLowerInvariant().StartsWith(query)) score += 80;
        
        // Title contains query
        if (command.Title.ToLowerInvariant().Contains(query)) score += 60;

        // Description contains query
        if (command.Description.ToLowerInvariant().Contains(query)) score += 40;

        // Alias exact match
        if (command.Aliases.Any(a => a.ToLowerInvariant() == query)) score += 90;

        // Alias contains query
        if (command.Aliases.Any(a => a.ToLowerInvariant().Contains(query))) score += 50;

        // Category match
        if (command.Category.ToLowerInvariant().Contains(query)) score += 30;

        return score;
    }

    private int GetCategoryOrder(string category)
    {
        return category switch
        {
            "JSON Operations" => 1,
            "Navigation" => 2,
            "Tools & Utilities" => 3,
            "Advanced Features" => 4,
            _ => 5
        };
    }

    public void Dispose()
    {
        _objectRef?.Dispose();
    }
}

public class CommandAction
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Func<Task> Callback { get; set; } = () => Task.CompletedTask;
    public string? Icon { get; set; }
    public List<string> Aliases { get; set; } = new();
    public string? Shortcut { get; set; }
}

public class CommandCategory
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
}