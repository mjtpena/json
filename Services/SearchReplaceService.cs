using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonBlazer.Services;

public class SearchReplaceService
{
    public SearchResult Search(string jsonContent, SearchOptions options)
    {
        var result = new SearchResult();
        
        try
        {
            if (string.IsNullOrWhiteSpace(jsonContent) || string.IsNullOrWhiteSpace(options.SearchTerm))
            {
                return result;
            }

            var searchTerm = options.CaseSensitive ? options.SearchTerm : options.SearchTerm.ToLowerInvariant();
            var content = options.CaseSensitive ? jsonContent : jsonContent.ToLowerInvariant();

            if (options.UseRegex)
            {
                result = SearchWithRegex(jsonContent, options);
            }
            else if (options.WholeWordOnly)
            {
                result = SearchWholeWords(jsonContent, options);
            }
            else
            {
                result = SearchSimple(jsonContent, options);
            }

            if (options.SearchInKeysOnly || options.SearchInValuesOnly)
            {
                result = FilterJsonSpecificResults(jsonContent, result, options);
            }
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        return result;
    }

    public ReplaceResult Replace(string jsonContent, ReplaceOptions options)
    {
        var result = new ReplaceResult();
        
        try
        {
            if (string.IsNullOrWhiteSpace(jsonContent) || string.IsNullOrWhiteSpace(options.SearchTerm))
            {
                result.ModifiedContent = jsonContent;
                return result;
            }

            var searchResult = Search(jsonContent, new SearchOptions
            {
                SearchTerm = options.SearchTerm,
                CaseSensitive = options.CaseSensitive,
                WholeWordOnly = options.WholeWordOnly,
                UseRegex = options.UseRegex,
                SearchInKeysOnly = options.SearchInKeysOnly,
                SearchInValuesOnly = options.SearchInValuesOnly
            });

            if (!searchResult.Matches.Any())
            {
                result.ModifiedContent = jsonContent;
                return result;
            }

            if (options.ReplaceAll)
            {
                result = ReplaceAll(jsonContent, options, searchResult);
            }
            else
            {
                result = ReplaceFirst(jsonContent, options, searchResult);
            }

            // Validate JSON after replacement
            try
            {
                JsonDocument.Parse(result.ModifiedContent);
                result.IsValidJson = true;
            }
            catch
            {
                result.IsValidJson = false;
                result.ValidationError = "The replacement resulted in invalid JSON";
            }
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            result.ModifiedContent = jsonContent;
        }

        return result;
    }

    private SearchResult SearchSimple(string jsonContent, SearchOptions options)
    {
        var result = new SearchResult();
        var searchTerm = options.CaseSensitive ? options.SearchTerm : options.SearchTerm.ToLowerInvariant();
        var content = options.CaseSensitive ? jsonContent : jsonContent.ToLowerInvariant();

        int index = 0;
        while ((index = content.IndexOf(searchTerm, index, StringComparison.Ordinal)) != -1)
        {
            var lineInfo = GetLineInfo(jsonContent, index);
            result.Matches.Add(new SearchMatch
            {
                StartIndex = index,
                EndIndex = index + options.SearchTerm.Length,
                MatchedText = jsonContent.Substring(index, options.SearchTerm.Length),
                LineNumber = lineInfo.LineNumber,
                ColumnNumber = lineInfo.ColumnNumber,
                LineText = lineInfo.LineText
            });
            index += searchTerm.Length;
        }

        result.TotalMatches = result.Matches.Count;
        return result;
    }

    private SearchResult SearchWithRegex(string jsonContent, SearchOptions options)
    {
        var result = new SearchResult();
        
        try
        {
            var regexOptions = RegexOptions.None;
            if (!options.CaseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;

            var regex = new Regex(options.SearchTerm, regexOptions);
            var matches = regex.Matches(jsonContent);

            foreach (Match match in matches)
            {
                var lineInfo = GetLineInfo(jsonContent, match.Index);
                result.Matches.Add(new SearchMatch
                {
                    StartIndex = match.Index,
                    EndIndex = match.Index + match.Length,
                    MatchedText = match.Value,
                    LineNumber = lineInfo.LineNumber,
                    ColumnNumber = lineInfo.ColumnNumber,
                    LineText = lineInfo.LineText,
                    Groups = match.Groups.Cast<Group>().Skip(1).Select(g => g.Value).ToList()
                });
            }
        }
        catch (ArgumentException ex)
        {
            result.Error = $"Invalid regex pattern: {ex.Message}";
        }

        result.TotalMatches = result.Matches.Count;
        return result;
    }

    private SearchResult SearchWholeWords(string jsonContent, SearchOptions options)
    {
        var result = new SearchResult();
        var pattern = $@"\b{Regex.Escape(options.SearchTerm)}\b";
        var regexOptions = options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

        try
        {
            var regex = new Regex(pattern, regexOptions);
            var matches = regex.Matches(jsonContent);

            foreach (Match match in matches)
            {
                var lineInfo = GetLineInfo(jsonContent, match.Index);
                result.Matches.Add(new SearchMatch
                {
                    StartIndex = match.Index,
                    EndIndex = match.Index + match.Length,
                    MatchedText = match.Value,
                    LineNumber = lineInfo.LineNumber,
                    ColumnNumber = lineInfo.ColumnNumber,
                    LineText = lineInfo.LineText
                });
            }
        }
        catch (ArgumentException ex)
        {
            result.Error = $"Error in whole word search: {ex.Message}";
        }

        result.TotalMatches = result.Matches.Count;
        return result;
    }

    private SearchResult FilterJsonSpecificResults(string jsonContent, SearchResult searchResult, SearchOptions options)
    {
        var filteredResult = new SearchResult();
        
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var jsonPositions = AnalyzeJsonPositions(jsonContent);

            foreach (var match in searchResult.Matches)
            {
                var position = jsonPositions.FirstOrDefault(p => 
                    match.StartIndex >= p.StartIndex && match.EndIndex <= p.EndIndex);

                if (position != null)
                {
                    bool includeMatch = false;
                    
                    if (options.SearchInKeysOnly && position.IsKey)
                        includeMatch = true;
                    else if (options.SearchInValuesOnly && !position.IsKey)
                        includeMatch = true;
                    else if (!options.SearchInKeysOnly && !options.SearchInValuesOnly)
                        includeMatch = true;

                    if (includeMatch)
                    {
                        match.JsonPath = position.JsonPath;
                        match.IsInKey = position.IsKey;
                        filteredResult.Matches.Add(match);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // If JSON is invalid, return original results
            return searchResult;
        }

        filteredResult.TotalMatches = filteredResult.Matches.Count;
        return filteredResult;
    }

    private ReplaceResult ReplaceAll(string jsonContent, ReplaceOptions options, SearchResult searchResult)
    {
        var result = new ReplaceResult();
        var modifiedContent = jsonContent;
        var replacementCount = 0;
        var offset = 0;

        // Sort matches by position (descending) to maintain correct indices
        var sortedMatches = searchResult.Matches.OrderByDescending(m => m.StartIndex).ToList();

        foreach (var match in sortedMatches)
        {
            var replacementText = options.ReplacementText;
            
            // Handle regex group replacements
            if (options.UseRegex && match.Groups.Any())
            {
                for (int i = 0; i < match.Groups.Count; i++)
                {
                    replacementText = replacementText.Replace($"${i + 1}", match.Groups[i]);
                }
            }

            modifiedContent = modifiedContent.Remove(match.StartIndex, match.EndIndex - match.StartIndex);
            modifiedContent = modifiedContent.Insert(match.StartIndex, replacementText);
            replacementCount++;
        }

        result.ModifiedContent = modifiedContent;
        result.ReplacementCount = replacementCount;
        return result;
    }

    private ReplaceResult ReplaceFirst(string jsonContent, ReplaceOptions options, SearchResult searchResult)
    {
        var result = new ReplaceResult();
        var modifiedContent = jsonContent;

        if (searchResult.Matches.Any())
        {
            var firstMatch = searchResult.Matches.First();
            var replacementText = options.ReplacementText;
            
            // Handle regex group replacements
            if (options.UseRegex && firstMatch.Groups.Any())
            {
                for (int i = 0; i < firstMatch.Groups.Count; i++)
                {
                    replacementText = replacementText.Replace($"${i + 1}", firstMatch.Groups[i]);
                }
            }

            modifiedContent = modifiedContent.Remove(firstMatch.StartIndex, firstMatch.EndIndex - firstMatch.StartIndex);
            modifiedContent = modifiedContent.Insert(firstMatch.StartIndex, replacementText);
            result.ReplacementCount = 1;
        }

        result.ModifiedContent = modifiedContent;
        return result;
    }

    private LineInfo GetLineInfo(string content, int position)
    {
        var lines = content.Substring(0, position).Split('\n');
        var lineNumber = lines.Length;
        var columnNumber = lines.Last().Length + 1;
        
        var allLines = content.Split('\n');
        var lineText = lineNumber <= allLines.Length ? allLines[lineNumber - 1] : "";

        return new LineInfo
        {
            LineNumber = lineNumber,
            ColumnNumber = columnNumber,
            LineText = lineText
        };
    }

    private List<JsonPosition> AnalyzeJsonPositions(string jsonContent)
    {
        var positions = new List<JsonPosition>();
        // This is a simplified implementation
        // In a full implementation, you'd parse the JSON and track positions of keys and values
        return positions;
    }

    public List<SearchSuggestion> GetSearchSuggestions(string jsonContent)
    {
        var suggestions = new List<SearchSuggestion>();
        
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var keys = new HashSet<string>();
            var values = new HashSet<string>();
            
            ExtractKeysAndValues(jsonDoc.RootElement, keys, values, "");
            
            suggestions.AddRange(keys.Take(10).Select(k => new SearchSuggestion
            {
                Text = k,
                Type = "Key",
                Description = $"JSON property key: {k}"
            }));
            
            suggestions.AddRange(values.Take(10).Select(v => new SearchSuggestion
            {
                Text = v,
                Type = "Value", 
                Description = $"JSON value: {v.Truncate(50)}"
            }));
        }
        catch (JsonException)
        {
            // Return common JSON patterns as suggestions
            suggestions.AddRange(new[]
            {
                new SearchSuggestion { Text = "\".*\"", Type = "Regex", Description = "All string values" },
                new SearchSuggestion { Text = "\\d+", Type = "Regex", Description = "All numbers" },
                new SearchSuggestion { Text = "true|false", Type = "Regex", Description = "All boolean values" },
                new SearchSuggestion { Text = "null", Type = "Value", Description = "Null values" }
            });
        }
        
        return suggestions;
    }

    private void ExtractKeysAndValues(JsonElement element, HashSet<string> keys, HashSet<string> values, string currentPath)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    keys.Add(property.Name);
                    ExtractKeysAndValues(property.Value, keys, values, $"{currentPath}.{property.Name}");
                }
                break;
                
            case JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    ExtractKeysAndValues(item, keys, values, $"{currentPath}[{index}]");
                    index++;
                }
                break;
                
            case JsonValueKind.String:
                values.Add(element.GetString() ?? "");
                break;
                
            case JsonValueKind.Number:
                values.Add(element.ToString());
                break;
                
            case JsonValueKind.True:
            case JsonValueKind.False:
                values.Add(element.ToString());
                break;
        }
    }
}

public class SearchOptions
{
    public string SearchTerm { get; set; } = "";
    public bool CaseSensitive { get; set; } = false;
    public bool WholeWordOnly { get; set; } = false;
    public bool UseRegex { get; set; } = false;
    public bool SearchInKeysOnly { get; set; } = false;
    public bool SearchInValuesOnly { get; set; } = false;
}

public class ReplaceOptions : SearchOptions
{
    public string ReplacementText { get; set; } = "";
    public bool ReplaceAll { get; set; } = false;
}

public class SearchResult
{
    public List<SearchMatch> Matches { get; set; } = new();
    public int TotalMatches { get; set; }
    public string? Error { get; set; }
    public bool HasError => !string.IsNullOrEmpty(Error);
}

public class ReplaceResult
{
    public string ModifiedContent { get; set; } = "";
    public int ReplacementCount { get; set; }
    public bool IsValidJson { get; set; } = true;
    public string? ValidationError { get; set; }
    public string? Error { get; set; }
    public bool HasError => !string.IsNullOrEmpty(Error);
}

public class SearchMatch
{
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public string MatchedText { get; set; } = "";
    public int LineNumber { get; set; }
    public int ColumnNumber { get; set; }
    public string LineText { get; set; } = "";
    public string? JsonPath { get; set; }
    public bool IsInKey { get; set; }
    public List<string> Groups { get; set; } = new();
}

public class SearchSuggestion
{
    public string Text { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
}

public class LineInfo
{
    public int LineNumber { get; set; }
    public int ColumnNumber { get; set; }
    public string LineText { get; set; } = "";
}

public class JsonPosition
{
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public string JsonPath { get; set; } = "";
    public bool IsKey { get; set; }
}

public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }
}