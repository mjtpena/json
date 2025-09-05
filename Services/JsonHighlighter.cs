using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonBlazer.Services;

public static class JsonHighlighter
{
    private static readonly Regex JsonKeyRegex = new(@"""([^""\\]|\\.)*""\s*:", RegexOptions.Compiled);
    private static readonly Regex JsonStringRegex = new(@"""([^""\\]|\\.)*""(?!\s*:)", RegexOptions.Compiled);
    private static readonly Regex JsonNumberRegex = new(@"-?\d+\.?\d*([eE][+-]?\d+)?", RegexOptions.Compiled);
    private static readonly Regex JsonBooleanRegex = new(@"\b(true|false)\b", RegexOptions.Compiled);
    private static readonly Regex JsonNullRegex = new(@"\bnull\b", RegexOptions.Compiled);
    
    public static string HighlightJson(string json, bool isDarkMode = false)
    {
        if (string.IsNullOrWhiteSpace(json))
            return string.Empty;
            
        try
        {
            // Validate and format JSON first
            var document = JsonDocument.Parse(json);
            var formatted = JsonSerializer.Serialize(document, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            return ApplyHighlighting(formatted, isDarkMode);
        }
        catch
        {
            // If JSON is invalid, still try to apply highlighting
            return ApplyHighlighting(json, isDarkMode);
        }
    }
    
    private static string ApplyHighlighting(string json, bool isDarkMode)
    {
        var html = System.Net.WebUtility.HtmlEncode(json);
        
        var colors = GetColorScheme(isDarkMode);
        
        // Apply syntax highlighting
        html = JsonKeyRegex.Replace(html, match => 
            $"<span class=\"json-key\" style=\"color: {colors.Key}; font-weight: 600;\">{match.Value}</span>");
            
        html = JsonStringRegex.Replace(html, match => 
            $"<span class=\"json-string\" style=\"color: {colors.String};\">{match.Value}</span>");
            
        html = JsonNumberRegex.Replace(html, match => 
            $"<span class=\"json-number\" style=\"color: {colors.Number}; font-weight: 500;\">{match.Value}</span>");
            
        html = JsonBooleanRegex.Replace(html, match => 
            $"<span class=\"json-boolean\" style=\"color: {colors.Boolean}; font-weight: 600;\">{match.Value}</span>");
            
        html = JsonNullRegex.Replace(html, match => 
            $"<span class=\"json-null\" style=\"color: {colors.Null}; font-style: italic; opacity: 0.8;\">{match.Value}</span>");
        
        // Highlight braces and brackets
        html = html.Replace("{", $"<span class=\"json-brace\" style=\"color: {colors.Brace}; font-weight: bold;\">{{</span>");
        html = html.Replace("}", $"<span class=\"json-brace\" style=\"color: {colors.Brace}; font-weight: bold;\">}}</span>");
        html = html.Replace("[", $"<span class=\"json-bracket\" style=\"color: {colors.Bracket}; font-weight: bold;\">[</span>");
        html = html.Replace("]", $"<span class=\"json-bracket\" style=\"color: {colors.Bracket}; font-weight: bold;\">]</span>");
        html = html.Replace(":", $"<span class=\"json-colon\" style=\"color: {colors.Punctuation};\">:</span>");
        html = html.Replace(",", $"<span class=\"json-comma\" style=\"color: {colors.Punctuation};\">,</span>");
        
        return html;
    }
    
    private static ColorScheme GetColorScheme(bool isDarkMode)
    {
        return isDarkMode ? new ColorScheme
        {
            Key = "#9CDCFE",      // Light blue for keys
            String = "#CE9178",   // Orange for strings
            Number = "#B5CEA8",   // Green for numbers
            Boolean = "#569CD6",  // Blue for booleans
            Null = "#808080",     // Gray for null
            Brace = "#D4D4D4",    // Light gray for braces
            Bracket = "#D4D4D4",  // Light gray for brackets
            Punctuation = "#D4D4D4" // Light gray for punctuation
        } : new ColorScheme
        {
            Key = "#0451A5",      // Dark blue for keys
            String = "#A31515",   // Dark red for strings
            Number = "#098658",   // Dark green for numbers
            Boolean = "#0000FF",  // Blue for booleans
            Null = "#808080",     // Gray for null
            Brace = "#000000",    // Black for braces
            Bracket = "#000000",  // Black for brackets
            Punctuation = "#000000" // Black for punctuation
        };
    }
    
    private class ColorScheme
    {
        public string Key { get; set; } = "#000000";
        public string String { get; set; } = "#000000";
        public string Number { get; set; } = "#000000";
        public string Boolean { get; set; } = "#000000";
        public string Null { get; set; } = "#000000";
        public string Brace { get; set; } = "#000000";
        public string Bracket { get; set; } = "#000000";
        public string Punctuation { get; set; } = "#000000";
    }
}