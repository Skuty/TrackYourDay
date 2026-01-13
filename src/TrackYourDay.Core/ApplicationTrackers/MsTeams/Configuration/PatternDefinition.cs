using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;

/// <summary>
/// Defines a single pattern to match against process name or window title.
/// </summary>
public sealed record PatternDefinition
{
    private const int RegexTimeoutSeconds = 2;

    /// <summary>
    /// The pattern text to match (e.g., "Microsoft Teams", ".*\\| Meeting$").
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// How the pattern should be matched against target string.
    /// </summary>
    public required PatternMatchMode MatchMode { get; init; }

    /// <summary>
    /// Whether pattern matching is case-sensitive.
    /// Default rule uses case-insensitive matching.
    /// </summary>
    public bool CaseSensitive { get; init; }

    /// <summary>
    /// Compiled regex if MatchMode is Regex. Lazy-initialized for performance.
    /// Not serialized—reconstructed from Pattern on deserialization.
    /// </summary>
    [JsonIgnore]
    public Regex? CompiledRegex { get; init; }

    /// <summary>
    /// Creates a pattern definition with regex compilation and validation.
    /// </summary>
    public static PatternDefinition CreateRegexPattern(string pattern, bool caseSensitive)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be empty", nameof(pattern));

        var options = RegexOptions.Compiled;
        if (!caseSensitive)
            options |= RegexOptions.IgnoreCase;

        try
        {
            var timeout = TimeSpan.FromSeconds(RegexTimeoutSeconds);
            var regex = new Regex(pattern, options, timeout);
            
            return new PatternDefinition
            {
                Pattern = pattern,
                MatchMode = PatternMatchMode.Regex,
                CaseSensitive = caseSensitive,
                CompiledRegex = regex
            };
        }
        catch (RegexMatchTimeoutException ex)
        {
            throw new ArgumentException($"Regex pattern timed out during compilation: {pattern}", nameof(pattern), ex);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Invalid regex pattern: {pattern}", nameof(pattern), ex);
        }
    }

    /// <summary>
    /// Creates a string-based pattern definition (non-regex).
    /// </summary>
    public static PatternDefinition CreateStringPattern(string pattern, PatternMatchMode matchMode, bool caseSensitive)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be empty", nameof(pattern));

        if (matchMode == PatternMatchMode.Regex)
            throw new ArgumentException("Use CreateRegexPattern for regex patterns", nameof(matchMode));

        return new PatternDefinition
        {
            Pattern = pattern,
            MatchMode = matchMode,
            CaseSensitive = caseSensitive
        };
    }

    /// <summary>
    /// Evaluates if the input string matches this pattern.
    /// Returns false on regex timeout (never crashes).
    /// </summary>
    public bool Matches(string input, ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        if (MatchMode == PatternMatchMode.Regex)
        {
            if (CompiledRegex is null)
            {
                logger?.LogWarning("Regex pattern not compiled: {Pattern}", Pattern);
                return false;
            }

            try
            {
                return CompiledRegex.IsMatch(input);
            }
            catch (RegexMatchTimeoutException ex)
            {
                logger?.LogWarning(ex, "Regex timeout matching pattern {Pattern} against input length {Length}", Pattern, input.Length);
                return false;
            }
        }

        var comparison = GetStringComparison();
        return MatchMode switch
        {
            PatternMatchMode.Contains => input.Contains(Pattern, comparison),
            PatternMatchMode.StartsWith => input.StartsWith(Pattern, comparison),
            PatternMatchMode.EndsWith => input.EndsWith(Pattern, comparison),
            PatternMatchMode.Exact => input.Equals(Pattern, comparison),
            _ => false
        };
    }

    private StringComparison GetStringComparison() =>
        CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
}
