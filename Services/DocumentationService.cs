using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonBlazer.Services;

public class DocumentationService
{
    public DocumentationResult GenerateDocumentation(string jsonContent, DocumentationOptions options)
    {
        var result = new DocumentationResult();
        
        try
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                result.Success = false;
                result.Error = "JSON content cannot be empty";
                return result;
            }

            // Validate JSON first
            var jsonDocument = JsonDocument.Parse(jsonContent);
            
            result.Content = options.Type switch
            {
                DocumentationType.ApiDocumentation => GenerateApiDocumentation(jsonContent, options),
                DocumentationType.SchemaDocumentation => GenerateSchemaDocumentation(jsonContent, options),
                DocumentationType.ReadmeTemplate => GenerateReadmeTemplate(jsonContent, options),
                DocumentationType.PostmanCollection => GeneratePostmanCollection(jsonContent, options),
                DocumentationType.OpenApiSpec => GenerateOpenApiSpec(jsonContent, options),
                DocumentationType.TypeScriptInterface => GenerateTypeScriptInterface(jsonContent, options),
                DocumentationType.CSharpClass => GenerateCSharpClass(jsonContent, options),
                _ => GenerateApiDocumentation(jsonContent, options)
            };
            
            result.Success = true;
            result.GeneratedAt = DateTime.UtcNow;
            result.Format = options.OutputFormat;
            result.Type = options.Type;
            
            jsonDocument.Dispose();
        }
        catch (JsonException ex)
        {
            result.Success = false;
            result.Error = $"Invalid JSON: {ex.Message}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Documentation generation failed: {ex.Message}";
        }
        
        return result;
    }

    public List<DocumentationTemplate> GetTemplates()
    {
        return new List<DocumentationTemplate>
        {
            new()
            {
                Name = "REST API Endpoint",
                Description = "Template for documenting REST API endpoints",
                Content = """
                {
                  "endpoint": "/api/users/{id}",
                  "method": "GET",
                  "description": "Retrieve user information by ID",
                  "parameters": {
                    "id": {
                      "type": "integer",
                      "required": true,
                      "description": "Unique user identifier"
                    }
                  },
                  "response": {
                    "id": 1,
                    "name": "John Doe",
                    "email": "john@example.com",
                    "created_at": "2023-01-01T00:00:00Z",
                    "profile": {
                      "avatar": "https://example.com/avatar.jpg",
                      "bio": "Software developer"
                    }
                  },
                  "errors": [
                    {
                      "code": 404,
                      "message": "User not found"
                    },
                    {
                      "code": 400,
                      "message": "Invalid user ID"
                    }
                  ]
                }
                """
            },
            new()
            {
                Name = "JSON Schema",
                Description = "JSON Schema definition template",
                Content = """
                {
                  "$schema": "https://json-schema.org/draft/2020-12/schema",
                  "title": "User",
                  "type": "object",
                  "properties": {
                    "id": {
                      "type": "integer",
                      "description": "Unique user identifier",
                      "minimum": 1
                    },
                    "name": {
                      "type": "string",
                      "description": "User's full name",
                      "minLength": 1,
                      "maxLength": 100
                    },
                    "email": {
                      "type": "string",
                      "format": "email",
                      "description": "User's email address"
                    },
                    "age": {
                      "type": "integer",
                      "minimum": 0,
                      "maximum": 150,
                      "description": "User's age in years"
                    },
                    "preferences": {
                      "type": "object",
                      "properties": {
                        "theme": {
                          "type": "string",
                          "enum": ["light", "dark", "auto"]
                        },
                        "notifications": {
                          "type": "boolean"
                        }
                      }
                    }
                  },
                  "required": ["id", "name", "email"]
                }
                """
            },
            new()
            {
                Name = "Configuration File",
                Description = "Application configuration template",
                Content = """
                {
                  "application": {
                    "name": "MyApp",
                    "version": "1.0.0",
                    "environment": "production"
                  },
                  "database": {
                    "provider": "postgresql",
                    "host": "localhost",
                    "port": 5432,
                    "database": "myapp",
                    "username": "user",
                    "ssl": true,
                    "connectionTimeout": 30
                  },
                  "api": {
                    "baseUrl": "https://api.example.com",
                    "version": "v1",
                    "timeout": 30000,
                    "retries": 3,
                    "rateLimiting": {
                      "enabled": true,
                      "requestsPerMinute": 100
                    }
                  },
                  "features": {
                    "authentication": true,
                    "logging": true,
                    "caching": true,
                    "monitoring": false
                  },
                  "security": {
                    "encryption": {
                      "algorithm": "AES-256-GCM",
                      "keyRotation": "daily"
                    },
                    "cors": {
                      "allowedOrigins": ["https://example.com"],
                      "allowCredentials": true
                    }
                  }
                }
                """
            },
            new()
            {
                Name = "E-commerce Product",
                Description = "Product catalog schema",
                Content = """
                {
                  "id": "PROD-001",
                  "sku": "LAPTOP-DELL-XPS13",
                  "name": "Dell XPS 13 Laptop",
                  "description": "Ultra-thin laptop with premium features",
                  "category": {
                    "id": "electronics",
                    "name": "Electronics",
                    "path": "Electronics > Computers > Laptops"
                  },
                  "pricing": {
                    "basePrice": 999.99,
                    "salePrice": 899.99,
                    "currency": "USD",
                    "discounts": [
                      {
                        "type": "percentage",
                        "value": 10,
                        "validUntil": "2024-12-31"
                      }
                    ]
                  },
                  "inventory": {
                    "inStock": true,
                    "quantity": 25,
                    "warehouse": "US-WEST",
                    "reorderLevel": 5
                  },
                  "specifications": {
                    "processor": "Intel Core i7",
                    "memory": "16GB RAM",
                    "storage": "512GB SSD",
                    "display": "13.3 inch 4K"
                  },
                  "images": [
                    {
                      "url": "https://cdn.example.com/laptop-1.jpg",
                      "alt": "Front view",
                      "primary": true
                    }
                  ],
                  "tags": ["laptop", "dell", "ultrabook", "business"],
                  "createdAt": "2023-01-01T00:00:00Z",
                  "updatedAt": "2023-06-01T00:00:00Z"
                }
                """
            }
        };
    }

    private string GenerateApiDocumentation(string jsonContent, DocumentationOptions options)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        
        return options.OutputFormat switch
        {
            OutputFormat.Markdown => GenerateMarkdownApiDocs(jsonContent, timestamp),
            OutputFormat.Html => GenerateHtmlApiDocs(jsonContent, timestamp),
            OutputFormat.Yaml => GenerateYamlApiDocs(jsonContent),
            OutputFormat.Json => GenerateJsonApiDocs(jsonContent),
            _ => GenerateMarkdownApiDocs(jsonContent, timestamp)
        };
    }

    private string GenerateMarkdownApiDocs(string jsonContent, string timestamp)
    {
        var prettyJson = FormatJson(jsonContent);
        var endpoints = ExtractEndpoints(jsonContent);
        
        var markdown = $"""
            # API Documentation
            
            ## Overview
            This API provides JSON data endpoints with structured responses.
            
            ## Base URL
            ```
            https://api.example.com/v1
            ```
            
            ## Authentication
            Include your API key in the Authorization header:
            ```
            Authorization: Bearer YOUR_API_KEY
            ```
            
            ## Content Type
            All requests and responses use `application/json` content type.
            
            ## Example Request/Response
            ```json
            {prettyJson}
            ```
            
            ## Endpoints
            
            {GenerateEndpointsMarkdown(endpoints)}
            
            ## Error Handling
            
            The API uses conventional HTTP response codes to indicate success or failure.
            
            | Code | Description |
            |------|-------------|
            | 200  | Success |
            | 400  | Bad Request - Invalid parameters |
            | 401  | Unauthorized - Invalid API key |
            | 404  | Not Found - Resource doesn't exist |
            | 429  | Too Many Requests - Rate limit exceeded |
            | 500  | Internal Server Error |
            
            ## Rate Limiting
            
            API calls are limited to 1000 requests per hour per API key.
            
            ## SDKs and Libraries
            
            - JavaScript: `npm install api-client`
            - Python: `pip install api-client`
            - PHP: `composer require api-client`
            
            ---
            *Documentation generated on {timestamp}*
            """;
            
        return markdown;
    }

    private string GenerateHtmlApiDocs(string jsonContent, string timestamp)
    {
        var prettyJson = FormatJson(jsonContent);
        
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>API Documentation</title>
                <style>
                    body {{ 
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; 
                        margin: 0; 
                        padding: 20px; 
                        line-height: 1.6; 
                        background: #f8f9fa; 
                    }}
                    .container {{ 
                        max-width: 1200px; 
                        margin: 0 auto; 
                        background: white; 
                        padding: 30px; 
                        border-radius: 8px; 
                        box-shadow: 0 2px 10px rgba(0,0,0,0.1); 
                    }}
                    .code {{ 
                        background: #f8f8f8; 
                        padding: 15px; 
                        border-radius: 6px; 
                        border-left: 4px solid #007acc; 
                        overflow-x: auto; 
                        font-family: 'Monaco', 'Menlo', monospace; 
                        font-size: 14px; 
                    }}
                    .method {{ 
                        display: inline-block; 
                        background: #28a745; 
                        color: white; 
                        padding: 4px 12px; 
                        border-radius: 4px; 
                        font-weight: bold; 
                        font-size: 12px; 
                        margin-right: 10px; 
                    }}
                    .method.post {{ background: #ffc107; color: #212529; }}
                    .method.put {{ background: #17a2b8; }}
                    .method.delete {{ background: #dc3545; }}
                    table {{ 
                        width: 100%; 
                        border-collapse: collapse; 
                        margin: 20px 0; 
                    }}
                    th, td {{ 
                        border: 1px solid #ddd; 
                        padding: 12px; 
                        text-align: left; 
                    }}
                    th {{ background: #f8f9fa; font-weight: 600; }}
                    .endpoint {{ 
                        background: #fff3cd; 
                        padding: 15px; 
                        margin: 10px 0; 
                        border-radius: 6px; 
                        border-left: 4px solid #ffc107; 
                    }}
                    h1 {{ color: #2c3e50; }}
                    h2 {{ color: #34495e; border-bottom: 2px solid #3498db; padding-bottom: 10px; }}
                    h3 {{ color: #7f8c8d; }}
                </style>
            </head>
            <body>
                <div class="container">
                    <h1>🚀 API Documentation</h1>
                    
                    <h2>Overview</h2>
                    <p>This API provides JSON data endpoints with structured responses. All endpoints return JSON data and support standard HTTP methods.</p>
                    
                    <h2>Authentication</h2>
                    <div class="code">Authorization: Bearer YOUR_API_KEY</div>
                    
                    <h2>Example Response</h2>
                    <div class="code"><pre>{prettyJson}</pre></div>
                    
                    <h2>Endpoints</h2>
                    <div class="endpoint">
                        <h3><span class="method">GET</span>/api/data</h3>
                        <p>Retrieves the main data structure.</p>
                        <p><strong>Parameters:</strong> None</p>
                        <p><strong>Response:</strong> JSON object as shown above</p>
                    </div>
                    
                    <h2>Error Codes</h2>
                    <table>
                        <thead>
                            <tr><th>Code</th><th>Description</th></tr>
                        </thead>
                        <tbody>
                            <tr><td>200</td><td>Success</td></tr>
                            <tr><td>400</td><td>Bad Request</td></tr>
                            <tr><td>401</td><td>Unauthorized</td></tr>
                            <tr><td>404</td><td>Not Found</td></tr>
                            <tr><td>500</td><td>Internal Server Error</td></tr>
                        </tbody>
                    </table>
                    
                    <footer style="margin-top: 40px; padding-top: 20px; border-top: 1px solid #eee; color: #6c757d; font-size: 14px;">
                        <em>Documentation generated on {timestamp}</em>
                    </footer>
                </div>
            </body>
            </html>
            """;
    }

    private string GenerateYamlApiDocs(string jsonContent)
    {
        return $"""
            openapi: 3.0.3
            info:
              title: JSON API
              description: Auto-generated API documentation from JSON data
              version: 1.0.0
              contact:
                name: API Support
                email: support@example.com
              license:
                name: MIT
                url: https://opensource.org/licenses/MIT
            
            servers:
              - url: https://api.example.com/v1
                description: Production server
              - url: https://staging-api.example.com/v1
                description: Staging server
            
            security:
              - bearerAuth: []
            
            paths:
              /api/data:
                get:
                  summary: Get JSON data
                  description: Retrieves the main JSON data structure
                  operationId: getData
                  tags:
                    - Data
                  responses:
                    '200':
                      description: Successful response
                      content:
                        application/json:
                          schema:
                            type: object
                          example: {FormatJsonForYaml(jsonContent)}
                    '400':
                      description: Bad request
                      content:
                        application/json:
                          schema:
                            $ref: '#/components/schemas/Error'
                    '401':
                      description: Unauthorized
                      content:
                        application/json:
                          schema:
                            $ref: '#/components/schemas/Error'
                    '500':
                      description: Internal server error
                      content:
                        application/json:
                          schema:
                            $ref: '#/components/schemas/Error'
            
            components:
              securitySchemes:
                bearerAuth:
                  type: http
                  scheme: bearer
                  bearerFormat: JWT
              
              schemas:
                Error:
                  type: object
                  required:
                    - code
                    - message
                  properties:
                    code:
                      type: integer
                      format: int32
                    message:
                      type: string
                    details:
                      type: string
            
            tags:
              - name: Data
                description: Data management operations
            """;
    }

    private string GenerateJsonApiDocs(string jsonContent)
    {
        return JsonSerializer.Serialize(new
        {
            apiVersion = "1.0.0",
            title = "JSON API Documentation",
            description = "Auto-generated API documentation",
            baseUrl = "https://api.example.com/v1",
            authentication = new { type = "Bearer", description = "API Key required" },
            endpoints = new[]
            {
                new
                {
                    path = "/api/data",
                    method = "GET",
                    description = "Get JSON data",
                    response = JsonDocument.Parse(jsonContent).RootElement
                }
            },
            generatedAt = DateTime.UtcNow
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    private string GenerateSchemaDocumentation(string jsonContent, DocumentationOptions options)
    {
        var analysis = AnalyzeJsonStructure(jsonContent);
        
        return $"""
            # JSON Schema Documentation
            
            ## Structure Analysis
            
            - **Total Properties**: {analysis.PropertyCount}
            - **Maximum Depth**: {analysis.MaxDepth}
            - **Data Types Found**: {string.Join(", ", analysis.DataTypes)}
            - **Array Count**: {analysis.ArrayCount}
            - **Object Count**: {analysis.ObjectCount}
            
            ## Schema Definition
            
            ```json
            {FormatJson(jsonContent)}
            ```
            
            ## Field Descriptions
            
            {GenerateFieldDescriptions(jsonContent)}
            
            ## Validation Rules
            
            - All fields are validated according to JSON Schema specification
            - Required fields must be present in all requests
            - Optional fields can be omitted
            - Data types must match the specified schema
            
            ---
            *Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}*
            """;
    }

    private string GenerateReadmeTemplate(string jsonContent, DocumentationOptions options)
    {
        return $"""
            # Project Name
            
            ![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)
            ![License](https://img.shields.io/badge/license-MIT-green.svg)
            
            ## Description
            
            This project provides comprehensive JSON data processing and API capabilities with a focus on developer experience and performance.
            
            ## Features
            
            - 🚀 Fast JSON processing
            - 📊 Data validation and schema support
            - 🔍 Advanced querying capabilities
            - 🛠️ Developer-friendly API
            - 📚 Comprehensive documentation
            - 🔒 Built-in security features
            
            ## Installation
            
            ```bash
            # Using npm
            npm install your-project-name
            
            # Using yarn
            yarn add your-project-name
            
            # Using pnpm
            pnpm add your-project-name
            ```
            
            ## Quick Start
            
            ```javascript
            import {{ JsonProcessor }} from 'your-project-name';
            
            const processor = new JsonProcessor();
            const data = {FormatJson(jsonContent)};
            
            // Process your JSON data
            const result = processor.process(data);
            console.log(result);
            ```
            
            ## API Reference
            
            ### Methods
            
            #### `process(data: object): ProcessedData`
            
            Processes the input JSON data and returns a structured result.
            
            **Parameters:**
            - `data` (object): The JSON data to process
            
            **Returns:**
            - `ProcessedData`: The processed result
            
            ## Configuration
            
            ```javascript
            const config = {{
              validateSchema: true,
              enableCaching: true,
              maxDepth: 10,
              timeout: 5000
            }};
            
            const processor = new JsonProcessor(config);
            ```
            
            ## Examples
            
            ### Basic Usage
            
            ```javascript
            const data = {FormatJson(jsonContent)};
            const result = processor.process(data);
            ```
            
            ### Advanced Usage
            
            ```javascript
            const processor = new JsonProcessor({{
              validateSchema: true,
              transformKeys: 'camelCase'
            }});
            
            const result = await processor.processAsync(data);
            ```
            
            ## Contributing
            
            1. Fork the repository
            2. Create a feature branch (`git checkout -b feature/amazing-feature`)
            3. Commit your changes (`git commit -m 'Add amazing feature'`)
            4. Push to the branch (`git push origin feature/amazing-feature`)
            5. Open a Pull Request
            
            ## License
            
            This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
            
            ## Support
            
            - 📧 Email: support@example.com
            - 💬 Discord: [Join our server](https://discord.gg/example)
            - 🐛 Issues: [GitHub Issues](https://github.com/username/repo/issues)
            
            ---
            
            **Made with ❤️ by [Your Name](https://github.com/username)**
            """;
    }

    private string GeneratePostmanCollection(string jsonContent, DocumentationOptions options)
    {
        return JsonSerializer.Serialize(new
        {
            info = new
            {
                _postman_id = Guid.NewGuid().ToString(),
                name = "JSON API Collection",
                description = "Auto-generated Postman collection from JSON data",
                schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            item = new[]
            {
                new
                {
                    name = "Get JSON Data",
                    @event = new[]
                    {
                        new
                        {
                            listen = "prerequest",
                            script = new
                            {
                                exec = new[]
                                {
                                    "// Set timestamp for request tracking",
                                    "pm.globals.set('timestamp', Date.now());"
                                }
                            }
                        },
                        new
                        {
                            listen = "test",
                            script = new
                            {
                                exec = new[]
                                {
                                    "pm.test('Status code is 200', function () {",
                                    "    pm.response.to.have.status(200);",
                                    "});",
                                    "",
                                    "pm.test('Response is JSON', function () {",
                                    "    pm.response.to.be.json;",
                                    "});",
                                    "",
                                    "pm.test('Response has required fields', function () {",
                                    "    const jsonData = pm.response.json();",
                                    "    pm.expect(jsonData).to.be.an('object');",
                                    "});"
                                }
                            }
                        }
                    },
                    request = new
                    {
                        method = "GET",
                        header = new[]
                        {
                            new
                            {
                                key = "Authorization",
                                value = "Bearer {{api_key}}",
                                type = "text"
                            },
                            new
                            {
                                key = "Accept",
                                value = "application/json",
                                type = "text"
                            }
                        },
                        url = new
                        {
                            raw = "{{base_url}}/api/data",
                            host = new[] { "{{base_url}}" },
                            path = new[] { "api", "data" }
                        },
                        description = "Retrieve JSON data from the API endpoint"
                    },
                    response = new[]
                    {
                        new
                        {
                            name = "Success Response",
                            originalRequest = new
                            {
                                method = "GET",
                                header = new[]
                                {
                                    new
                                    {
                                        key = "Authorization",
                                        value = "Bearer {{api_key}}"
                                    }
                                },
                                url = new
                                {
                                    raw = "{{base_url}}/api/data",
                                    host = new[] { "{{base_url}}" },
                                    path = new[] { "api", "data" }
                                }
                            },
                            status = "OK",
                            code = 200,
                            _postman_previewlanguage = "json",
                            header = new[]
                            {
                                new
                                {
                                    key = "Content-Type",
                                    value = "application/json"
                                }
                            },
                            cookie = new object[] { },
                            body = jsonContent
                        }
                    }
                }
            },
            @variable = new[]
            {
                new
                {
                    key = "base_url",
                    value = "https://api.example.com/v1",
                    type = "string"
                },
                new
                {
                    key = "api_key",
                    value = "your-api-key-here",
                    type = "string"
                }
            }
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    private string GenerateOpenApiSpec(string jsonContent, DocumentationOptions options)
    {
        // Similar to YAML but more comprehensive
        return GenerateYamlApiDocs(jsonContent);
    }

    private string GenerateTypeScriptInterface(string jsonContent, DocumentationOptions options)
    {
        var interfaces = GenerateTypeScriptInterfaces(jsonContent);
        return $"""
            // Auto-generated TypeScript interfaces
            // Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}
            
            {interfaces}
            
            // Usage example:
            // const data: RootInterface = {FormatJson(jsonContent)};
            """;
    }

    private string GenerateCSharpClass(string jsonContent, DocumentationOptions options)
    {
        var classes = GenerateCSharpClasses(jsonContent);
        return $"""
            // Auto-generated C# classes
            // Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}
            
            using System;
            using System.Collections.Generic;
            using System.Text.Json.Serialization;
            
            namespace GeneratedModels
            {{
            {classes}
            }}
            """;
    }

    // Helper methods
    private string FormatJson(string json)
    {
        try
        {
            var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }

    private string FormatJsonForYaml(string json)
    {
        return json.Replace("\n", "\n                ");
    }

    private List<string> ExtractEndpoints(string jsonContent)
    {
        var endpoints = new List<string>();
        try
        {
            var doc = JsonDocument.Parse(jsonContent);
            if (doc.RootElement.TryGetProperty("endpoint", out var endpointProp))
            {
                endpoints.Add(endpointProp.GetString() ?? "/api/data");
            }
        }
        catch
        {
            endpoints.Add("/api/data");
        }
        return endpoints;
    }

    private string GenerateEndpointsMarkdown(List<string> endpoints)
    {
        var markdown = "";
        foreach (var endpoint in endpoints)
        {
            markdown += $"""
                ### GET {endpoint}
                
                Retrieves data from the specified endpoint.
                
                **Response:**
                ```json
                {{
                  "status": "success",
                  "data": {{...}}
                }}
                ```
                
                """;
        }
        return markdown;
    }

    private JsonStructureAnalysis AnalyzeJsonStructure(string jsonContent)
    {
        var analysis = new JsonStructureAnalysis();
        try
        {
            var doc = JsonDocument.Parse(jsonContent);
            AnalyzeElement(doc.RootElement, analysis, 0);
        }
        catch { }
        return analysis;
    }

    private void AnalyzeElement(JsonElement element, JsonStructureAnalysis analysis, int depth)
    {
        analysis.MaxDepth = Math.Max(analysis.MaxDepth, depth);
        
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                analysis.ObjectCount++;
                analysis.PropertyCount += element.GetArrayLength();
                foreach (var property in element.EnumerateObject())
                {
                    AnalyzeElement(property.Value, analysis, depth + 1);
                }
                break;
                
            case JsonValueKind.Array:
                analysis.ArrayCount++;
                foreach (var item in element.EnumerateArray())
                {
                    AnalyzeElement(item, analysis, depth + 1);
                }
                break;
                
            case JsonValueKind.String:
                analysis.DataTypes.Add("string");
                break;
                
            case JsonValueKind.Number:
                analysis.DataTypes.Add("number");
                break;
                
            case JsonValueKind.True:
            case JsonValueKind.False:
                analysis.DataTypes.Add("boolean");
                break;
        }
    }

    private string GenerateFieldDescriptions(string jsonContent)
    {
        // Simple implementation - in a real scenario, you'd analyze the JSON structure
        return "Field descriptions would be auto-generated based on the JSON structure and naming conventions.";
    }

    private string GenerateTypeScriptInterfaces(string jsonContent)
    {
        // Simple implementation - would need more complex logic for full TS interface generation
        return """
            interface RootInterface {
              // Properties would be auto-generated from JSON structure
              [key: string]: any;
            }
            """;
    }

    private string GenerateCSharpClasses(string jsonContent)
    {
        // Simple implementation - would need more complex logic for full C# class generation
        return """
                public class RootClass
                {
                    // Properties would be auto-generated from JSON structure
                }
            """;
    }
}

public class DocumentationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string Content { get; set; } = "";
    public OutputFormat Format { get; set; }
    public DocumentationType Type { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class DocumentationOptions
{
    public DocumentationType Type { get; set; } = DocumentationType.ApiDocumentation;
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Markdown;
    public bool IncludeExamples { get; set; } = true;
    public bool IncludeErrorCodes { get; set; } = true;
    public string ProjectName { get; set; } = "API Project";
    public string BaseUrl { get; set; } = "https://api.example.com";
}

public class DocumentationTemplate
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Content { get; set; } = "";
    public DocumentationType Type { get; set; } = DocumentationType.ApiDocumentation;
}

public class JsonStructureAnalysis
{
    public int PropertyCount { get; set; }
    public int MaxDepth { get; set; }
    public int ArrayCount { get; set; }
    public int ObjectCount { get; set; }
    public HashSet<string> DataTypes { get; set; } = new();
}

public enum DocumentationType
{
    ApiDocumentation,
    SchemaDocumentation,
    ReadmeTemplate,
    PostmanCollection,
    OpenApiSpec,
    TypeScriptInterface,
    CSharpClass
}

public enum OutputFormat
{
    Markdown,
    Html,
    Yaml,
    Json
}