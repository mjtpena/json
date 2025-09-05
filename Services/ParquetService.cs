using Microsoft.JSInterop;
using System.Text.Json;

namespace JsonBlazer.Services;

public class ParquetService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ParquetService> _logger;

    public ParquetService(IJSRuntime jsRuntime, ILogger<ParquetService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("initializeParquetWasm");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Parquet-WASM");
            return false;
        }
    }

    public async Task<ParquetFileMetadata> ReadFileMetadataAsync(string fileName, byte[] fileData)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("readParquetMetadata", fileName, fileData);
            return JsonSerializer.Deserialize<ParquetFileMetadata>(result) ?? new ParquetFileMetadata();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read Parquet file metadata for {FileName}", fileName);
            throw;
        }
    }

    public async Task<ParquetDataPreview> ReadDataPreviewAsync(string fileName, byte[] fileData, int maxRows = 100)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("readParquetPreview", fileName, fileData, maxRows);
            return JsonSerializer.Deserialize<ParquetDataPreview>(result) ?? new ParquetDataPreview();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read Parquet data preview for {FileName}", fileName);
            throw;
        }
    }

    public async Task<ParquetSchema> GetSchemaAsync(string fileName, byte[] fileData)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("getParquetSchema", fileName, fileData);
            return JsonSerializer.Deserialize<ParquetSchema>(result) ?? new ParquetSchema();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Parquet schema for {FileName}", fileName);
            throw;
        }
    }

    public async Task<ParquetStatistics> GetStatisticsAsync(string fileName, byte[] fileData)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("getParquetStatistics", fileName, fileData);
            return JsonSerializer.Deserialize<ParquetStatistics>(result) ?? new ParquetStatistics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Parquet statistics for {FileName}", fileName);
            throw;
        }
    }

    public async Task<string> QueryDataAsync(string fileName, byte[] fileData, string sqlQuery)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("queryParquetData", fileName, fileData, sqlQuery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query Parquet data for {FileName}", fileName);
            throw;
        }
    }

    public async Task<byte[]> ConvertToJsonAsync(string fileName, byte[] fileData)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<byte[]>("convertParquetToJson", fileName, fileData);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert Parquet to JSON for {FileName}", fileName);
            throw;
        }
    }

    public async Task<byte[]> ConvertToCsvAsync(string fileName, byte[] fileData, ParquetConversionOptions? options = null)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<byte[]>("convertParquetToCsv", fileName, fileData, options ?? new());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert Parquet to CSV for {FileName}", fileName);
            throw;
        }
    }
}

public class ParquetFileMetadata
{
    public string Version { get; set; } = string.Empty;
    public long NumRows { get; set; }
    public long FileSize { get; set; }
    public int NumRowGroups { get; set; }
    public int NumColumns { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public Dictionary<string, string> KeyValueMetadata { get; set; } = new();
    public List<ParquetRowGroupInfo> RowGroups { get; set; } = new();
    public List<ParquetColumnMetadata> Columns { get; set; } = new();
}

public class ParquetRowGroupInfo
{
    public int Index { get; set; }
    public long NumRows { get; set; }
    public long TotalByteSize { get; set; }
    public List<ParquetColumnChunk> Columns { get; set; } = new();
}

public class ParquetColumnChunk
{
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Encoding { get; set; } = string.Empty;
    public string Compression { get; set; } = string.Empty;
    public long UncompressedSize { get; set; }
    public long CompressedSize { get; set; }
    public long? MinValue { get; set; }
    public long? MaxValue { get; set; }
    public long? NullCount { get; set; }
    public long? DistinctCount { get; set; }
}

public class ParquetColumnMetadata
{
    public string Name { get; set; } = string.Empty;
    public string PhysicalType { get; set; } = string.Empty;
    public string LogicalType { get; set; } = string.Empty;
    public bool IsRepeated { get; set; }
    public bool IsOptional { get; set; }
    public int MaxDefinitionLevel { get; set; }
    public int MaxRepetitionLevel { get; set; }
    public long TotalSize { get; set; }
    public string DefaultCompression { get; set; } = string.Empty;
}

public class ParquetDataPreview
{
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public bool HasMore { get; set; }
    public List<ParquetColumn> Schema { get; set; } = new();
}

public class ParquetColumn
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Nullable { get; set; } = true;
    public List<ParquetColumn> Children { get; set; } = new();
}

public class ParquetSchema
{
    public string Name { get; set; } = string.Empty;
    public List<ParquetColumn> Columns { get; set; } = new();
    public int MaxDepth { get; set; }
    public bool HasNestedTypes { get; set; }
}

public class ParquetStatistics
{
    public long TotalRows { get; set; }
    public long TotalSize { get; set; }
    public Dictionary<string, ParquetColumnStatistics> ColumnStatistics { get; set; } = new();
    public double CompressionRatio { get; set; }
    public Dictionary<string, int> CompressionCodecUsage { get; set; } = new();
    public Dictionary<string, int> EncodingUsage { get; set; } = new();
}

public class ParquetColumnStatistics
{
    public string ColumnName { get; set; } = string.Empty;
    public long NonNullCount { get; set; }
    public long NullCount { get; set; }
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }
    public long DistinctCount { get; set; }
    public double CompressionRatio { get; set; }
    public long UncompressedSize { get; set; }
    public long CompressedSize { get; set; }
}

public class ParquetConversionOptions
{
    public bool IncludeHeaders { get; set; } = true;
    public string Delimiter { get; set; } = ",";
    public bool FlattenNestedData { get; set; } = false;
    public int MaxNestingLevel { get; set; } = 5;
    public bool IncludeMetadata { get; set; } = false;
}