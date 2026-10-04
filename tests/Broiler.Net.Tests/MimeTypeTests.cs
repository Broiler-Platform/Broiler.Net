using System.Text.Json;
using Broiler.Net.Http;

namespace Broiler.Net.Tests;

/// <summary>
/// MIME types parse and serialize as the MIME Sniffing Standard specifies, checked against
/// web-platform-tests' own vectors (<c>Fixtures/wpt/mimesniff</c>).
/// </summary>
public sealed class MimeTypeTests
{
    private static readonly IReadOnlyList<(string Input, string? Output)> Vectors =
        [.. Load("mime-types.json"), .. Load("generated-mime-types.json")];

    public static TheoryData<int> VectorIndexes()
    {
        var indexes = new TheoryData<int>();
        for (var i = 0; i < Vectors.Count; i++)
            indexes.Add(i);
        return indexes;
    }

    /// <summary>
    /// Each vector parses to its expected serialization, or fails where WPT expects failure, and a
    /// serialization parses back to itself.
    /// </summary>
    [Theory]
    [MemberData(nameof(VectorIndexes))]
    public void A_Wpt_Vector_Parses_And_Serializes_As_Expected(int index)
    {
        var (input, output) = Vectors[index];
        var shown = JsonSerializer.Serialize(input);
        var parsed = MimeType.TryParse(input, out var mimeType);
        if (output is null)
        {
            Assert.False(parsed, $"{shown} should not parse");
            return;
        }

        Assert.True(parsed, $"{shown} should parse");
        Assert.Equal(output, mimeType!.ToString());
        Assert.True(MimeType.TryParse(output, out var again));
        Assert.Equal(output, again.ToString());
    }

    [Fact]
    public void The_Wpt_Vectors_Are_All_Read()
    {
        // 74 hand-written vectors and 881 generated ones at the pinned WPT commit.
        Assert.Equal(955, Vectors.Count);
    }

    [Fact]
    public void Names_Are_Lowercase_And_Parameters_Keep_Their_Order_Case_And_First_Value()
    {
        Assert.True(MimeType.TryParse(" Text/HTML ; Charset=\"UTF-8\" ; b=2; a=1; charset=x", out var mimeType));

        Assert.Equal("text", mimeType.Type);
        Assert.Equal("html", mimeType.Subtype);
        Assert.Equal("text/html", mimeType.Essence);
        Assert.Equal(["charset", "b", "a"], mimeType.Parameters.Keys);
        Assert.Equal("UTF-8", mimeType.Charset);
        Assert.Equal("text/html;charset=UTF-8;b=2;a=1", mimeType.ToString());
    }

    [Theory]
    [InlineData("text/plain;a=\"b\\\"c\"", "b\"c", "text/plain;a=\"b\\\"c\"")]
    [InlineData("text/plain;a=\"\"", "", "text/plain;a=\"\"")]
    [InlineData("text/plain;a=\"x", "x", "text/plain;a=x")]
    [InlineData("text/plain;a=\"x\"y;b=z", "x", "text/plain;a=x;b=z")]
    [InlineData("text/plain;a=\"x\\", "x\\", "text/plain;a=\"x\\\\\"")]
    public void A_Quoted_Value_Is_Held_Unquoted_And_Written_Quoted_Only_When_It_Must_Be(string input, string value, string serialization)
    {
        Assert.True(MimeType.TryParse(input, out var mimeType));
        Assert.Equal(value, mimeType.Parameters["a"]);
        Assert.Equal(serialization, mimeType.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("text")]
    [InlineData("/html")]
    [InlineData("text/")]
    [InlineData("text /html")]
    [InlineData("text/html garbage")]
    [InlineData("text/plain/x")]
    [InlineData("teÿxt/html")]
    public void A_Missing_Or_Malformed_Type_Or_Subtype_Does_Not_Parse(string? input) =>
        Assert.False(MimeType.TryParse(input, out _));

    /// <summary>
    /// U+212A KELVIN SIGN lowercases to <c>k</c> in .NET's invariant mapping, but the standard folds
    /// ASCII letters only: a parameter name holding it is no token and is dropped, so it cannot
    /// shadow the real <c>key</c> after it. A type holding it does not parse.
    /// </summary>
    [Fact]
    public void Only_Ascii_Letters_Are_Folded()
    {
        Assert.True(MimeType.TryParse("text/plain;Key=v;key=w", out var mimeType));
        Assert.Equal("text/plain;key=w", mimeType.ToString());

        Assert.False(MimeType.TryParse("text/Key", out _));
    }

    [Theory]
    [InlineData("image/svg+xml", true, false, true, false, false)]
    [InlineData("text/html;charset=utf-8", false, true, false, false, false)]
    [InlineData("application/xhtml+xml", false, false, true, false, false)]
    [InlineData("text/xml", false, false, true, false, false)]
    [InlineData("application/ld+json", false, false, false, true, false)]
    [InlineData("text/json", false, false, false, true, false)]
    [InlineData("TEXT/JavaScript; charset=utf-8", false, false, false, false, true)]
    [InlineData("application/x-ecmascript", false, false, false, false, true)]
    [InlineData("text/javascript1.6", false, false, false, false, false)]
    [InlineData("text/plain", false, false, false, false, false)]
    public void Groups_Are_Read_From_The_Type_And_Essence(string input, bool image, bool html, bool xml, bool json, bool javaScript)
    {
        Assert.True(MimeType.TryParse(input, out var mimeType));

        Assert.Equal(image, mimeType.IsImage);
        Assert.Equal(html, mimeType.IsHtml);
        Assert.Equal(xml, mimeType.IsXml);
        Assert.Equal(json, mimeType.IsJson);
        Assert.Equal(javaScript, mimeType.IsJavaScript);
    }

    /// <summary>
    /// The match is on the string as written, ASCII case-insensitive and nothing wider: the long s
    /// (U+017F) folds to <c>S</c> and the dotless i (U+0131) to <c>I</c> under ordinal ignore-case
    /// comparison, and neither may make a JavaScript essence.
    /// </summary>
    [Theory]
    [InlineData("text/javascript", true)]
    [InlineData("TEXT/JAVASCRIPT", true)]
    [InlineData("application/x-JavaScript", true)]
    [InlineData("text/javascript; charset=utf-8", false)]
    [InlineData(" text/javascript", false)]
    [InlineData("text/ecmaſcript", false)]
    [InlineData("text/javascrıpt", false)]
    [InlineData("module", false)]
    [InlineData(null, false)]
    public void A_JavaScript_Essence_Match_Is_Ascii_Case_Insensitive_And_Unparsed(string? value, bool match) =>
        Assert.Equal(match, MimeType.IsJavaScriptEssenceMatch(value));

    private static IEnumerable<(string, string?)> Load(string file)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "wpt", "mimesniff", "mime-types", "resources", file);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var vectors = new List<(string, string?)>();
        foreach (var entry in document.RootElement.EnumerateArray())
        {
            // Strings between the vectors are section comments.
            if (entry.ValueKind != JsonValueKind.Object)
                continue;

            var output = entry.GetProperty("output");
            vectors.Add((entry.GetProperty("input").GetString()!, output.ValueKind == JsonValueKind.Null ? null : output.GetString()));
        }

        return vectors;
    }
}
