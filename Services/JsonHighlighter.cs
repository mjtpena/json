using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonBlazer.Services;

public class JsonHighlighter
{
    // Enhanced regex patterns for better syntax highlighting
    private static readonly Regex JsonKeyRegex = new(@"""([^""\\]|\\.)*""\s*:", RegexOptions.Compiled);
    private static readonly Regex JsonStringRegex = new(@"""([^""\\]|\\.)*""(?!\s*:)", RegexOptions.Compiled);
    private static readonly Regex JsonNumberRegex = new(@"-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?", RegexOptions.Compiled);
    private static readonly Regex JsonBooleanRegex = new(@"\b(true|false)\b", RegexOptions.Compiled);
    private static readonly Regex JsonNullRegex = new(@"\bnull\b", RegexOptions.Compiled);
    private static readonly Regex JsonUrlRegex = new(@"""(https?://[^\s""]+)""", RegexOptions.Compiled);
    private static readonly Regex JsonEmailRegex = new(@"""([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})""", RegexOptions.Compiled);
    private static readonly Regex JsonDateRegex = new(@"""(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{3})?Z?)""", RegexOptions.Compiled);
    
    public string HighlightJson(string json, bool isDarkMode = false)
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
        
        // Apply special pattern highlighting first (URLs, emails, dates)
        html = JsonUrlRegex.Replace(html, match => 
            $"<span class=\"json-url\" style=\"color: {colors.Url}; text-decoration: underline; cursor: pointer;\" title=\"URL\">{match.Value}</span>");
            
        html = JsonEmailRegex.Replace(html, match => 
            $"<span class=\"json-email\" style=\"color: {colors.Email}; font-weight: 500;\" title=\"Email\">{match.Value}</span>");
            
        html = JsonDateRegex.Replace(html, match => 
            $"<span class=\"json-date\" style=\"color: {colors.Date}; font-weight: 500;\" title=\"ISO Date\">{match.Value}</span>");
        
        // Apply basic syntax highlighting
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
        
        // Highlight structural elements with hover effects
        html = html.Replace("{", $"<span class=\"json-brace expandable\" style=\"color: {colors.Brace}; font-weight: bold; cursor: pointer;\" title=\"Object start\">{{</span>");
        html = html.Replace("}", $"<span class=\"json-brace expandable\" style=\"color: {colors.Brace}; font-weight: bold; cursor: pointer;\" title=\"Object end\">}}</span>");
        html = html.Replace("[", $"<span class=\"json-bracket expandable\" style=\"color: {colors.Bracket}; font-weight: bold; cursor: pointer;\" title=\"Array start\">[</span>");
        html = html.Replace("]", $"<span class=\"json-bracket expandable\" style=\"color: {colors.Bracket}; font-weight: bold; cursor: pointer;\" title=\"Array end\">]</span>");
        html = html.Replace(":", $"<span class=\"json-colon\" style=\"color: {colors.Punctuation}; margin: 0 4px;\">:</span>");
        html = html.Replace(",", $"<span class=\"json-comma\" style=\"color: {colors.Punctuation};\">,</span>");
        
        return html;
    }
    
    private static ColorScheme GetColorScheme(bool isDarkMode)
    {
        return isDarkMode ? new ColorScheme
        {
            Key = "#9CDCFE",          // Light blue for keys
            String = "#CE9178",       // Orange for strings
            Number = "#B5CEA8",       // Green for numbers
            Boolean = "#569CD6",      // Blue for booleans
            Null = "#808080",         // Gray for null
            Brace = "#FFD700",        // Gold for braces
            Bracket = "#DA70D6",      // Orchid for brackets
            Punctuation = "#D4D4D4",  // Light gray for punctuation
            Url = "#87CEEB",          // Sky blue for URLs
            Email = "#DDA0DD",        // Plum for emails
            Date = "#98FB98"          // Pale green for dates
        } : new ColorScheme
        {
            Key = "#0451A5",          // Dark blue for keys
            String = "#A31515",       // Dark red for strings
            Number = "#098658",       // Dark green for numbers
            Boolean = "#0000FF",      // Blue for booleans
            Null = "#808080",         // Gray for null
            Brace = "#FF8C00",        // Dark orange for braces
            Bracket = "#8A2BE2",      // Blue violet for brackets
            Punctuation = "#000000",  // Black for punctuation
            Url = "#1E90FF",          // Dodger blue for URLs
            Email = "#BA55D3",        // Medium orchid for emails
            Date = "#32CD32"          // Lime green for dates
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
        public string Url { get; set; } = "#000000";
        public string Email { get; set; } = "#000000";
        public string Date { get; set; } = "#000000";
    }
}