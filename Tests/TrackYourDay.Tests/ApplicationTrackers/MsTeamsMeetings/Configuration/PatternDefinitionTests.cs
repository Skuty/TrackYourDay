using FluentAssertions;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings.Configuration;

[Trait("Category", "Unit")]
public class PatternDefinitionTests
{
    [Theory]
    [InlineData("test", "this is a test", true)]
    [InlineData("Test", "this is a test", true)]
    [InlineData("TEST", "this is a test", true)]
    [InlineData("test", "testing", true)]
    [InlineData("missing", "this is a test", false)]
    public void GivenContainsPattern_WhenCaseInsensitive_ThenMatchesCorrectly(string pattern, string input, bool expected)
    {
        // Given
        var patternDef = PatternDefinition.CreateStringPattern(pattern, PatternMatchMode.Contains, caseSensitive: false);

        // When
        var result = patternDef.Matches(input);

        // Then
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("test", "this is a test", true)]
    [InlineData("Test", "this is a test", false)]
    [InlineData("TEST", "this is a test", false)]
    [InlineData("test", "This is a Test", false)]
    public void GivenContainsPattern_WhenCaseSensitive_ThenMatchesCorrectly(string pattern, string input, bool expected)
    {
        // Given
        var patternDef = PatternDefinition.CreateStringPattern(pattern, PatternMatchMode.Contains, caseSensitive: true);

        // When
        var result = patternDef.Matches(input);

        // Then
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Test", "Test meeting", true)]
    [InlineData("test", "Test meeting", true)]
    [InlineData("Meeting", "Test meeting", false)]
    [InlineData("Test", "test", true)]
    public void GivenStartsWithPattern_WhenMatching_ThenMatchesPrefix(string pattern, string input, bool expected)
    {
        // Given
        var patternDef = PatternDefinition.CreateStringPattern(pattern, PatternMatchMode.StartsWith, caseSensitive: false);

        // When
        var result = patternDef.Matches(input);

        // Then
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("meeting", "Test meeting", true)]
    [InlineData("MEETING", "Test meeting", true)]
    [InlineData("Test", "Test meeting", false)]
    [InlineData("ing", "Test meeting", true)]
    public void GivenEndsWithPattern_WhenMatching_ThenMatchesSuffix(string pattern, string input, bool expected)
    {
        // Given
        var patternDef = PatternDefinition.CreateStringPattern(pattern, PatternMatchMode.EndsWith, caseSensitive: false);

        // When
        var result = patternDef.Matches(input);

        // Then
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Microsoft Teams", "Microsoft Teams", true)]
    [InlineData("microsoft teams", "Microsoft Teams", true)]
    [InlineData("Microsoft Teams", "Microsoft Teams Call", false)]
    [InlineData("Teams", "Microsoft Teams", false)]
    public void GivenExactPattern_WhenMatching_ThenMatchesExactly(string pattern, string input, bool expected)
    {
        // Given
        var patternDef = PatternDefinition.CreateStringPattern(pattern, PatternMatchMode.Exact, caseSensitive: false);

        // When
        var result = patternDef.Matches(input);

        // Then
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(@"Meeting \d+", "Meeting 123", true)]
    [InlineData(@"Meeting \d+", "Meeting ABC", false)]
    [InlineData(@"^Test.*end$", "Test in the end", true)]
    [InlineData(@"[A-Z]{3}-\d{3}", "ABC-123", true)]
    [InlineData(@"[A-Z]{3}-\d{3}", "abc-123", false)]
    public void GivenRegexPattern_WhenMatching_ThenEvaluatesRegex(string pattern, string input, bool expected)
    {
        // Given
        var patternDef = PatternDefinition.CreateRegexPattern(pattern, caseSensitive: true);

        // When
        var result = patternDef.Matches(input);

        // Then
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(@"Meeting \d+", "meeting 123", true)]
    [InlineData(@"[A-Z]{3}-\d{3}", "abc-123", true)]
    public void GivenRegexPattern_WhenCaseInsensitive_ThenIgnoresCase(string pattern, string input, bool expected)
    {
        // Given
        var patternDef = PatternDefinition.CreateRegexPattern(pattern, caseSensitive: false);

        // When
        var result = patternDef.Matches(input);

        // Then
        result.Should().Be(expected);
    }

    [Fact]
    public void GivenInvalidRegexSyntax_WhenCreating_ThenThrowsArgumentException()
    {
        // When
        var act = () => PatternDefinition.CreateRegexPattern("[invalid(", caseSensitive: false);

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid regex pattern*");
    }

    [Fact]
    public void GivenCatastrophicBacktrackingPattern_WhenMatching_ThenTimesOutGracefully()
    {
        // Given
        var pattern = PatternDefinition.CreateRegexPattern(@"(a+)+b", caseSensitive: false);
        var input = new string('a', 30) + "X";

        // When
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = pattern.Matches(input);
        sw.Stop();

        // Then
        result.Should().BeFalse();
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GivenEmptyPattern_WhenCreatingString_ThenThrowsArgumentException(string emptyPattern)
    {
        // When
        var act = () => PatternDefinition.CreateStringPattern(emptyPattern, PatternMatchMode.Contains, caseSensitive: false);

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Pattern cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GivenEmptyInput_WhenMatching_ThenReturnsFalse(string emptyInput)
    {
        // Given
        var pattern = PatternDefinition.CreateStringPattern("test", PatternMatchMode.Contains, caseSensitive: false);

        // When
        var result = pattern.Matches(emptyInput);

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public void GivenUnicodePattern_WhenMatching_ThenHandlesUnicodeCorrectly()
    {
        // Given
        var pattern = PatternDefinition.CreateStringPattern("spotkanie", PatternMatchMode.Contains, caseSensitive: false);
        var input = "Spotkanie z zespołem 🎯";

        // When
        var result = pattern.Matches(input);

        // Then
        result.Should().BeTrue();
    }

    [Fact]
    public void GivenRegexMode_WhenCreatingWithStringMethod_ThenThrowsArgumentException()
    {
        // When
        var act = () => PatternDefinition.CreateStringPattern("test", PatternMatchMode.Regex, caseSensitive: false);

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Use CreateRegexPattern for regex patterns*");
    }
}
