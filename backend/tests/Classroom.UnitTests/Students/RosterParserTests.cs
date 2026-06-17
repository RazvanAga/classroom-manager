using Classroom.Domain.Students;

namespace Classroom.UnitTests.Students;

public class RosterParserTests
{
    [Fact]
    public void Plain_names_parse_with_null_gender()
    {
        var result = RosterParser.Parse("Alice\nBob\nCharlie");

        Assert.Collection(result,
            e => AssertEntry(e, "Alice", null),
            e => AssertEntry(e, "Bob", null),
            e => AssertEntry(e, "Charlie", null));
    }

    [Theory]
    [InlineData("Dana, F", Gender.Female)]
    [InlineData("Dana, f", Gender.Female)]
    [InlineData("Dana, Female", Gender.Female)]
    [InlineData("Dana, female", Gender.Female)]
    [InlineData("Evan, M", Gender.Male)]
    [InlineData("Evan, m", Gender.Male)]
    [InlineData("Evan, Male", Gender.Male)]
    public void Gender_token_is_parsed_case_insensitively(string line, Gender expected)
    {
        var entry = Assert.Single(RosterParser.Parse(line));
        Assert.Equal(expected, entry.Gender);
    }

    [Fact]
    public void Surrounding_whitespace_is_trimmed_from_name_and_marker()
    {
        var entry = Assert.Single(RosterParser.Parse("   Fiona  ,  F  "));

        AssertEntry(entry, "Fiona", Gender.Female);
    }

    [Fact]
    public void Blank_and_whitespace_only_lines_are_skipped()
    {
        var result = RosterParser.Parse("Alice\n\n   \n\t\nBob");

        Assert.Collection(result,
            e => AssertEntry(e, "Alice", null),
            e => AssertEntry(e, "Bob", null));
    }

    [Fact]
    public void Carriage_returns_are_tolerated()
    {
        var result = RosterParser.Parse("Alice\r\nBob, M\r\n");

        Assert.Collection(result,
            e => AssertEntry(e, "Alice", null),
            e => AssertEntry(e, "Bob", Gender.Male));
    }

    [Fact]
    public void Unrecognized_marker_keeps_the_student_with_null_gender()
    {
        var result = RosterParser.Parse("Greg, X\nHana, ");

        Assert.Collection(result,
            e => AssertEntry(e, "Greg", null),
            e => AssertEntry(e, "Hana", null));
    }

    [Fact]
    public void A_line_with_no_name_before_the_comma_is_skipped()
    {
        var result = RosterParser.Parse(", F\nIvan");

        var entry = Assert.Single(result);
        AssertEntry(entry, "Ivan", null);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_or_whitespace_input_yields_no_entries(string? text)
    {
        Assert.Empty(RosterParser.Parse(text));
    }

    private static void AssertEntry(ParsedRosterEntry entry, string name, Gender? gender)
    {
        Assert.Equal(name, entry.Name);
        Assert.Equal(gender, entry.Gender);
    }
}
