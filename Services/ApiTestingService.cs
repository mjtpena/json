using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace JsonBlazer.Services;

public class ApiTestingService
{
    private readonly HttpClient _httpClient;
    
    public ApiTestingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }
    
    public async Task<ApiTestResult> ExecuteRequestAsync(ApiTestRequest request)
    {
        var result = new ApiTestResult
        {
            RequestUrl = request.Url,
            RequestMethod = request.Method,
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            using var httpRequest = CreateHttpRequestMessage(request);
            
            result.RequestHeaders = GetRequestHeaders(httpRequest);
            result.RequestBody = request.Body;
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            using var response = await _httpClient.SendAsync(httpRequest);
            
            stopwatch.Stop();
            result.ResponseTime = stopwatch.ElapsedMilliseconds;
            result.EndTime = DateTime.UtcNow;
            
            // Response details
            result.StatusCode = (int)response.StatusCode;
            result.StatusText = response.ReasonPhrase ?? "";
            result.IsSuccess = response.IsSuccessStatusCode;
            
            // Response headers
            result.ResponseHeaders = GetResponseHeaders(response);
            
            // Response body
            result.ResponseBody = await response.Content.ReadAsStringAsync();
            
            // Try to format as JSON if possible
            result.FormattedResponseBody = TryFormatJson(result.ResponseBody);
            
            // Content type
            result.ContentType = response.Content.Headers.ContentType?.ToString() ?? "";
            result.ContentLength = response.Content.Headers.ContentLength ?? result.ResponseBody.Length;
            
            result.Success = true;
            
        }
        catch (HttpRequestException ex)
        {
            result.Success = false;
            result.Error = $"HTTP Request Error: {ex.Message}";
            result.EndTime = DateTime.UtcNow;
        }
        catch (TaskCanceledException ex)
        {
            result.Success = false;
            result.Error = ex.InnerException is TimeoutException ? "Request timeout" : "Request canceled";
            result.EndTime = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Unexpected error: {ex.Message}";
            result.EndTime = DateTime.UtcNow;
        }
        
        return result;
    }
    
    private HttpRequestMessage CreateHttpRequestMessage(ApiTestRequest request)
    {
        var httpRequest = new HttpRequestMessage
        {
            Method = new HttpMethod(request.Method.ToUpper()),
            RequestUri = new Uri(request.Url)
        };
        
        // Add headers
        foreach (var header in request.Headers)
        {
            if (string.IsNullOrWhiteSpace(header.Key) || string.IsNullOrWhiteSpace(header.Value))
                continue;
                
            try
            {
                if (IsContentHeader(header.Key))
                {
                    // Will be added with content
                    continue;
                }
                else
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                }
            }
            catch
            {
                // Invalid header, skip
            }
        }
        
        // Add body for methods that support it
        if (!string.IsNullOrEmpty(request.Body) && 
            (request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
             request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
             request.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase)))
        {
            var contentTypeHeader = request.Headers.FirstOrDefault(h => 
                h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));
            var contentType = !string.IsNullOrEmpty(contentTypeHeader.Key) ? contentTypeHeader.Value : "application/json";
                
            httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, contentType);
            
            // Add content headers
            foreach (var header in request.Headers.Where(h => IsContentHeader(h.Key)))
            {
                try
                {
                    httpRequest.Content.Headers.Add(header.Key, header.Value);
                }
                catch
                {
                    // Invalid content header, skip
                }
            }
        }
        
        return httpRequest;
    }
    
    private bool IsContentHeader(string headerName)
    {
        return headerName.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Language", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Location", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Range", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase);
    }
    
    private Dictionary<string, string> GetRequestHeaders(HttpRequestMessage request)
    {
        var headers = new Dictionary<string, string>();
        
        foreach (var header in request.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }
        
        if (request.Content?.Headers != null)
        {
            foreach (var header in request.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
        }
        
        return headers;
    }
    
    private Dictionary<string, string> GetResponseHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>();
        
        foreach (var header in response.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }
        
        foreach (var header in response.Content.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }
        
        return headers;
    }
    
    private string TryFormatJson(string content)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content)) return content;
            
            var jsonDocument = JsonDocument.Parse(content);
            return JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return content;
        }
    }
    
    public List<ApiTestTemplate> GetCommonTemplates()
    {
        return new List<ApiTestTemplate>
        {
            new()
            {
                Name = "GET JSON API",
                Method = "GET",
                Url = "https://jsonplaceholder.typicode.com/posts/1",
                Headers = new() { { "Accept", "application/json" } }
            },
            new()
            {
                Name = "POST JSON Data",
                Method = "POST",
                Url = "https://jsonplaceholder.typicode.com/posts",
                Headers = new() { { "Content-Type", "application/json" }, { "Accept", "application/json" } },
                Body = """
                {
                  "title": "foo",
                  "body": "bar",
                  "userId": 1
                }
                """
            },
            new()
            {
                Name = "PUT Update",
                Method = "PUT",
                Url = "https://jsonplaceholder.typicode.com/posts/1",
                Headers = new() { { "Content-Type", "application/json" } },
                Body = """
                {
                  "id": 1,
                  "title": "updated title",
                  "body": "updated body",
                  "userId": 1
                }
                """
            },
            new()
            {
                Name = "DELETE Resource",
                Method = "DELETE",
                Url = "https://jsonplaceholder.typicode.com/posts/1",
                Headers = new() { { "Accept", "application/json" } }
            },
            new()
            {
                Name = "GraphQL Query",
                Method = "POST",
                Url = "https://api.github.com/graphql",
                Headers = new() 
                { 
                    { "Content-Type", "application/json" },
                    { "Authorization", "Bearer YOUR_TOKEN_HERE" }
                },
                Body = """
                {
                  "query": "query { viewer { login } }"
                }
                """
            }
        };
    }
}

public class ApiTestRequest
{
    public string Url { get; set; } = "";
    public string Method { get; set; } = "GET";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = "";
}

public class ApiTestResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    
    // Request Info
    public string RequestUrl { get; set; } = "";
    public string RequestMethod { get; set; } = "";
    public Dictionary<string, string> RequestHeaders { get; set; } = new();
    public string RequestBody { get; set; } = "";
    
    // Response Info
    public int StatusCode { get; set; }
    public string StatusText { get; set; } = "";
    public bool IsSuccess { get; set; }
    public Dictionary<string, string> ResponseHeaders { get; set; } = new();
    public string ResponseBody { get; set; } = "";
    public string FormattedResponseBody { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long ContentLength { get; set; }
    
    // Timing
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long ResponseTime { get; set; } // milliseconds
}

public class ApiTestTemplate
{
    public string Name { get; set; } = "";
    public string Method { get; set; } = "";
    public string Url { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = "";
}