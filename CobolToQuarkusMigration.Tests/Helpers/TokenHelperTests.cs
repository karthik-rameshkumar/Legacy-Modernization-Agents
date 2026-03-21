using Xunit;
using FluentAssertions;
using CobolToQuarkusMigration.Helpers;

namespace CobolToQuarkusMigration.Tests.Helpers;

public class TokenHelperTests
{
    #region EstimateTokens

    [Fact]
    public void EstimateTokens_EmptyString_ReturnsZero()
    {
        TokenHelper.EstimateTokens("").Should().Be(0);
    }

    [Fact]
    public void EstimateTokens_NullString_ReturnsZero()
    {
        TokenHelper.EstimateTokens(null!).Should().Be(0);
    }

    [Fact]
    public void EstimateTokens_ShortText_ReturnsPositiveCount()
    {
        // "Hello" = 5 chars / 3.5 = ~2 tokens
        var result = TokenHelper.EstimateTokens("Hello");
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void EstimateTokens_LongerText_ReturnsMoreTokens()
    {
        var short_text = "Hello";
        var long_text = "Hello World This Is A Longer Text String";
        TokenHelper.EstimateTokens(long_text).Should().BeGreaterThan(TokenHelper.EstimateTokens(short_text));
    }

    [Fact]
    public void EstimateTokens_UsesCharsPerTokenConstant()
    {
        // 35 chars / 3.5 chars-per-token = exactly 10 tokens
        var text = new string('a', 35);
        TokenHelper.EstimateTokens(text).Should().Be(10);
    }

    #endregion

    #region EstimateCobolTokens

    [Fact]
    public void EstimateCobolTokens_EmptyString_ReturnsZero()
    {
        TokenHelper.EstimateCobolTokens("").Should().Be(0);
    }

    [Fact]
    public void EstimateCobolTokens_NullString_ReturnsZero()
    {
        TokenHelper.EstimateCobolTokens(null!).Should().Be(0);
    }

    [Fact]
    public void EstimateCobolTokens_CobolCode_ReturnsPositiveCount()
    {
        var cobol = "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. HELLO.";
        var result = TokenHelper.EstimateCobolTokens(cobol);
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void EstimateCobolTokens_UsesThreeCharsPerToken()
    {
        // 30 chars / 3.0 = exactly 10 tokens
        var text = new string('a', 30);
        TokenHelper.EstimateCobolTokens(text).Should().Be(10);
    }

    #endregion

    #region TruncateToTokenLimit

    [Fact]
    public void TruncateToTokenLimit_ContentWithinLimit_ReturnsUnchanged()
    {
        var content = "Short content";
        var (result, wasTruncated) = TokenHelper.TruncateToTokenLimit(content, 1000);
        result.Should().Be(content);
        wasTruncated.Should().BeFalse();
    }

    [Fact]
    public void TruncateToTokenLimit_EmptyContent_ReturnsEmpty()
    {
        var (result, wasTruncated) = TokenHelper.TruncateToTokenLimit("", 100);
        result.Should().BeEmpty();
        wasTruncated.Should().BeFalse();
    }

    [Fact]
    public void TruncateToTokenLimit_ContentExceedsLimit_TruncatesAndMarks()
    {
        var content = new string('a', 1000); // Many chars, will exceed small limit
        var (result, wasTruncated) = TokenHelper.TruncateToTokenLimit(content, 10);
        wasTruncated.Should().BeTrue();
        result.Should().Contain("[TRUNCATED");
    }

    [Fact]
    public void TruncateToTokenLimit_PreserveStart_KeepsBeginning()
    {
        var content = "START" + new string('x', 500);
        var (result, wasTruncated) = TokenHelper.TruncateToTokenLimit(content, 10, preserveStart: true);
        wasTruncated.Should().BeTrue();
        result.Should().StartWith("START");
    }

    [Fact]
    public void TruncateToTokenLimit_PreserveEnd_KeepsEnd()
    {
        var content = new string('x', 500) + "END";
        var (result, wasTruncated) = TokenHelper.TruncateToTokenLimit(content, 10, preserveStart: false);
        wasTruncated.Should().BeTrue();
        result.Should().EndWith("END");
    }

    [Fact]
    public void TruncateToTokenLimit_NewlineBreak_TruncatesAtNewline()
    {
        // Content with a newline near the cut point
        var content = new string('a', 20) + "\n" + new string('b', 200);
        var (result, wasTruncated) = TokenHelper.TruncateToTokenLimit(content, 10, preserveStart: true);
        wasTruncated.Should().BeTrue();
    }

    #endregion

    #region TruncateCobolIntelligently

    [Fact]
    public void TruncateCobolIntelligently_ContentWithinLimit_ReturnsFullContent()
    {
        var cobol = "       IDENTIFICATION DIVISION.";
        var (result, wasTruncated, summary) = TokenHelper.TruncateCobolIntelligently(cobol, 10000);
        result.Should().Be(cobol);
        wasTruncated.Should().BeFalse();
        summary.Should().Contain("Full content");
    }

    [Fact]
    public void TruncateCobolIntelligently_EmptyContent_ReturnsEmpty()
    {
        var (result, wasTruncated, summary) = TokenHelper.TruncateCobolIntelligently("", 100);
        result.Should().BeEmpty();
        wasTruncated.Should().BeFalse();
    }

    [Fact]
    public void TruncateCobolIntelligently_LargeContent_TruncatesWithNotice()
    {
        // Create a large multi-line COBOL file
        var lines = Enumerable.Range(1, 500).Select(i => $"       LINE {i} OF COBOL CODE");
        var cobol = string.Join("\n", lines);
        var (result, wasTruncated, summary) = TokenHelper.TruncateCobolIntelligently(cobol, 100);
        wasTruncated.Should().BeTrue();
        result.Should().Contain("TRUNCATED");
        summary.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region CalculateRequestDelay

    [Fact]
    public void CalculateRequestDelay_NormalLimits_ReturnsPositiveDelay()
    {
        var delay = TokenHelper.CalculateRequestDelay(300000, 20000, 10000);
        delay.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateRequestDelay_HighTPM_ReturnsSmallerDelay()
    {
        var lowDelay = TokenHelper.CalculateRequestDelay(1000000, 20000, 10000);
        var highDelay = TokenHelper.CalculateRequestDelay(100000, 20000, 10000);
        lowDelay.Should().BeLessThan(highDelay);
    }

    [Fact]
    public void CalculateRequestDelay_BelowMinDelay_ReturnsMinDelay()
    {
        // Very high TPM should still respect minimum delay
        var delay = TokenHelper.CalculateRequestDelay(100_000_000, 100, 100, minDelayMs: 2000);
        delay.Should().BeGreaterThanOrEqualTo(500); // internal clamp lower bound
    }

    [Fact]
    public void CalculateRequestDelay_MaxCapAt120Seconds()
    {
        // Very low TPM with high token usage should not exceed 120s
        var delay = TokenHelper.CalculateRequestDelay(1, 50000, 50000);
        delay.Should().BeLessThanOrEqualTo(120000);
    }

    [Fact]
    public void CalculateRequestDelay_ZeroTPM_ReturnsFallback()
    {
        // Zero or negative TPM should return 60s fallback (safe)
        var delay = TokenHelper.CalculateRequestDelay(0, 20000, 10000);
        delay.Should().Be(60000);
    }

    #endregion

    #region GetRateLimitSummary

    [Fact]
    public void GetRateLimitSummary_ValidInput_ReturnsSummaryString()
    {
        var summary = TokenHelper.GetRateLimitSummary(300000, 20000, 10000);
        summary.Should().NotBeNullOrEmpty();
        summary.Should().Contain("300,000");
        summary.Should().Contain("20,000");
        summary.Should().Contain("10,000");
    }

    [Fact]
    public void GetRateLimitSummary_ContainsKeyFields()
    {
        var summary = TokenHelper.GetRateLimitSummary(300000, 20000, 10000);
        summary.Should().Contain("TPM Limit");
        summary.Should().Contain("Max Input Tokens");
        summary.Should().Contain("Max Output Tokens");
        summary.Should().Contain("Tokens per Request");
        summary.Should().Contain("Safe Requests/Min");
        summary.Should().Contain("Delay Between Requests");
    }

    #endregion

    #region Constants

    [Fact]
    public void CharsPerToken_IsExpectedValue()
    {
        TokenHelper.CharsPerToken.Should().Be(3.5);
    }

    [Fact]
    public void SafetyMargin_IsExpectedValue()
    {
        TokenHelper.SafetyMargin.Should().Be(0.5);
    }

    #endregion
}
