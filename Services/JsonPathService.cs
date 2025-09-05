using Json.Path;
using System.Text.Json;

namespace JsonBlazer.Services;

public class JsonPathService
{
    public JsonPathQueryResult ExecuteQuery(string jsonContent, string jsonPathExpression)
    {
        var result = new JsonPathQueryResult();
        
        try
        {
            // Parse JSON
            var jsonDocument = JsonDocument.Parse(jsonContent);
            var jsonNode = JsonNode.Parse(jsonContent);
            
            if (jsonNode == null)
            {
                result.Success = false;
                result.Error = "Failed to parse JSON content";
                return result;
            }
            
            // Parse JSONPath expression
            if (!JsonPath.TryParse(jsonPathExpression, out var path))
            {
                result.Success = false;
                result.Error = "Invalid JSONPath expression";
                return result;
            }
            
            // Execute query
            var pathResult = path.Evaluate(jsonNode);
            result.Success = true;
            result.MatchCount = pathResult.Matches?.Count ?? 0;
            
            if (pathResult.Matches != null && pathResult.Matches.Any())
            {
                result.Results = pathResult.Matches.Select(match => new JsonPathMatch
                {
                    Path = match.Location.ToString(),
                    Value = match.Value?.ToJsonString() ?? "null",
                    ValueType = GetValueType(match.Value)
                }).ToList();
                
                // Create formatted result
                if (result.Results.Count == 1)
                {
                    result.FormattedResult = result.Results.First().Value;
                }
                else
                {
                    var array = result.Results.Select(r => r.Value).ToArray();
                    result.FormattedResult = JsonSerializer.Serialize(array, new JsonSerializerOptions { WriteIndented = true });
                }
            }
            else
            {
                result.FormattedResult = "[]";
            }
            
        }
        catch (JsonException ex)
        {
            result.Success = false;
            result.Error = $"JSON parsing error: {ex.Message}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Query execution error: {ex.Message}";
        }
        
        return result;
    }
    
    public List<JsonPathSuggestion> GetPathSuggestions(string jsonContent)
    {
        var suggestions = new List<JsonPathSuggestion>();
        
        try
        {
            var jsonNode = JsonNode.Parse(jsonContent);
            if (jsonNode != null)
            {
                CollectPaths(jsonNode, "$", suggestions);
            }
        }
        catch
        {
            // Ignore errors for suggestions
        }
        
        return suggestions;
    }
    
    private void CollectPaths(JsonNode node, string currentPath, List<JsonPathSuggestion> suggestions, int depth = 0)
    {
        if (depth > 10) return; // Prevent infinite recursion
        
        suggestions.Add(new JsonPathSuggestion
        {
            Path = currentPath,
            Description = $"Current node ({GetNodeType(node)})",
            Type = GetNodeType(node)
        });
        
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj)
                {
                    var newPath = $"{currentPath}.{property.Key}";
                    suggestions.Add(new JsonPathSuggestion
                    {
                        Path = newPath,
                        Description = $"Property: {property.Key} ({GetNodeType(property.Value)})",
                        Type = GetNodeType(property.Value)
                    });
                    
                    if (property.Value != null)
                    {
                        CollectPaths(property.Value, newPath, suggestions, depth + 1);
                    }
                }
                break;
                
            case JsonArray array:
                for (int i = 0; i < array.Count && i < 3; i++) // Limit to first 3 for performance
                {
                    var newPath = $"{currentPath}[{i}]";
                    var item = array[i];
                    if (item != null)
                    {
                        suggestions.Add(new JsonPathSuggestion
                        {
                            Path = newPath,
                            Description = $"Array item [{i}] ({GetNodeType(item)})",
                            Type = GetNodeType(item)
                        });
                        CollectPaths(item, newPath, suggestions, depth + 1);
                    }
                }
                
                // Add wildcard suggestions
                suggestions.Add(new JsonPathSuggestion
                {
                    Path = $"{currentPath}[*]",
                    Description = "All array items",
                    Type = "wildcard"
                });
                break;
        }
    }
    
    private string GetNodeType(JsonNode? node)
    {
        return node switch
        {
            JsonObject => "object",
            JsonArray => "array",
            JsonValue value => GetValueType(value),
            null => "null",
            _ => "unknown"
        };
    }
    
    private string GetValueType(JsonNode? node)
    {
        if (node == null) return "null";
        
        if (node is JsonValue value)
        {
            if (value.TryGetValue<bool>(out _)) return "boolean";
            if (value.TryGetValue<int>(out _)) return "integer";
            if (value.TryGetValue<double>(out _)) return "number";
            if (value.TryGetValue<string>(out _)) return "string";
        }
        
        return "unknown";
    }
    
    public List<string> GetCommonExpressions()
    {
        return new List<string>
        {
            "$",                          // Root
            "$.*",                        // All properties
            "$..*",                       // Recursive descent
            "$..name",                    // All 'name' properties
            "$[0]",                       // First item
            "$[-1]",                      // Last item
            "$[0:2]",                     // Slice (first 2)
            "$[?@.price < 10]",          // Filter by price
            "$.store.book[*].author",    // Authors of all books
            "$.store.book[?@.price < 10].title", // Cheap book titles
            "$..book[?@.isbn]",          // Books with ISBN
            "$..book[?@.category == 'fiction']", // Fiction books
            "$..*[?@.name]",             // All nodes with 'name'
            "$[?@.length() > 0]"         // Non-empty arrays
        };
    }
}

public class JsonPathQueryResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int MatchCount { get; set; }
    public string FormattedResult { get; set; } = "";
    public List<JsonPathMatch> Results { get; set; } = new();
}

public class JsonPathMatch
{
    public string Path { get; set; } = "";
    public string Value { get; set; } = "";
    public string ValueType { get; set; } = "";
}

public class JsonPathSuggestion
{
    public string Path { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "";
}