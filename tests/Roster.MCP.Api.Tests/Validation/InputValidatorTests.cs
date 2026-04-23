using Roster.MCP.Api.Validation;

namespace Roster.MCP.Api.Tests.Validation;

public class InputValidatorTests
{
    // ── Category ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("chinese")]
    [InlineData("English")]       // case-insensitive
    [InlineData("SUNDAYSCHOOL")]  // case-insensitive
    [InlineData(null)]            // optional — always valid
    public void ValidateCategory_ValidValues_ReturnsNull(string? category)
    {
        Assert.Null(InputValidator.ValidateCategory(category));
    }

    [Theory]
    [InlineData("friday")]
    [InlineData("")]
    [InlineData("INVALID")]
    public void ValidateCategory_InvalidValues_ReturnsErrorMessage(string category)
    {
        var result = InputValidator.ValidateCategory(category);
        Assert.NotNull(result);
        Assert.Contains(category, result, StringComparison.OrdinalIgnoreCase);
    }

    // ── Date format ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("2024-01-14", "from")]
    [InlineData("2025-12-31", "to")]
    [InlineData(null, "from")]    // optional — always valid
    public void ValidateDate_ValidValues_ReturnsNull(string? date, string param)
    {
        Assert.Null(InputValidator.ValidateDate(date, param));
    }

    [Theory]
    [InlineData("2024/01/14", "from")]  // wrong separator
    [InlineData("14-01-2024", "from")]  // wrong order
    [InlineData("20240114", "from")]    // missing separators
    [InlineData("2024-1-1", "from")]    // missing leading zeros
    public void ValidateDate_InvalidFormat_ReturnsErrorMessage(string date, string param)
    {
        var result = InputValidator.ValidateDate(date, param);
        Assert.NotNull(result);
        Assert.Contains(param, result);
    }

    // ── Date range ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("2024-01-01", "2024-01-31")]  // from < to
    [InlineData("2024-06-15", "2024-06-15")]  // from == to
    public void ValidateDateRange_ValidRange_ReturnsNull(string from, string to)
    {
        Assert.Null(InputValidator.ValidateDateRange(from, to));
    }

    [Fact]
    public void ValidateDateRange_FromAfterTo_ReturnsErrorMessage()
    {
        var result = InputValidator.ValidateDateRange("2024-02-01", "2024-01-01");
        Assert.NotNull(result);
        Assert.Contains("2024-02-01", result);
        Assert.Contains("2024-01-01", result);
    }

    // ── Write request required fields ─────────────────────────────────────────

    [Fact]
    public void ValidateWriteRequest_BothPresent_ReturnsNull()
    {
        Assert.Null(InputValidator.ValidateWriteRequest("2024-01-21", "chinese"));
    }

    [Theory]
    [InlineData(null, "chinese")]
    [InlineData("", "chinese")]
    [InlineData("   ", "chinese")]
    public void ValidateWriteRequest_MissingDate_ReturnsError(string? date, string category)
    {
        var result = InputValidator.ValidateWriteRequest(date, category);
        Assert.NotNull(result);
        Assert.Contains("date", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("2024-01-21", null)]
    [InlineData("2024-01-21", "")]
    public void ValidateWriteRequest_MissingCategory_ReturnsError(string date, string? category)
    {
        var result = InputValidator.ValidateWriteRequest(date, category);
        Assert.NotNull(result);
        Assert.Contains("category", result, StringComparison.OrdinalIgnoreCase);
    }
}
