using System.Diagnostics;
using System.Text.Json;

namespace JsonBlazer.Services;

public class PerformanceService
{
    public async Task<PerformanceTestResult> RunPerformanceTestAsync(string jsonContent, PerformanceTestOptions options)
    {
        var result = new PerformanceTestResult
        {
            JsonSize = jsonContent.Length,
            TestOptions = options,
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            // Parse test
            if (options.TestParsing)
            {
                result.ParseResults = await TestJsonParsingAsync(jsonContent, options.Iterations);
            }
            
            // Serialization test
            if (options.TestSerialization)
            {
                result.SerializationResults = await TestJsonSerializationAsync(jsonContent, options.Iterations);
            }
            
            // Validation test
            if (options.TestValidation)
            {
                result.ValidationResults = await TestJsonValidationAsync(jsonContent, options.Iterations);
            }
            
            // Memory analysis
            if (options.TestMemory)
            {
                result.MemoryResults = await TestMemoryUsageAsync(jsonContent, options.Iterations);
            }
            
            // Structure analysis
            result.StructureAnalysis = AnalyzeJsonStructure(jsonContent);
            
            result.EndTime = DateTime.UtcNow;
            result.TotalTestTime = (result.EndTime - result.StartTime).TotalMilliseconds;
            result.Success = true;
            
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Performance test failed: {ex.Message}";
            result.EndTime = DateTime.UtcNow;
        }
        
        return result;
    }
    
    private async Task<PerformanceMetrics> TestJsonParsingAsync(string jsonContent, int iterations)
    {
        var results = new List<double>();
        var sw = new Stopwatch();
        
        // Warmup
        for (int i = 0; i < Math.Min(10, iterations); i++)
        {
            JsonDocument.Parse(jsonContent).Dispose();
        }
        
        // Actual test
        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            var doc = JsonDocument.Parse(jsonContent);
            sw.Stop();
            doc.Dispose();
            
            results.Add(sw.Elapsed.TotalMilliseconds);
            
            // Yield control periodically
            if (i % 100 == 0)
                await Task.Yield();
        }
        
        return CalculateMetrics(results, "JSON Parsing");
    }
    
    private async Task<PerformanceMetrics> TestJsonSerializationAsync(string jsonContent, int iterations)
    {
        var results = new List<double>();
        var sw = new Stopwatch();
        
        // Parse once for serialization tests
        var jsonDocument = JsonDocument.Parse(jsonContent);
        var jsonElement = jsonDocument.RootElement;
        
        try
        {
            // Warmup
            for (int i = 0; i < Math.Min(10, iterations); i++)
            {
                JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = false });
            }
            
            // Actual test
            for (int i = 0; i < iterations; i++)
            {
                sw.Restart();
                var serialized = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
                sw.Stop();
                
                results.Add(sw.Elapsed.TotalMilliseconds);
                
                // Yield control periodically
                if (i % 100 == 0)
                    await Task.Yield();
            }
        }
        finally
        {
            jsonDocument.Dispose();
        }
        
        return CalculateMetrics(results, "JSON Serialization");
    }
    
    private async Task<PerformanceMetrics> TestJsonValidationAsync(string jsonContent, int iterations)
    {
        var results = new List<double>();
        var sw = new Stopwatch();
        
        // Warmup
        for (int i = 0; i < Math.Min(10, iterations); i++)
        {
            ValidateJson(jsonContent);
        }
        
        // Actual test
        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            var isValid = ValidateJson(jsonContent);
            sw.Stop();
            
            results.Add(sw.Elapsed.TotalMilliseconds);
            
            // Yield control periodically
            if (i % 100 == 0)
                await Task.Yield();
        }
        
        return CalculateMetrics(results, "JSON Validation");
    }
    
    private async Task<MemoryMetrics> TestMemoryUsageAsync(string jsonContent, int iterations)
    {
        var initialMemory = GC.GetTotalMemory(true);
        var memoryReadings = new List<long>();
        
        for (int i = 0; i < Math.Min(iterations, 100); i++) // Limit memory tests
        {
            var beforeMemory = GC.GetTotalMemory(false);
            
            var doc = JsonDocument.Parse(jsonContent);
            var afterMemory = GC.GetTotalMemory(false);
            
            memoryReadings.Add(afterMemory - beforeMemory);
            
            doc.Dispose();
            
            if (i % 10 == 0)
            {
                GC.Collect();
                await Task.Yield();
            }
        }
        
        var finalMemory = GC.GetTotalMemory(true);
        
        return new MemoryMetrics
        {
            AverageMemoryPerOperation = memoryReadings.Average(),
            MinMemoryUsage = memoryReadings.Min(),
            MaxMemoryUsage = memoryReadings.Max(),
            InitialMemory = initialMemory,
            FinalMemory = finalMemory,
            MemoryDifference = finalMemory - initialMemory
        };
    }
    
    private PerformanceJsonStructureAnalysis AnalyzeJsonStructure(string jsonContent)
    {
        var analysis = new PerformanceJsonStructureAnalysis();
        
        try
        {
            var document = JsonDocument.Parse(jsonContent);
            AnalyzeElement(document.RootElement, analysis, 0);
            document.Dispose();
            
            analysis.EstimatedComplexity = CalculateComplexity(analysis);
        }
        catch
        {
            // If parsing fails, still return basic info
        }
        
        return analysis;
    }
    
    private void AnalyzeElement(JsonElement element, PerformanceJsonStructureAnalysis analysis, int depth)
    {
        analysis.MaxDepth = Math.Max(analysis.MaxDepth, depth);
        
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                analysis.ObjectCount++;
                analysis.TotalProperties += element.GetArrayLength();
                foreach (var property in element.EnumerateObject())
                {
                    AnalyzeElement(property.Value, analysis, depth + 1);
                }
                break;
                
            case JsonValueKind.Array:
                analysis.ArrayCount++;
                var arrayLength = element.GetArrayLength();
                analysis.TotalArrayItems += arrayLength;
                analysis.LargestArraySize = Math.Max(analysis.LargestArraySize, arrayLength);
                
                foreach (var item in element.EnumerateArray())
                {
                    AnalyzeElement(item, analysis, depth + 1);
                }
                break;
                
            case JsonValueKind.String:
                analysis.StringCount++;
                var stringLength = element.GetString()?.Length ?? 0;
                analysis.LongestStringLength = Math.Max(analysis.LongestStringLength, stringLength);
                break;
                
            case JsonValueKind.Number:
                analysis.NumberCount++;
                break;
                
            case JsonValueKind.True:
            case JsonValueKind.False:
                analysis.BooleanCount++;
                break;
                
            case JsonValueKind.Null:
                analysis.NullCount++;
                break;
        }
    }
    
    private string CalculateComplexity(PerformanceJsonStructureAnalysis analysis)
    {
        var complexity = analysis.ObjectCount + analysis.ArrayCount + (analysis.MaxDepth * 2);
        
        return complexity switch
        {
            < 10 => "Very Simple",
            < 50 => "Simple",
            < 200 => "Moderate",
            < 500 => "Complex",
            _ => "Very Complex"
        };
    }
    
    private bool ValidateJson(string jsonContent)
    {
        try
        {
            JsonDocument.Parse(jsonContent).Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private PerformanceMetrics CalculateMetrics(List<double> results, string operationName)
    {
        if (!results.Any())
        {
            return new PerformanceMetrics { OperationName = operationName };
        }
        
        results.Sort();
        
        return new PerformanceMetrics
        {
            OperationName = operationName,
            AverageTime = results.Average(),
            MinTime = results.Min(),
            MaxTime = results.Max(),
            MedianTime = results[results.Count / 2],
            Percentile95 = results[(int)(results.Count * 0.95)],
            Percentile99 = results[(int)(results.Count * 0.99)],
            StandardDeviation = CalculateStandardDeviation(results),
            OperationsPerSecond = 1000.0 / results.Average(),
            TotalOperations = results.Count
        };
    }
    
    private double CalculateStandardDeviation(List<double> values)
    {
        var mean = values.Average();
        var squaredDifferences = values.Select(x => Math.Pow(x - mean, 2));
        return Math.Sqrt(squaredDifferences.Average());
    }
}

public class PerformanceTestResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double TotalTestTime { get; set; }
    public int JsonSize { get; set; }
    public PerformanceTestOptions TestOptions { get; set; } = new();
    
    public PerformanceMetrics? ParseResults { get; set; }
    public PerformanceMetrics? SerializationResults { get; set; }
    public PerformanceMetrics? ValidationResults { get; set; }
    public MemoryMetrics? MemoryResults { get; set; }
    public PerformanceJsonStructureAnalysis StructureAnalysis { get; set; } = new();
}

public class PerformanceTestOptions
{
    public bool TestParsing { get; set; } = true;
    public bool TestSerialization { get; set; } = true;
    public bool TestValidation { get; set; } = true;
    public bool TestMemory { get; set; } = true;
    public int Iterations { get; set; } = 100;
}

public class PerformanceMetrics
{
    public string OperationName { get; set; } = "";
    public double AverageTime { get; set; }
    public double MinTime { get; set; }
    public double MaxTime { get; set; }
    public double MedianTime { get; set; }
    public double Percentile95 { get; set; }
    public double Percentile99 { get; set; }
    public double StandardDeviation { get; set; }
    public double OperationsPerSecond { get; set; }
    public int TotalOperations { get; set; }
}

public class MemoryMetrics
{
    public double AverageMemoryPerOperation { get; set; }
    public long MinMemoryUsage { get; set; }
    public long MaxMemoryUsage { get; set; }
    public long InitialMemory { get; set; }
    public long FinalMemory { get; set; }
    public long MemoryDifference { get; set; }
}

public class PerformanceJsonStructureAnalysis
{
    public int ObjectCount { get; set; }
    public int ArrayCount { get; set; }
    public int StringCount { get; set; }
    public int NumberCount { get; set; }
    public int BooleanCount { get; set; }
    public int NullCount { get; set; }
    public int MaxDepth { get; set; }
    public int TotalProperties { get; set; }
    public int TotalArrayItems { get; set; }
    public int LargestArraySize { get; set; }
    public int LongestStringLength { get; set; }
    public string EstimatedComplexity { get; set; } = "";
}