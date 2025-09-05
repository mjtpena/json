using Microsoft.JSInterop;
using System.Text.Json;

namespace JsonBlazer.Services;

public class DeltaLakeService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<DeltaLakeService> _logger;

    public DeltaLakeService(IJSRuntime jsRuntime, ILogger<DeltaLakeService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("initializeDeltaLake");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Delta Lake");
            return false;
        }
    }

    public async Task<DeltaTableMetadata> GetTableMetadataAsync(string tablePath)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("getDeltaTableMetadata", tablePath);
            return JsonSerializer.Deserialize<DeltaTableMetadata>(result) ?? new DeltaTableMetadata();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Delta table metadata for {TablePath}", tablePath);
            throw;
        }
    }

    public async Task<List<DeltaTransaction>> GetTransactionLogAsync(string tablePath, int? maxVersions = null)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("getDeltaTransactionLog", tablePath, maxVersions);
            return JsonSerializer.Deserialize<List<DeltaTransaction>>(result) ?? new List<DeltaTransaction>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Delta transaction log for {TablePath}", tablePath);
            throw;
        }
    }

    public async Task<DeltaTableSnapshot> GetTableSnapshotAsync(string tablePath, long? version = null, DateTime? timestamp = null)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("getDeltaTableSnapshot", tablePath, version, timestamp?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            return JsonSerializer.Deserialize<DeltaTableSnapshot>(result) ?? new DeltaTableSnapshot();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Delta table snapshot for {TablePath}", tablePath);
            throw;
        }
    }

    public async Task<List<DeltaFileAction>> GetFileActionsAsync(string tablePath, long version)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("getDeltaFileActions", tablePath, version);
            return JsonSerializer.Deserialize<List<DeltaFileAction>>(result) ?? new List<DeltaFileAction>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Delta file actions for {TablePath} version {Version}", tablePath, version);
            throw;
        }
    }

    public async Task<DeltaVersionComparison> CompareVersionsAsync(string tablePath, long fromVersion, long toVersion)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("compareDeltaVersions", tablePath, fromVersion, toVersion);
            return JsonSerializer.Deserialize<DeltaVersionComparison>(result) ?? new DeltaVersionComparison();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare Delta versions for {TablePath}", tablePath);
            throw;
        }
    }

    public async Task<DeltaOptimizationAnalysis> AnalyzeOptimizationOpportunitiesAsync(string tablePath)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("analyzeDeltaOptimization", tablePath);
            return JsonSerializer.Deserialize<DeltaOptimizationAnalysis>(result) ?? new DeltaOptimizationAnalysis();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze Delta optimization for {TablePath}", tablePath);
            throw;
        }
    }

    public async Task<DeltaVacuumAnalysis> SimulateVacuumAsync(string tablePath, TimeSpan retentionPeriod)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("simulateDeltaVacuum", tablePath, retentionPeriod.TotalHours);
            return JsonSerializer.Deserialize<DeltaVacuumAnalysis>(result) ?? new DeltaVacuumAnalysis();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to simulate Delta vacuum for {TablePath}", tablePath);
            throw;
        }
    }

    public async Task<string> QueryTableAsync(string tablePath, string sqlQuery, long? version = null, DateTime? timestamp = null)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("queryDeltaTable", tablePath, sqlQuery, version, timestamp?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query Delta table {TablePath}", tablePath);
            throw;
        }
    }
}

public class DeltaTableMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DeltaSchema Schema { get; set; } = new();
    public List<string> PartitionColumns { get; set; } = new();
    public Dictionary<string, string> Configuration { get; set; } = new();
    public long Version { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime LastModified { get; set; }
    public int MinReaderVersion { get; set; }
    public int MinWriterVersion { get; set; }
    public List<string> ReaderFeatures { get; set; } = new();
    public List<string> WriterFeatures { get; set; } = new();
}

public class DeltaSchema
{
    public List<DeltaColumn> Columns { get; set; } = new();
    public string Type { get; set; } = "struct";
}

public class DeltaColumn
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Nullable { get; set; } = true;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<DeltaColumn> Children { get; set; } = new();
}

public class DeltaTransaction
{
    public long Version { get; set; }
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string Operation { get; set; } = string.Empty;
    public Dictionary<string, object> OperationParameters { get; set; } = new();
    public string? Job { get; set; }
    public string? Notebook { get; set; }
    public string? ClusterId { get; set; }
    public long ReadVersion { get; set; }
    public string? IsolationLevel { get; set; }
    public bool IsBlindAppend { get; set; }
    public List<DeltaAction> Actions { get; set; } = new();
    public string? Engine { get; set; }
    public string? EngineVersion { get; set; }
}

public class DeltaAction
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
}

public class DeltaTableSnapshot
{
    public long Version { get; set; }
    public DateTime Timestamp { get; set; }
    public List<DeltaDataFile> Files { get; set; } = new();
    public DeltaSchema Schema { get; set; } = new();
    public List<string> PartitionColumns { get; set; } = new();
    public long TotalRows { get; set; }
    public long TotalSize { get; set; }
    public int FileCount { get; set; }
}

public class DeltaDataFile
{
    public string Path { get; set; } = string.Empty;
    public Dictionary<string, string> PartitionValues { get; set; } = new();
    public long Size { get; set; }
    public long ModificationTime { get; set; }
    public bool DataChange { get; set; }
    public Dictionary<string, object> Statistics { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class DeltaFileAction
{
    public string Action { get; set; } = string.Empty; // ADD, REMOVE
    public string Path { get; set; } = string.Empty;
    public Dictionary<string, string> PartitionValues { get; set; } = new();
    public long Size { get; set; }
    public long ModificationTime { get; set; }
    public bool DataChange { get; set; }
    public string? DeletionVector { get; set; }
    public Dictionary<string, object> Statistics { get; set; } = new();
}

public class DeltaVersionComparison
{
    public long FromVersion { get; set; }
    public long ToVersion { get; set; }
    public List<DeltaFileChange> FileChanges { get; set; } = new();
    public DeltaSchemaChange? SchemaChange { get; set; }
    public Dictionary<string, object> StatisticsChanges { get; set; } = new();
    public long RowsAdded { get; set; }
    public long RowsRemoved { get; set; }
    public long FilesAdded { get; set; }
    public long FilesRemoved { get; set; }
    public long SizeChange { get; set; }
}

public class DeltaFileChange
{
    public string Type { get; set; } = string.Empty; // ADDED, REMOVED, MODIFIED
    public string Path { get; set; } = string.Empty;
    public long SizeDiff { get; set; }
    public Dictionary<string, string> PartitionValues { get; set; } = new();
}

public class DeltaSchemaChange
{
    public string Type { get; set; } = string.Empty; // COLUMN_ADDED, COLUMN_REMOVED, COLUMN_MODIFIED
    public List<DeltaColumnChange> Changes { get; set; } = new();
}

public class DeltaColumnChange
{
    public string Type { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string? OldType { get; set; }
    public string? NewType { get; set; }
    public bool IsBreaking { get; set; }
}

public class DeltaOptimizationAnalysis
{
    public long SmallFilesCount { get; set; }
    public long SmallFilesThreshold { get; set; } = 128 * 1024 * 1024; // 128MB
    public double CompressionRatio { get; set; }
    public List<DeltaOptimizationRecommendation> Recommendations { get; set; } = new();
    public DeltaZOrderAnalysis? ZOrderAnalysis { get; set; }
    public long EstimatedSavings { get; set; }
    public TimeSpan EstimatedOptimizationTime { get; set; }
}

public class DeltaOptimizationRecommendation
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // HIGH, MEDIUM, LOW
    public long EstimatedBenefit { get; set; }
    public string Command { get; set; } = string.Empty;
}

public class DeltaZOrderAnalysis
{
    public List<string> RecommendedColumns { get; set; } = new();
    public Dictionary<string, double> ColumnSelectivity { get; set; } = new();
    public long EstimatedImprovement { get; set; }
}

public class DeltaVacuumAnalysis
{
    public List<string> FilesToRemove { get; set; } = new();
    public long TotalSizeToRemove { get; set; }
    public int FileCountToRemove { get; set; }
    public TimeSpan RetentionPeriod { get; set; }
    public DateTime CutoffTimestamp { get; set; }
    public List<DeltaVacuumRisk> Risks { get; set; } = new();
}

public class DeltaVacuumRisk
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public List<string> AffectedFiles { get; set; } = new();
}