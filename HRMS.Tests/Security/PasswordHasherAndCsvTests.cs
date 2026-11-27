using System.Text;
using HRMS.Business.Security;
using HRMS.Web.Helpers;
using Xunit;

namespace HRMS.Tests.Security;

public sealed class PasswordHasherAndCsvTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_UsesPbkdf2Sha256Format_WithUniqueSalts()
    {
        var hash1 = _hasher.HashPassword("Str0ng!Pass#2026");
        var hash2 = _hasher.HashPassword("Str0ng!Pass#2026");

        Assert.StartsWith("PBKDF2-SHA256.120000.", hash1);
        Assert.NotEqual(hash1, hash2);
        Assert.True(_hasher.VerifyPassword("Str0ng!Pass#2026", hash1));
        Assert.True(_hasher.VerifyPassword("Str0ng!Pass#2026", hash2));
    }

    [Theory]
    [InlineData("WrongPassword")]
    [InlineData("")]
    [InlineData("   ")]
    public void VerifyPassword_RejectsInvalidPassword(string candidate)
    {
        var hash = _hasher.HashPassword("CorrectHorseBatteryStaple!9");
        Assert.False(_hasher.VerifyPassword(candidate, hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-valid-hash")]
    [InlineData("PBKDF2-SHA256.500.abc.def")] // Below minimum 10,000 iteration threshold
    [InlineData("PBKDF2-SHA256.120000.!!!invalid!!!.def")]
    public void VerifyPassword_RejectsMalformedOrWeakHashes(string storedHash)
    {
        Assert.False(_hasher.VerifyPassword("Str0ng!Pass#2026", storedHash));
    }

    [Fact]
    public void HashPassword_ThrowsOnEmptyInput()
    {
        Assert.Throws<ArgumentException>(() => _hasher.HashPassword("   "));
    }

    [Fact]
    public void BuildCsv_NeutralizesSpreadsheetFormulas_AndEscapesQuotes()
    {
        var rows = new[]
        {
            new { Name = "=CMD|'/C calc'!A0", Note = "+SUM(A1:A2)" },
            new { Name = "-1+1", Note = "@HYPERLINK(\"http://example.com\")" },
            new { Name = "Alice \"QA\" Smith", Note = "Normal text" }
        };

        var bytes = CsvExportHelper.BuildCsv(rows, new (string, Func<dynamic, object?>)[]
        {
            ("Name", r => r.Name),
            ("Note", r => r.Note)
        });

        var preamble = Encoding.UTF8.GetPreamble();
        Assert.True(bytes.Take(preamble.Length).SequenceEqual(preamble));

        var content = Encoding.UTF8.GetString(bytes, preamble.Length, bytes.Length - preamble.Length);
        Assert.Contains("\"'=CMD|'/C calc'!A0\"", content);
        Assert.Contains("\"'+SUM(A1:A2)\"", content);
        Assert.Contains("\"'-1+1\"", content);
        Assert.Contains("\"'@HYPERLINK(\"\"http://example.com\"\")\"", content);
        Assert.Contains("\"Alice \"\"QA\"\" Smith\"", content);
    }
}
