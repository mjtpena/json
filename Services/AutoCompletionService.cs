using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonBlazer.Services;

public class AutoCompletionService : IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<AutoCompletionService>? _objectRef;
    private bool _isInitialized = false;
    private readonly Dictionary<string, List<CompletionItem>> _completionCache = new();
    private readonly List<JsonSchema> _schemas = new();

    public AutoCompletionService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        InitializeBuiltInCompletions();
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        _objectRef = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("initializeAutoCompletion", _objectRef);
        _isInitialized = true;
    }

    public async Task RegisterEditor(string editorId, AutoCompletionOptions options)
    {
        if (!_isInitialized) await InitializeAsync();
        
        await _jsRuntime.InvokeVoidAsync("registerAutoCompletionEditor", editorId, new
        {
            options.EnableSmartCompletion,
            options.EnableSchemaValidation,
            options.EnableContextualHelp,
            options.MaxSuggestions,
            options.TriggerCharacters,
            options.CompletionDelay
        });
    }

    [JSInvokable]
    public async Task<List<CompletionItem>> GetCompletions(string editorId, string text, int position, CompletionContext context)
    {
        try
        {
            var completions = new List<CompletionItem>();
            
            // Analyze context
            var jsonContext = AnalyzeJsonContext(text, position);
            
            // Get completions based on context
            switch (jsonContext.Type)
            {
                case JsonContextType.PropertyKey:
                    completions.AddRange(GetPropertyKeyCompletions(jsonContext));
                    break;
                    
                case JsonContextType.PropertyValue:
                    completions.AddRange(GetPropertyValueCompletions(jsonContext));
                    break;
                    
                case JsonContextType.ArrayElement:
                    completions.AddRange(GetArrayElementCompletions(jsonContext));
                    break;
                    
                case JsonContextType.Root:
                    completions.AddRange(GetRootCompletions());
                    break;
            }
            
            // Add built-in completions
            completions.AddRange(GetBuiltInCompletions(jsonContext));
            
            // Filter and sort by relevance
            var query = GetQueryFromPosition(text, position);
            if (!string.IsNullOrEmpty(query))
            {
                completions = FilterCompletions(completions, query);
            }
            
            return completions.Take(50).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting completions: {ex.Message}");
            return new List<CompletionItem>();
        }
    }

    [JSInvokable]
    public async Task<CompletionDocumentation?> GetCompletionDocumentation(string itemId)
    {
        // Return detailed documentation for a completion item
        return GetDocumentationForItem(itemId);
    }

    [JSInvokable]
    public async Task<List<DiagnosticItem>> ValidateJson(string text)
    {
        var diagnostics = new List<DiagnosticItem>();
        
        try
        {
            // Parse JSON for syntax errors
            JsonDocument.Parse(text);
        }
        catch (JsonException ex)
        {
            diagnostics.Add(new DiagnosticItem
            {
                Message = ex.Message,
                Severity = DiagnosticSeverity.Error,
                StartLine = GetLineFromBytePosition(text, (int)(ex.BytePositionInLine ?? 0)),
                StartColumn = (int)(ex.BytePositionInLine ?? 0),
                EndLine = GetLineFromBytePosition(text, (int)(ex.BytePositionInLine ?? 0)),
                EndColumn = (int)(ex.BytePositionInLine ?? 0) + 1
            });
        }
        
        // Schema validation
        if (_schemas.Any())
        {
            diagnostics.AddRange(ValidateAgainstSchemas(text));
        }
        
        // Lint checks
        diagnostics.AddRange(LintJson(text));
        
        return diagnostics;
    }

    private JsonContext AnalyzeJsonContext(string text, int position)
    {
        var context = new JsonContext { Position = position };
        
        try
        {
            // Find the context at the cursor position
            var beforeCursor = text.Substring(0, Math.Min(position, text.Length));
            var afterCursor = position < text.Length ? text.Substring(position) : "";
            
            // Determine if we're in a key, value, or other context
            var inString = IsInsideString(beforeCursor);
            var lastChar = beforeCursor.TrimEnd().LastOrDefault();
            
            if (inString)
            {
                // Check if we're in a property key or value
                var keyValueSeparatorIndex = beforeCursor.LastIndexOf(':');
                var lastOpenBrace = beforeCursor.LastIndexOf('{');
                var lastCloseBrace = beforeCursor.LastIndexOf('}');
                
                if (keyValueSeparatorIndex > lastOpenBrace && keyValueSeparatorIndex > lastCloseBrace)
                {
                    context.Type = JsonContextType.PropertyValue;
                }
                else
                {
                    context.Type = JsonContextType.PropertyKey;
                }
            }
            else
            {
                switch (lastChar)
                {
                    case '{':
                    case ',':
                        context.Type = JsonContextType.PropertyKey;
                        break;
                    case ':':
                        context.Type = JsonContextType.PropertyValue;
                        break;
                    case '[':
                        context.Type = JsonContextType.ArrayElement;
                        break;
                    default:
                        context.Type = JsonContextType.Root;
                        break;
                }
            }
            
            // Extract current path
            context.Path = ExtractJsonPath(beforeCursor);
            context.ParentObject = GetParentObjectType(beforeCursor);
        }
        catch
        {
            context.Type = JsonContextType.Root;
        }
        
        return context;
    }

    private List<CompletionItem> GetPropertyKeyCompletions(JsonContext context)
    {
        var completions = new List<CompletionItem>();
        
        // Common JSON properties
        var commonProperties = new[]
        {
            new CompletionItem { Label = "id", Kind = CompletionItemKind.Property, Detail = "Unique identifier" },
            new CompletionItem { Label = "name", Kind = CompletionItemKind.Property, Detail = "Name property" },
            new CompletionItem { Label = "type", Kind = CompletionItemKind.Property, Detail = "Type property" },
            new CompletionItem { Label = "value", Kind = CompletionItemKind.Property, Detail = "Value property" },
            new CompletionItem { Label = "description", Kind = CompletionItemKind.Property, Detail = "Description text" },
            new CompletionItem { Label = "created", Kind = CompletionItemKind.Property, Detail = "Creation timestamp" },
            new CompletionItem { Label = "updated", Kind = CompletionItemKind.Property, Detail = "Last update timestamp" },
            new CompletionItem { Label = "status", Kind = CompletionItemKind.Property, Detail = "Status indicator" },
            new CompletionItem { Label = "data", Kind = CompletionItemKind.Property, Detail = "Data payload" }
        };
        
        completions.AddRange(commonProperties);
        
        // Schema-based completions if available
        if (context.ParentObject != null && _completionCache.ContainsKey(context.ParentObject))
        {
            completions.AddRange(_completionCache[context.ParentObject]);
        }
        
        return completions;
    }

    private List<CompletionItem> GetPropertyValueCompletions(JsonContext context)
    {
        var completions = new List<CompletionItem>();
        
        // Boolean values
        completions.Add(new CompletionItem 
        { 
            Label = "true", 
            Kind = CompletionItemKind.Value, 
            Detail = "Boolean true",
            InsertText = "true"
        });
        completions.Add(new CompletionItem 
        { 
            Label = "false", 
            Kind = CompletionItemKind.Value, 
            Detail = "Boolean false",
            InsertText = "false"
        });
        
        // Null value
        completions.Add(new CompletionItem 
        { 
            Label = "null", 
            Kind = CompletionItemKind.Value, 
            Detail = "Null value",
            InsertText = "null"
        });
        
        // Common string values
        var commonValues = new[]
        {
            "active", "inactive", "pending", "completed", "failed",
            "success", "error", "warning", "info",
            "public", "private", "draft", "published"
        };
        
        foreach (var value in commonValues)
        {
            completions.Add(new CompletionItem
            {
                Label = $"\"{value}\"",
                Kind = CompletionItemKind.Value,
                Detail = $"String value: {value}",
                InsertText = $"\"{value}\""
            });
        }
        
        // Number values
        completions.Add(new CompletionItem 
        { 
            Label = "0", 
            Kind = CompletionItemKind.Value, 
            Detail = "Number zero",
            InsertText = "0"
        });
        
        return completions;
    }

    private List<CompletionItem> GetArrayElementCompletions(JsonContext context)
    {
        var completions = new List<CompletionItem>();
        
        // Object template
        completions.Add(new CompletionItem
        {
            Label = "{ }",
            Kind = CompletionItemKind.Snippet,
            Detail = "Empty object",
            InsertText = "{\n  \n}"
        });
        
        // Array template
        completions.Add(new CompletionItem
        {
            Label = "[ ]",
            Kind = CompletionItemKind.Snippet,
            Detail = "Empty array",
            InsertText = "[]"
        });
        
        return completions;
    }

    private List<CompletionItem> GetRootCompletions()
    {
        var completions = new List<CompletionItem>();
        
        // Root object template
        completions.Add(new CompletionItem
        {
            Label = "Object",
            Kind = CompletionItemKind.Snippet,
            Detail = "JSON object",
            InsertText = "{\n  \n}"
        });
        
        // Root array template
        completions.Add(new CompletionItem
        {
            Label = "Array",
            Kind = CompletionItemKind.Snippet,
            Detail = "JSON array",
            InsertText = "[]"
        });
        
        return completions;
    }

    private List<CompletionItem> GetBuiltInCompletions(JsonContext context)
    {
        var completions = new List<CompletionItem>();
        
        // JSON Schema keywords
        if (context.Type == JsonContextType.PropertyKey)
        {
            var schemaKeywords = new[]
            {
                "$schema", "$id", "title", "description", "type", "properties", 
                "items", "required", "enum", "const", "examples"
            };
            
            foreach (var keyword in schemaKeywords)
            {
                completions.Add(new CompletionItem
                {
                    Label = $"\"{keyword}\"",
                    Kind = CompletionItemKind.Keyword,
                    Detail = $"JSON Schema keyword: {keyword}",
                    InsertText = $"\"{keyword}\": "
                });
            }
        }
        
        return completions;
    }

    private void InitializeBuiltInCompletions()
    {
        // Initialize common completion patterns
        _completionCache["user"] = new List<CompletionItem>
        {
            new() { Label = "username", Kind = CompletionItemKind.Property },
            new() { Label = "email", Kind = CompletionItemKind.Property },
            new() { Label = "password", Kind = CompletionItemKind.Property },
            new() { Label = "firstName", Kind = CompletionItemKind.Property },
            new() { Label = "lastName", Kind = CompletionItemKind.Property }
        };
        
        _completionCache["api"] = new List<CompletionItem>
        {
            new() { Label = "endpoint", Kind = CompletionItemKind.Property },
            new() { Label = "method", Kind = CompletionItemKind.Property },
            new() { Label = "headers", Kind = CompletionItemKind.Property },
            new() { Label = "body", Kind = CompletionItemKind.Property },
            new() { Label = "response", Kind = CompletionItemKind.Property }
        };
    }

    private List<CompletionItem> FilterCompletions(List<CompletionItem> completions, string query)
    {
        if (string.IsNullOrEmpty(query))
            return completions;
        
        query = query.ToLowerInvariant();
        
        return completions
            .Where(c => c.Label.ToLowerInvariant().Contains(query) || 
                       (c.Detail?.ToLowerInvariant().Contains(query) ?? false))
            .OrderBy(c => c.Label.ToLowerInvariant().StartsWith(query) ? 0 : 1)
            .ThenBy(c => c.Label)
            .ToList();
    }

    private string GetQueryFromPosition(string text, int position)
    {
        if (position <= 0) return "";
        
        var start = position - 1;
        while (start > 0 && char.IsLetterOrDigit(text[start]))
        {
            start--;
        }
        
        if (start < position - 1)
        {
            return text.Substring(start + 1, position - start - 1);
        }
        
        return "";
    }

    private bool IsInsideString(string text)
    {
        var quoteCount = 0;
        var escaped = false;
        
        foreach (var c in text)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }
            
            if (c == '\\')
            {
                escaped = true;
            }
            else if (c == '"')
            {
                quoteCount++;
            }
        }
        
        return quoteCount % 2 == 1;
    }

    private string ExtractJsonPath(string text)
    {
        // Simplified path extraction
        var path = new List<string>();
        // Implementation would parse the JSON structure to build the path
        return string.Join(".", path);
    }

    private string? GetParentObjectType(string text)
    {
        // Heuristic to determine parent object type
        if (text.Contains("user")) return "user";
        if (text.Contains("api")) return "api";
        return null;
    }

    private CompletionDocumentation? GetDocumentationForItem(string itemId)
    {
        var docs = new Dictionary<string, CompletionDocumentation>
        {
            ["id"] = new() 
            { 
                Summary = "Unique identifier for the object",
                Description = "A unique identifier that distinguishes this object from others. Usually a string or number.",
                Examples = new[] { "\"12345\"", "42" }
            },
            ["name"] = new()
            {
                Summary = "Display name for the object",
                Description = "A human-readable name or title for the object.",
                Examples = new[] { "\"John Doe\"", "\"My Project\"" }
            }
        };
        
        return docs.TryGetValue(itemId, out var doc) ? doc : null;
    }

    private List<DiagnosticItem> ValidateAgainstSchemas(string text)
    {
        // Schema validation implementation
        return new List<DiagnosticItem>();
    }

    private List<DiagnosticItem> LintJson(string text)
    {
        var diagnostics = new List<DiagnosticItem>();
        
        // Check for trailing commas
        var trailingCommaPattern = new Regex(@",\s*[}\]]");
        var matches = trailingCommaPattern.Matches(text);
        
        foreach (Match match in matches)
        {
            var line = GetLineFromPosition(text, match.Index);
            diagnostics.Add(new DiagnosticItem
            {
                Message = "Trailing comma is not allowed in JSON",
                Severity = DiagnosticSeverity.Error,
                StartLine = line,
                StartColumn = GetColumnFromPosition(text, match.Index),
                EndLine = line,
                EndColumn = GetColumnFromPosition(text, match.Index + match.Length)
            });
        }
        
        return diagnostics;
    }

    private int GetLineFromPosition(string text, int position)
    {
        return text.Take(position).Count(c => c == '\n') + 1;
    }

    private int GetColumnFromPosition(string text, int position)
    {
        var lastNewline = text.LastIndexOf('\n', position - 1);
        return position - lastNewline;
    }

    private int GetLineFromBytePosition(string text, int bytePosition)
    {
        // Convert byte position to character position
        var charPosition = Math.Min(bytePosition, text.Length);
        return GetLineFromPosition(text, charPosition);
    }

    public void Dispose()
    {
        _objectRef?.Dispose();
    }
}

public class AutoCompletionOptions
{
    public bool EnableSmartCompletion { get; set; } = true;
    public bool EnableSchemaValidation { get; set; } = true;
    public bool EnableContextualHelp { get; set; } = true;
    public int MaxSuggestions { get; set; } = 50;
    public string[] TriggerCharacters { get; set; } = { "\"", ":", ",", "{", "[" };
    public int CompletionDelay { get; set; } = 300;
}

public class CompletionItem
{
    public string Label { get; set; } = "";
    public CompletionItemKind Kind { get; set; } = CompletionItemKind.Text;
    public string? Detail { get; set; }
    public string? Documentation { get; set; }
    public string? InsertText { get; set; }
    public bool Preselect { get; set; }
    public int Priority { get; set; } = 50;
}

public class CompletionContext
{
    public string TriggerCharacter { get; set; } = "";
    public CompletionTriggerKind TriggerKind { get; set; }
}

public class CompletionDocumentation
{
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string[] Examples { get; set; } = Array.Empty<string>();
}

public class DiagnosticItem
{
    public string Message { get; set; } = "";
    public DiagnosticSeverity Severity { get; set; }
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
}

public class JsonContext
{
    public JsonContextType Type { get; set; }
    public int Position { get; set; }
    public string Path { get; set; } = "";
    public string? ParentObject { get; set; }
}

public class JsonSchema
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public Dictionary<string, object> Properties { get; set; } = new();
}

public enum JsonContextType
{
    Root,
    PropertyKey,
    PropertyValue,
    ArrayElement
}

public enum CompletionItemKind
{
    Text = 1,
    Method = 2,
    Function = 3,
    Constructor = 4,
    Field = 5,
    Variable = 6,
    Class = 7,
    Interface = 8,
    Module = 9,
    Property = 10,
    Unit = 11,
    Value = 12,
    Enum = 13,
    Keyword = 14,
    Snippet = 15,
    Color = 16,
    File = 17,
    Reference = 18
}

public enum CompletionTriggerKind
{
    Invoked = 1,
    TriggerCharacter = 2,
    TriggerForIncompleteCompletions = 3
}

public enum DiagnosticSeverity
{
    Error = 1,
    Warning = 2,
    Information = 3,
    Hint = 4
}