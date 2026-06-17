namespace Classroom.Domain.Students;

/// <summary>One parsed roster line: a name plus an optional, parsed gender marker.</summary>
public record ParsedRosterEntry(string Name, Gender? Gender);

/// <summary>
/// Pure, unit-testable bulk-paste parser (design.md "Domain: classroom tools"; CLAUDE.md testing
/// seam). Turns pasted text — one student per line, either <c>Name</c> or <c>Name, F/M</c> — into
/// entries. Deliberately <b>tolerant</b>: blank/whitespace-only lines are skipped, surrounding
/// whitespace is trimmed, and an unrecognized gender marker keeps the student with a null gender
/// rather than dropping them. No DB, no I/O.
/// </summary>
public static class RosterParser
{
    public static IReadOnlyList<ParsedRosterEntry> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var entries = new List<ParsedRosterEntry>();

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            // Split into "name" and an optional marker on the first comma; a later comma in the
            // name part is preserved as-is (e.g. "Smith, Jr") only when there is no marker token.
            var commaIndex = line.IndexOf(',');
            if (commaIndex < 0)
            {
                entries.Add(new ParsedRosterEntry(line, null));
                continue;
            }

            var name = line[..commaIndex].Trim();
            if (name.Length == 0)
            {
                continue;
            }

            var marker = line[(commaIndex + 1)..].Trim();
            entries.Add(new ParsedRosterEntry(name, ParseGender(marker)));
        }

        return entries;
    }

    private static Gender? ParseGender(string marker) => marker.ToLowerInvariant() switch
    {
        "f" or "female" => Gender.Female,
        "m" or "male" => Gender.Male,
        _ => null,
    };
}
