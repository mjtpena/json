using System.Text.Json;

namespace JsonBlazer.Services;

public class JsonDiffService
{
    public JsonDiffResult CompareJson(string leftJson, string rightJson)
    {
        var result = new JsonDiffResult();
        
        try
        {
            var leftDoc = JsonDocument.Parse(leftJson);
            var rightDoc = JsonDocument.Parse(rightJson);
            
            result.LeftJson = leftJson;
            result.RightJson = rightJson;
            result.Success = true;
            
            CompareElements(leftDoc.RootElement, rightDoc.RootElement, "$", result);
            
            // Generate summary
            GenerateSummary(result);
            
        }
        catch (JsonException ex)
        {
            result.Success = false;
            result.Error = $"Invalid JSON: {ex.Message}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Comparison failed: {ex.Message}";
        }
        
        return result;
    }
    
    private void CompareElements(JsonElement left, JsonElement right, string path, JsonDiffResult result)
    {
        if (left.ValueKind != right.ValueKind)
        {
            result.Differences.Add(new JsonDifference
            {
                Path = path,
                Type = DifferenceType.TypeChanged,
                LeftValue = GetElementString(left),
                RightValue = GetElementString(right),
                LeftType = left.ValueKind.ToString(),
                RightType = right.ValueKind.ToString()
            });
            return;
        }
        
        switch (left.ValueKind)
        {
            case JsonValueKind.Object:
                CompareObjects(left, right, path, result);
                break;
                
            case JsonValueKind.Array:
                CompareArrays(left, right, path, result);
                break;
                
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                if (!JsonElementEquals(left, right))
                {
                    result.Differences.Add(new JsonDifference
                    {
                        Path = path,
                        Type = DifferenceType.ValueChanged,
                        LeftValue = GetElementString(left),
                        RightValue = GetElementString(right),
                        LeftType = left.ValueKind.ToString(),
                        RightType = right.ValueKind.ToString()
                    });
                }
                break;
        }
    }
    
    private void CompareObjects(JsonElement left, JsonElement right, string path, JsonDiffResult result)
    {
        var leftProps = left.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        var rightProps = right.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        
        // Find added properties
        foreach (var rightProp in rightProps.Where(p => !leftProps.ContainsKey(p.Key)))
        {
            result.Differences.Add(new JsonDifference
            {
                Path = $"{path}.{rightProp.Key}",
                Type = DifferenceType.PropertyAdded,
                RightValue = GetElementString(rightProp.Value),
                RightType = rightProp.Value.ValueKind.ToString()
            });
        }
        
        // Find removed properties
        foreach (var leftProp in leftProps.Where(p => !rightProps.ContainsKey(p.Key)))
        {
            result.Differences.Add(new JsonDifference
            {
                Path = $"{path}.{leftProp.Key}",
                Type = DifferenceType.PropertyRemoved,
                LeftValue = GetElementString(leftProp.Value),
                LeftType = leftProp.Value.ValueKind.ToString()
            });
        }
        
        // Compare common properties
        foreach (var leftProp in leftProps.Where(p => rightProps.ContainsKey(p.Key)))
        {
            CompareElements(leftProp.Value, rightProps[leftProp.Key], $"{path}.{leftProp.Key}", result);
        }
    }
    
    private void CompareArrays(JsonElement left, JsonElement right, string path, JsonDiffResult result)
    {
        var leftArray = left.EnumerateArray().ToArray();
        var rightArray = right.EnumerateArray().ToArray();
        
        int maxLength = Math.Max(leftArray.Length, rightArray.Length);
        
        for (int i = 0; i < maxLength; i++)
        {
            if (i >= leftArray.Length)
            {
                result.Differences.Add(new JsonDifference
                {
                    Path = $"{path}[{i}]",
                    Type = DifferenceType.ArrayItemAdded,
                    RightValue = GetElementString(rightArray[i]),
                    RightType = rightArray[i].ValueKind.ToString()
                });
            }
            else if (i >= rightArray.Length)
            {
                result.Differences.Add(new JsonDifference
                {
                    Path = $"{path}[{i}]",
                    Type = DifferenceType.ArrayItemRemoved,
                    LeftValue = GetElementString(leftArray[i]),
                    LeftType = leftArray[i].ValueKind.ToString()
                });
            }
            else
            {
                CompareElements(leftArray[i], rightArray[i], $"{path}[{i}]", result);
            }
        }
    }
    
    private bool JsonElementEquals(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind) return false;
        
        return left.ValueKind switch
        {
            JsonValueKind.String => left.GetString() == right.GetString(),
            JsonValueKind.Number => left.GetRawText() == right.GetRawText(),
            JsonValueKind.True or JsonValueKind.False => left.GetBoolean() == right.GetBoolean(),
            JsonValueKind.Null => true,
            _ => false
        };
    }
    
    private string GetElementString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => $"\"{element.GetString()}\"",
            JsonValueKind.Null => "null",
            _ => element.GetRawText()
        };
    }
    
    private void GenerateSummary(JsonDiffResult result)
    {
        result.Summary = new JsonDiffSummary
        {
            TotalDifferences = result.Differences.Count,
            PropertiesAdded = result.Differences.Count(d => d.Type == DifferenceType.PropertyAdded),
            PropertiesRemoved = result.Differences.Count(d => d.Type == DifferenceType.PropertyRemoved),
            ValuesChanged = result.Differences.Count(d => d.Type == DifferenceType.ValueChanged),
            TypesChanged = result.Differences.Count(d => d.Type == DifferenceType.TypeChanged),
            ArrayItemsAdded = result.Differences.Count(d => d.Type == DifferenceType.ArrayItemAdded),
            ArrayItemsRemoved = result.Differences.Count(d => d.Type == DifferenceType.ArrayItemRemoved)
        };
    }
    
    public JsonMergeResult MergeJson(string baseJson, string leftJson, string rightJson)
    {
        var result = new JsonMergeResult();
        
        try
        {
            var baseDoc = JsonDocument.Parse(baseJson);
            var leftDoc = JsonDocument.Parse(leftJson);
            var rightDoc = JsonDocument.Parse(rightJson);
            
            // For now, implement a simple merge strategy
            var mergedNode = MergeElements(baseDoc.RootElement, leftDoc.RootElement, rightDoc.RootElement);
            
            result.MergedJson = JsonSerializer.Serialize(mergedNode, new JsonSerializerOptions { WriteIndented = true });
            result.Success = true;
            
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Merge failed: {ex.Message}";
        }
        
        return result;
    }
    
    private object? MergeElements(JsonElement baseElement, JsonElement leftElement, JsonElement rightElement)
    {
        // Simple merge strategy: right wins in conflicts
        if (rightElement.ValueKind != JsonValueKind.Undefined)
        {
            return ConvertJsonElement(rightElement);
        }
        
        if (leftElement.ValueKind != JsonValueKind.Undefined)
        {
            return ConvertJsonElement(leftElement);
        }
        
        return ConvertJsonElement(baseElement);
    }
    
    private object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }
}

public class JsonDiffResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string LeftJson { get; set; } = "";
    public string RightJson { get; set; } = "";
    public List<JsonDifference> Differences { get; set; } = new();
    public JsonDiffSummary Summary { get; set; } = new();
}

public class JsonDifference
{
    public string Path { get; set; } = "";
    public DifferenceType Type { get; set; }
    public string? LeftValue { get; set; }
    public string? RightValue { get; set; }
    public string? LeftType { get; set; }
    public string? RightType { get; set; }
}

public class JsonDiffSummary
{
    public int TotalDifferences { get; set; }
    public int PropertiesAdded { get; set; }
    public int PropertiesRemoved { get; set; }
    public int ValuesChanged { get; set; }
    public int TypesChanged { get; set; }
    public int ArrayItemsAdded { get; set; }
    public int ArrayItemsRemoved { get; set; }
}

public class JsonMergeResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string MergedJson { get; set; } = "";
    public List<string> ConflictPaths { get; set; } = new();
}

public enum DifferenceType
{
    PropertyAdded,
    PropertyRemoved,
    ValueChanged,
    TypeChanged,
    ArrayItemAdded,
    ArrayItemRemoved
}