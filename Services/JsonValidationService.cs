using NJsonSchema;
using NJsonSchema.Validation;
using System.Text.Json;

namespace JsonBlazer.Services;

public class JsonValidationService
{
    public async Task<ValidationResult> ValidateJsonAsync(string jsonContent, string? schemaContent = null)
    {
        var result = new ValidationResult();
        
        try
        {
            // First, validate that JSON is well-formed
            JsonDocument.Parse(jsonContent);
            result.IsValidJson = true;
        }
        catch (JsonException ex)
        {
            result.IsValidJson = false;
            result.Errors.Add($"Invalid JSON: {ex.Message}");
            return result;
        }
        
        // If schema is provided, validate against it
        if (!string.IsNullOrEmpty(schemaContent))
        {
            try
            {
                var schema = await JsonSchema.FromJsonAsync(schemaContent);
                var errors = schema.Validate(jsonContent);
                
                result.IsValidSchema = !errors.Any();
                result.SchemaErrors.AddRange(errors.Select(e => e.ToString()));
                
                if (result.IsValidSchema)
                {
                    result.Messages.Add("JSON is valid according to the provided schema.");
                }
            }
            catch (Exception ex)
            {
                result.IsValidSchema = false;
                result.Errors.Add($"Schema validation error: {ex.Message}");
            }
        }
        
        // Add general JSON analysis
        result.Messages.AddRange(AnalyzeJson(jsonContent));
        
        return result;
    }
    
    public async Task<string> GenerateSchemaFromJsonAsync(string jsonContent)
    {
        try
        {
            var schema = JsonSchema.FromSampleJson(jsonContent);
            return schema.ToJson();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate schema: {ex.Message}", ex);
        }
    }
    
    public ValidationResult ValidateJsonStructure(string jsonContent)
    {
        var result = new ValidationResult();
        
        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            result.IsValidJson = true;
            
            var stats = AnalyzeJsonStructure(document.RootElement);
            result.Messages.AddRange(stats);
            
            // Check for common issues
            CheckCommonIssues(document.RootElement, result);
        }
        catch (JsonException ex)
        {
            result.IsValidJson = false;
            result.Errors.Add($"JSON parsing error: {ex.Message}");
        }
        
        return result;
    }
    
    private List<string> AnalyzeJson(string jsonContent)
    {
        var messages = new List<string>();
        
        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var stats = AnalyzeJsonStructure(document.RootElement);
            messages.AddRange(stats);
        }
        catch
        {
            // Already handled in main validation
        }
        
        return messages;
    }
    
    private List<string> AnalyzeJsonStructure(JsonElement element)
    {
        var messages = new List<string>();
        var stats = new JsonStats();
        
        CountElements(element, stats);
        
        messages.Add($"Total objects: {stats.ObjectCount}");
        messages.Add($"Total arrays: {stats.ArrayCount}");
        messages.Add($"Total properties: {stats.PropertyCount}");
        messages.Add($"Total values: {stats.ValueCount}");
        messages.Add($"Maximum depth: {GetMaxDepth(element)}");
        
        return messages;
    }
    
    private void CountElements(JsonElement element, JsonStats stats)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                stats.ObjectCount++;
                foreach (var property in element.EnumerateObject())
                {
                    stats.PropertyCount++;
                    CountElements(property.Value, stats);
                }
                break;
                
            case JsonValueKind.Array:
                stats.ArrayCount++;
                foreach (var item in element.EnumerateArray())
                {
                    CountElements(item, stats);
                }
                break;
                
            default:
                stats.ValueCount++;
                break;
        }
    }
    
    private int GetMaxDepth(JsonElement element)
    {
        return GetDepth(element, 0);
    }
    
    private int GetDepth(JsonElement element, int currentDepth)
    {
        int maxDepth = currentDepth;
        
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    maxDepth = Math.Max(maxDepth, GetDepth(property.Value, currentDepth + 1));
                }
                break;
                
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    maxDepth = Math.Max(maxDepth, GetDepth(item, currentDepth + 1));
                }
                break;
        }
        
        return maxDepth;
    }
    
    private void CheckCommonIssues(JsonElement element, ValidationResult result)
    {
        // Check for very deep nesting (potential performance issue)
        var maxDepth = GetMaxDepth(element);
        if (maxDepth > 20)
        {
            result.Warnings.Add($"JSON has very deep nesting (depth: {maxDepth}). This may cause performance issues.");
        }
        
        // Check for very large arrays
        CheckLargeArrays(element, result, "");
        
        // Check for duplicate keys (not directly detectable in System.Text.Json, but we can check for suspicious patterns)
        CheckSuspiciousPatterns(element, result);
    }
    
    private void CheckLargeArrays(JsonElement element, ValidationResult result, string path)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var arrayLength = element.GetArrayLength();
            if (arrayLength > 1000)
            {
                result.Warnings.Add($"Large array detected at {(string.IsNullOrEmpty(path) ? "root" : path)} with {arrayLength} items.");
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var newPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                CheckLargeArrays(property.Value, result, newPath);
            }
        }
    }
    
    private void CheckSuspiciousPatterns(JsonElement element, ValidationResult result)
    {
        // This is a basic check - in practice you might want more sophisticated analysis
        if (element.ValueKind == JsonValueKind.Object)
        {
            var properties = element.EnumerateObject().ToList();
            var propertyNames = properties.Select(p => p.Name).ToList();
            
            // Check for very similar property names (potential typos)
            for (int i = 0; i < propertyNames.Count; i++)
            {
                for (int j = i + 1; j < propertyNames.Count; j++)
                {
                    if (AreSimilar(propertyNames[i], propertyNames[j]))
                    {
                        result.Warnings.Add($"Similar property names detected: '{propertyNames[i]}' and '{propertyNames[j]}' - possible typo?");
                    }
                }
            }
        }
    }
    
    private bool AreSimilar(string str1, string str2)
    {
        if (str1.Length != str2.Length) return false;
        
        int differences = 0;
        for (int i = 0; i < str1.Length; i++)
        {
            if (str1[i] != str2[i])
            {
                differences++;
                if (differences > 1) return false;
            }
        }
        
        return differences == 1;
    }
    
    private class JsonStats
    {
        public int ObjectCount { get; set; }
        public int ArrayCount { get; set; }
        public int PropertyCount { get; set; }
        public int ValueCount { get; set; }
    }
}

public class ValidationResult
{
    public bool IsValidJson { get; set; }
    public bool IsValidSchema { get; set; } = true; // Default to true when no schema provided
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Messages { get; set; } = new();
    public List<string> SchemaErrors { get; set; } = new();
    
    public bool IsValid => IsValidJson && IsValidSchema && !Errors.Any();
    public bool HasWarnings => Warnings.Any();
    public bool HasMessages => Messages.Any() || SchemaErrors.Any();
}