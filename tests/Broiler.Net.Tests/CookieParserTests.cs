using System.Text;
using Broiler.Net.Cookies;

namespace Broiler.Net.Tests;

public sealed class CookieParserTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static ParsedCookie Parse(string input) => Assert.IsType<ParsedCookie>(CookieParser.Parse(input, Now).Cookie);

    [Theory]
    [InlineData("a=b", "a", "b")]
    [InlineData(" a \t= b \t; anything", "a", "b")]
    [InlineData("a=b=c", "a", "b=c")]
    [InlineData("a=", "a", "")]
    [InlineData("token", "", "token")]
    [InlineData("=token", "", "token")]
    [InlineData("a=%20%0a%3b", "a", "%20%0a%3b")]
    [InlineData("a=\"hello world\"", "a", "\"hello world\"")]
    [InlineData("a=b, c=d", "a", "b, c=d")]
    [InlineData("a=b\tc", "a", "b\tc")]
    public void PreservesPairsAndNeverSplitsCommas(string text, string name, string value)
    {
        var cookie = Parse(text);
        Assert.Equal(name, cookie.Name); Assert.Equal(value, cookie.Value);
    }

    [Theory]
    [InlineData("")][InlineData(" \t")][InlineData("=")][InlineData(";Secure")]
    public void RejectsEmpty(string text) => Assert.Equal(CookieDecision.RejectedEmpty, CookieParser.Parse(text, Now).Rejection);

    [Fact]
    public void RejectsEveryControlExceptTabAnywhereInField()
    {
        foreach (var c in Enumerable.Range(0, 32).Append(127).Where(c => c != 9))
        foreach (var text in new[] { $"a={(char)c}", $"a=b;ignored={(char)c}", $"{(char)c}a=b" })
            Assert.Equal(CookieDecision.RejectedControlCharacter, CookieParser.Parse(text, Now).Rejection);
    }

    [Fact]
    public void OctetLimitsAreSeparateFromFieldAndAttributeLimits()
    {
        Assert.True(CookieParser.Parse("n=" + new string('v', 4095), Now).Success);
        Assert.Equal(CookieDecision.RejectedSize, CookieParser.Parse("n=" + new string('v', 4096), Now).Rejection);
        Assert.True(CookieParser.Parse("n=v;unknown=" + new string('x', 60000), Now).Success);
        // A small name/value exceeds the field bound only through an ignored attribute.
        var atBound = "n=v;unknown=" + new string('x', CookieParser.MaximumFieldBytes - 12);
        Assert.True(CookieParser.Parse(atBound, Now).Success);
        Assert.True(CookieParser.Parse(Encoding.Latin1.GetBytes(atBound), Now).Success);
        Assert.Equal(CookieDecision.RejectedSize, CookieParser.Parse(atBound + "x", Now).Rejection);
        Assert.Equal(CookieDecision.RejectedSize, CookieParser.Parse(Encoding.Latin1.GetBytes(atBound + "x"), Now).Rejection);
        Assert.Equal("/valid", Parse("a=b;Path=/valid;Path=/" + new string('x', 1024)).Path);
        Assert.Equal(1024, Parse("a=b;Path=/" + new string('x', 1023)).Path!.Length);
    }

    [Fact]
    public void LastRecognizedAttributeWinsButMalformedDatesAndAgesAreIgnored()
    {
        var cookie = Parse("a=b; DOMAIN=.EXAMPLE.TEST;domain=;path=/one;Path=/two;secure=no;httponly=false;SameSite=Strict;samesite=bogus;max-age=2;Max-Age=+3;expires=Wed, 09 Jun 2030 10:18:14 GMT");
        Assert.Equal("", cookie.Domain); Assert.Equal("/two", cookie.Path);
        Assert.True(cookie.Secure); Assert.True(cookie.HttpOnly);
        Assert.Equal(CookieSameSite.Default, cookie.SameSite);
        Assert.Equal(Now.AddSeconds(2), cookie.Expires);
        Assert.Equal(Now.AddSeconds(5), Parse("a=b;max-age=1;max-age=5").Expires);
        Assert.Equal(new DateTimeOffset(2026, 6, 9, 10, 18, 14, TimeSpan.Zero), Parse("a=b;Expires=Wed, 09 Jun 2026 10:18:14 GMT;Expires=nonsense").Expires);
    }

    [Theory]
    [InlineData("0")][InlineData("-0")][InlineData("-1")][InlineData("-999999999999999999999999999999")]
    public void NonpositiveAgesExpireImmediately(string age) => Assert.Equal(DateTimeOffset.MinValue, Parse("a=b;max-age=" + age).Expires);

    [Theory]
    [InlineData("")][InlineData("-")][InlineData("+5")][InlineData("1.5")][InlineData("1x")][InlineData("１２")]
    public void InvalidAgesDoNotCreatePersistence(string age)
    {
        var result = CookieParser.Parse("a=b;max-age=" + age, Now);
        if (age == "１２") Assert.Equal(CookieDecision.RejectedEncoding, result.Rejection);
        else Assert.Null(result.Cookie!.Expires);
    }

    [Fact]
    public void ClampsHugeLifetimeAndHandlesDateTimeBoundary()
    {
        Assert.Equal(Now.AddDays(400), Parse("a=b;max-age=" + new string('9', 1024)).Expires);
        Assert.Null(Parse("a=b;max-age=" + new string('9', 1025)).Expires);
        Assert.Equal(Now.AddDays(400), Parse("a=b;Expires=Fri, 31 Dec 9999 23:59:59 GMT").Expires);
        Assert.Equal(DateTimeOffset.MaxValue, CookieParser.Parse("a=b;max-age=20", DateTimeOffset.MaxValue.AddSeconds(-1)).Cookie!.Expires);
    }

    [Fact]
    public void PositiveOffsetNearMaxValueUsesUtcArithmetic()
    {
        var now = new DateTimeOffset(9998, 11, 27, 10, 0, 0, TimeSpan.FromHours(14));
        Assert.Null(CookieParser.Parse("a=b", now).Cookie!.Expires);
        // Clock time plus 400 days would pass DateTime.MaxValue; the UTC instant does not.
        var limit = now.ToUniversalTime().AddDays(400);
        Assert.Equal(limit, CookieParser.Parse("a=b;Max-Age=34560000", now).Cookie!.Expires);
        Assert.Equal(limit, CookieParser.Parse("a=b;Expires=Fri, 31 Dec 9999 23:59:59 GMT", now).Cookie!.Expires);
        var late = new DateTimeOffset(9999, 12, 31, 23, 0, 0, TimeSpan.FromHours(14));
        Assert.Equal(DateTimeOffset.MaxValue, CookieParser.Parse("a=b;Max-Age=86400", late).Cookie!.Expires);
    }

    [Theory]
    [InlineData("Wed, 09 Jun 2026 10:18:14 GMT", 2026, 6, 9, 10, 18, 14)]
    [InlineData("Wednesday, 09-Jun-26 10:18:14 GMT", 2026, 6, 9, 10, 18, 14)]
    [InlineData("Wed Jun  9 10:18:14 2026", 2026, 6, 9, 10, 18, 14)]
    [InlineData("9/June/69 1:2:3", 2069, 6, 9, 1, 2, 3)]
    [InlineData("9 Jun 70 1:2:3", 1970, 6, 9, 1, 2, 3)]
    [InlineData("9x Jun 2026x 1:2:3x", 2026, 6, 9, 1, 2, 3)]
    [InlineData("1 Jan 1601 0:0:0", 1601, 1, 1, 0, 0, 0)]
    public void ParsesCookieDatesWithoutLocale(string text, int y, int m, int d, int h, int min, int s)
    {
        Assert.True(CookieParser.TryParseDate(text, out var result));
        Assert.Equal(new DateTimeOffset(y, m, d, h, min, s, TimeSpan.Zero), result);
    }

    [Theory]
    [InlineData("29 Feb 2025 12:00:00")][InlineData("31 Apr 2026 12:00:00")]
    [InlineData("1 Jan 1600 12:00:00")][InlineData("0 Jan 2026 12:00:00")]
    [InlineData("32 Jan 2026 12:00:00")][InlineData("1 Jan 2026 24:00:00")]
    [InlineData("1 Jan 2026 23:60:00")][InlineData("1 Jan 2026 23:59:60")]
    [InlineData("1 Jan 2026")][InlineData("1 Jan 20260 1:2:3")][InlineData("1 Jan 2026 1:2:300")]
    public void RejectsInvalidDates(string text) => Assert.False(CookieParser.TryParseDate(text, out _));

    [Fact]
    public void WireBytesArePreserved()
    {
        byte[] field = [0x61, 0x3d, 0xc3, 0xa9];
        var cookie = CookieParser.Parse(field, Now).Cookie!;
        Assert.Equal(new byte[] { 0xc3, 0xa9 }, Encoding.Latin1.GetBytes(cookie.Value));
        Assert.DoesNotContain(cookie.Value, cookie.ToString());
    }

    [Fact]
    public void SeededMalformedInputNeverThrowsOrCreatesInvalidParsedFields()
    {
        var random = new Random(6265);
        for (var i = 0; i < 10000; i++)
        {
            var bytes = new byte[random.Next(0, 2048)]; random.NextBytes(bytes);
            if (i % 2 == 0) for (var j = 0; j < bytes.Length; j++) bytes[j] = (byte)(32 + bytes[j] % 95);
            var result = CookieParser.Parse(bytes, Now);
            if (result.Cookie is { } cookie)
            {
                Assert.InRange(cookie.Name.Length + cookie.Value.Length, 1, 4096);
                Assert.DoesNotContain(';', cookie.Name); Assert.DoesNotContain(';', cookie.Value);
                Assert.False(cookie.Expires > Now.AddDays(400));
            }
            else Assert.NotNull(result.Rejection);
        }
    }
}
