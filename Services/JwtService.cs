using System.Text;
using System.Text.Json;

namespace JsonBlazer.Services;

public class JwtService
{
    public JwtDecodeResult DecodeToken(string jwtToken)
    {
        var result = new JwtDecodeResult();
        
        try
        {
            if (string.IsNullOrWhiteSpace(jwtToken))
            {
                result.IsValid = false;
                result.Error = "JWT token is empty or null";
                return result;
            }
            
            // Remove Bearer prefix if present
            jwtToken = jwtToken.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
            
            var parts = jwtToken.Split('.');
            if (parts.Length != 3)
            {
                result.IsValid = false;
                result.Error = "Invalid JWT format. Expected 3 parts separated by dots.";
                return result;
            }
            
            // Decode header
            result.Header = DecodeJwtPart(parts[0]);
            result.HeaderJson = FormatJson(result.Header);
            
            // Decode payload
            result.Payload = DecodeJwtPart(parts[1]);
            result.PayloadJson = FormatJson(result.Payload);
            
            // Store signature (can't decode without key)
            result.Signature = parts[2];
            
            // Parse claims
            ParseClaims(result);
            
            // Validate expiration
            ValidateExpiration(result);
            
            result.IsValid = true;
            result.RawToken = jwtToken;
            
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Error = $"Failed to decode JWT: {ex.Message}";
        }
        
        return result;
    }
    
    private string DecodeJwtPart(string base64UrlString)
    {
        // Convert base64url to base64
        string base64 = base64UrlString.Replace('-', '+').Replace('_', '/');
        
        // Add padding if needed
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        
        byte[] bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }
    
    private string FormatJson(string json)
    {
        try
        {
            var jsonDocument = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }
    
    private void ParseClaims(JwtDecodeResult result)
    {
        try
        {
            var payloadDoc = JsonDocument.Parse(result.Payload);
            var root = payloadDoc.RootElement;
            
            result.Claims = new Dictionary<string, object>();
            
            foreach (var property in root.EnumerateObject())
            {
                result.Claims[property.Name] = GetPropertyValue(property.Value);
            }
            
            // Extract common claims
            if (result.Claims.ContainsKey("iss"))
                result.Issuer = result.Claims["iss"]?.ToString();
                
            if (result.Claims.ContainsKey("sub"))
                result.Subject = result.Claims["sub"]?.ToString();
                
            if (result.Claims.ContainsKey("aud"))
                result.Audience = result.Claims["aud"]?.ToString();
                
            if (result.Claims.ContainsKey("exp") && long.TryParse(result.Claims["exp"]?.ToString(), out long exp))
            {
                result.ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(exp);
            }
            
            if (result.Claims.ContainsKey("iat") && long.TryParse(result.Claims["iat"]?.ToString(), out long iat))
            {
                result.IssuedAt = DateTimeOffset.FromUnixTimeSeconds(iat);
            }
            
            if (result.Claims.ContainsKey("nbf") && long.TryParse(result.Claims["nbf"]?.ToString(), out long nbf))
            {
                result.NotBefore = DateTimeOffset.FromUnixTimeSeconds(nbf);
            }
            
            if (result.Claims.ContainsKey("jti"))
                result.JwtId = result.Claims["jti"]?.ToString();
        }
        catch (Exception ex)
        {
            result.Error += $" Warning: Failed to parse claims: {ex.Message}";
        }
    }
    
    private object GetPropertyValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(GetPropertyValue).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => GetPropertyValue(p.Value)),
            JsonValueKind.Null => null!,
            _ => element.ToString()
        };
    }
    
    private void ValidateExpiration(JwtDecodeResult result)
    {
        if (result.ExpiresAt.HasValue)
        {
            var now = DateTimeOffset.UtcNow;
            result.IsExpired = now > result.ExpiresAt.Value;
            
            if (result.IsExpired)
            {
                var expiredDuration = now - result.ExpiresAt.Value;
                result.ValidationMessages.Add($"Token expired {expiredDuration.Days} days, {expiredDuration.Hours} hours, {expiredDuration.Minutes} minutes ago");
            }
            else
            {
                var remainingTime = result.ExpiresAt.Value - now;
                result.ValidationMessages.Add($"Token expires in {remainingTime.Days} days, {remainingTime.Hours} hours, {remainingTime.Minutes} minutes");
            }
        }
        
        if (result.NotBefore.HasValue)
        {
            var now = DateTimeOffset.UtcNow;
            if (now < result.NotBefore.Value)
            {
                var waitTime = result.NotBefore.Value - now;
                result.ValidationMessages.Add($"Token not valid for {waitTime.Hours} hours, {waitTime.Minutes} minutes");
            }
        }
    }
    
    public List<string> GetCommonClaims()
    {
        return new List<string>
        {
            "iss", "sub", "aud", "exp", "nbf", "iat", "jti",
            "name", "email", "role", "scope", "permissions",
            "given_name", "family_name", "nickname", "preferred_username",
            "profile", "picture", "website", "email_verified",
            "gender", "birthdate", "zoneinfo", "locale", "phone_number",
            "phone_number_verified", "address", "updated_at"
        };
    }
    
    public string GetClaimDescription(string claim)
    {
        return claim switch
        {
            "iss" => "Issuer - identifies the principal that issued the JWT",
            "sub" => "Subject - identifies the principal that is the subject of the JWT",
            "aud" => "Audience - identifies the recipients that the JWT is intended for",
            "exp" => "Expiration Time - identifies the expiration time on or after which the JWT MUST NOT be accepted",
            "nbf" => "Not Before - identifies the time before which the JWT MUST NOT be accepted",
            "iat" => "Issued At - identifies the time at which the JWT was issued",
            "jti" => "JWT ID - provides a unique identifier for the JWT",
            "name" => "Full name of the user",
            "email" => "Email address of the user",
            "role" => "Role or roles assigned to the user",
            "scope" => "OAuth 2.0 scopes granted to the application",
            _ => $"Custom claim: {claim}"
        };
    }
}

public class JwtDecodeResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public string RawToken { get; set; } = "";
    
    // JWT Parts
    public string Header { get; set; } = "";
    public string Payload { get; set; } = "";
    public string Signature { get; set; } = "";
    
    // Formatted JSON
    public string HeaderJson { get; set; } = "";
    public string PayloadJson { get; set; } = "";
    
    // Standard Claims
    public string? Issuer { get; set; }
    public string? Subject { get; set; }
    public string? Audience { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? NotBefore { get; set; }
    public string? JwtId { get; set; }
    
    // All Claims
    public Dictionary<string, object> Claims { get; set; } = new();
    
    // Validation
    public bool IsExpired { get; set; }
    public List<string> ValidationMessages { get; set; } = new();
}