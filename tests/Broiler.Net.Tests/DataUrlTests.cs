using System.Text;
using System.Text.Json;
using Broiler.Net.Http;

namespace Broiler.Net.Tests;

/// <summary>
/// <c>data:</c> URLs decode as a browser decodes them, checked against web-platform-tests' own
/// vectors (<c>Fixtures/wpt/fetch/data-urls</c>): the serialized MIME type and the body of each.
/// </summary>
public sealed class DataUrlTests
{
    /// <summary>A 1×1 opaque red PNG, without the <c>==</c> its encoder padded it with.</summary>
    private const string UnpaddedRedPng =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFBQIAX8jx0gAAAABJRU5ErkJggg";

    private static readonly IReadOnlyList<Vector> DataUrlVectors = Wpt.Load("data-urls.json", static entry => entry.Length > 2
        ? new Vector(entry[0].GetString()!, entry[1].GetString(), Wpt.Bytes(entry[2]))
        : new Vector(entry[0].GetString()!, null, null));

    private static readonly IReadOnlyList<Vector> Base64Vectors = Wpt.Load("base64.json", static entry => Wpt.Bytes(entry[1]) is { } body
        ? new Vector("data:;base64," + entry[0].GetString(), "text/plain;charset=US-ASCII", body)
        : new Vector("data:;base64," + entry[0].GetString(), null, null));

    /// <summary>
    /// The vectors the URL parser decides rather than the processor: this processor takes the URL as
    /// written, so their failures are the parser's (see <see cref="DataUrl"/>).
    /// </summary>
    private static readonly HashSet<string> ParserVectors = ["data://test:test/,X"];

    public static TheoryData<int> DataUrlVectorIndexes() => Wpt.Indexes(DataUrlVectors.Count);

    public static TheoryData<int> Base64VectorIndexes() => Wpt.Indexes(Base64Vectors.Count);

    [Theory]
    [MemberData(nameof(DataUrlVectorIndexes))]
    public void A_Data_Url_Decodes_As_Wpt_Expects(int index)
    {
        var vector = DataUrlVectors[index];
        if (!ParserVectors.Contains(vector.Input))
            AssertVector(vector);
    }

    [Theory]
    [MemberData(nameof(Base64VectorIndexes))]
    public void A_Base64_Body_Decodes_As_Wpt_Expects(int index) => AssertVector(Base64Vectors[index]);

    [Fact]
    public void The_Wpt_Vectors_Are_All_Read()
    {
        Assert.Equal(72, DataUrlVectors.Count);
        Assert.Equal(80, Base64Vectors.Count);
    }

    [Fact]
    public void An_Unpadded_Base64_Image_Decodes_With_Its_Declared_Type()
    {
        Assert.True(DataUrl.TryParse("data:IMAGE/PNG;base64," + UnpaddedRedPng, out var dataUrl));

        Assert.True(dataUrl.MimeType.IsImage);
        Assert.Equal("image/png", dataUrl.MimeType.ToString());
        Assert.Equal(Convert.FromBase64String(UnpaddedRedPng + "=="), dataUrl.Body.ToArray());
    }

    [Fact]
    public void The_Declared_Parameters_Are_Kept()
    {
        Assert.True(DataUrl.TryParse("data:text/html;charset=ISO-8859-1;base64,PHA+6TwvcD4", out var dataUrl));

        Assert.True(dataUrl.MimeType.IsHtml);
        Assert.Equal("ISO-8859-1", dataUrl.MimeType.Charset);
        Assert.Equal("<p>é</p>", Encoding.Latin1.GetString(dataUrl.Body.Span));
    }

    /// <summary>No type, or one that does not parse, is <c>text/plain;charset=US-ASCII</c>; parameters alone are <c>text/plain</c> with them.</summary>
    [Theory]
    [InlineData("data:,X", "text/plain;charset=US-ASCII")]
    [InlineData("data:image;base64,WA", "text/plain;charset=US-ASCII")]
    [InlineData("data:;charset=x;base64,WA", "text/plain;charset=x")]
    public void A_Missing_Type_Is_Text_Plain(string url, string mimeType)
    {
        Assert.True(DataUrl.TryParse(url, out var dataUrl));
        Assert.Equal(mimeType, dataUrl.MimeType.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("data")]
    [InlineData("data:text/plain")]
    [InlineData("datı:,X")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/plain;base64,A")]
    public void A_Url_That_Does_Not_Decode_Is_No_Data_Url(string? url) => Assert.False(DataUrl.TryParse(url, out _));

    [Fact]
    public void A_Text_Body_Decodes_As_Utf8_Without_Its_Byte_Order_Mark()
    {
        Assert.Equal("é", Body([0xEF, 0xBB, 0xBF, 0xC3, 0xA9]));
        Assert.Equal("﻿A", Body([0xEF, 0xBB, 0xBF, 0xEF, 0xBB, 0xBF, (byte)'A']));
        Assert.Equal("�A", Body([0xFF, (byte)'A']));
        Assert.Equal(string.Empty, Body([]));

        static string Body(byte[] bytes)
        {
            Assert.True(DataUrl.TryParse("data:text/plain;base64," + Convert.ToBase64String(bytes), out var dataUrl));
            return dataUrl.DecodeUtf8();
        }
    }

    private static void AssertVector(Vector vector)
    {
        var decoded = DataUrl.TryParse(vector.Input, out var dataUrl);
        var shown = JsonSerializer.Serialize(vector.Input);
        if (vector.Body is null)
        {
            Assert.False(decoded, $"{shown} should not decode");
            return;
        }

        Assert.True(decoded, $"{shown} should decode");
        Assert.Equal(vector.MimeType, dataUrl!.MimeType.ToString());
        Assert.Equal(vector.Body, dataUrl.Body.ToArray());
    }

    /// <summary>
    /// A WPT vector: the URL, and the serialized MIME type and body it decodes to, both
    /// <see langword="null"/> when it does not decode.
    /// </summary>
    private sealed record Vector(string Input, string? MimeType, byte[]? Body);
}

/// <summary>The forgiving-base64 decode on its own, over the same WPT vectors and the edges they leave out.</summary>
public sealed class ForgivingBase64Tests
{
    private static readonly IReadOnlyList<(string Input, byte[]? Bytes)> Vectors =
        Wpt.Load("base64.json", static entry => (entry[0].GetString()!, Wpt.Bytes(entry[1])));

    public static TheoryData<int> VectorIndexes() => Wpt.Indexes(Vectors.Count);

    /// <summary>
    /// WPT applies these through a <c>data:</c> URL, and none of them leans on the URL parser: the
    /// only characters it would trim are at the end of the URL, which ASCII whitespace removal drops
    /// here as well.
    /// </summary>
    [Theory]
    [MemberData(nameof(VectorIndexes))]
    public void A_Wpt_Vector_Decodes_As_Expected(int index)
    {
        var (input, expected) = Vectors[index];
        var decoded = ForgivingBase64.TryDecode(input, out var bytes);
        Assert.Equal(expected is not null, decoded);
        Assert.Equal(expected, bytes);
    }

    [Theory]
    [InlineData("YQ", "a")]
    [InlineData("YQ==", "a")]
    [InlineData(" Y Q = = ", "a")]
    [InlineData("YWI", "ab")]
    [InlineData("YWI=", "ab")]
    [InlineData("", "")]
    public void Padding_And_Ascii_Whitespace_Are_Optional(string input, string expected)
    {
        Assert.True(ForgivingBase64.TryDecode(input, out var bytes));
        Assert.Equal(expected, Encoding.Latin1.GetString(bytes));
    }

    [Theory]
    [InlineData("Y")]
    [InlineData("YQ=")]
    [InlineData("Y=Q=")]
    [InlineData("YQ\vYQ")]
    [InlineData("YQ ")]
    [InlineData("YQ-_")]
    public void What_A_Browser_Rejects_Does_Not_Decode(string input)
    {
        Assert.False(ForgivingBase64.TryDecode(input, out var bytes));
        Assert.Null(bytes);
    }
}

/// <summary>Reads the WPT vector files copied under <c>Fixtures/wpt/fetch/data-urls/resources</c>.</summary>
internal static class Wpt
{
    public static IReadOnlyList<T> Load<T>(string file, Func<JsonElement[], T> select)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "wpt", "fetch", "data-urls", "resources", file);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return [.. document.RootElement.EnumerateArray()
            .Where(static entry => entry.ValueKind == JsonValueKind.Array)
            .Select(entry => select([.. entry.EnumerateArray()]))];
    }

    public static byte[]? Bytes(JsonElement expected) =>
        expected.ValueKind == JsonValueKind.Null
            ? null
            : [.. expected.EnumerateArray().Select(static b => (byte)b.GetInt32())];

    public static TheoryData<int> Indexes(int count)
    {
        var data = new TheoryData<int>();
        for (var i = 0; i < count; i++)
            data.Add(i);
        return data;
    }
}
