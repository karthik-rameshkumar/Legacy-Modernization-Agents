using Xunit;
using FluentAssertions;
using CobolToQuarkusMigration.Helpers;

namespace CobolToQuarkusMigration.Tests.Helpers;

public class TokenHelperTests
{
    #region EstimateTokens

    [Fact]
    public void EstimateTokens_WithEmptyString_ReturnsZero()
    {
        var result = TokenHelper.EstimateTokens(string.Empty);
        result.Should().Be(0);
    }

    [Fact]
    public void EstimateTokens_WithNullString_ReturnsZero()
    {
        var result = TokenHelper.EstimateTokens(null!);
        result.Should().Be(0);
    }

    [Fact]
    public void EstimateTokens_WithKnownText_ReturnsExpectedEstimate()
    {
        // "hello" = 5 chars / 3.5 = 1.43 -> ceil -> 2 tokens
        var result = TokenHelper.EstimateTokens("hello");
        result.Should().Be(2);
    }

    [Fact]
    public void EstimateTokens_WithLongerText_ScalesWithLength()
    {
        var shortResult = TokenHelper.EstimateTokens("short");
        var longResult = TokenHelper.EstimateTokens("this is a much longer text that has more characters");
        longResult.Should().BeGreaterThan(shortResult);
    }

    #endregion

    #region EstimateCobolTokens

    [Fact]
    public void EstimateCobolTokens_WithEmptyString_ReturnsZero()
    {
        var result = TokenHelper.EstimateCobolTokens(string.Empty);
        result.Should().Be(0);
    }

    [Fact]
    public void EstimateCobolTokens_WithNullString_ReturnsZero()
    {
        var result = TokenHelper.EstimateCobolTokens(null!);
        result.Should().Be(0);
    }

    [Fact]
    public void EstimateCobolTokens_WithCode_ReturnsPositiveCount()
    {
        var cobol = "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. MYPROG.";
        var result = TokenHelper.EstimateCobolTokens(cobol);
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void EstimateCobolTokens_UsesHigherDensity_ThanGeneralEstimate()
    {
        // COBOL uses 3.0 chars/token vs 3.5 for general text → more tokens for same content
        var text = "IDENTIFICATION DIVISION PROGRAM-ID MYPROG";
        var cobolTokens = TokenHelper.EstimateCobolTokens(text);
        var generalTokens = TokenHelper.EstimateTokens(text);
        cobolTokens.Should().BeGreaterThanOrEqualTo(generalTokens);
    }

    #endregion

    #region TruncateToTokenLimit

    [Fact]
    public void TruncateToTokenLimit_WithEmptyContent_ReturnsSameContent()
    {
        var (content, wasTruncated) = TokenHelper.TruncateToTokenLimit(string.Empty, 100);
        content.Should().Be(string.Empty);
        wasTruncated.Should().BeFalse();
    }

    [Fact]
    public void TruncateToTokenLimit_WithContentUnderLimit_ReturnsUnchanged()
    {
        var text = "Short text";
        var (content, wasTruncated) = TokenHelper.TruncateToTokenLimit(text, 1000);
        content.Should().Be(text);
        wasTruncated.Should().BeFalse();
    }

    [Fact]
    public void TruncateToTokenLimit_WithContentOverLimit_TruncatesAndMarksFlag()
    {
        // Create content that is definitely over limit
        var largeText = new string('A', 10000);
        var (content, wasTruncated) = TokenHelper.TruncateToTokenLimit(largeText, 10);
        wasTruncated.Should().BeTrue();
        content.Should().Contain("[TRUNCATED");
    }

    [Fact]
    public void TruncateToTokenLimit_WithPreserveStartTrue_KeepsBeginning()
    {
        // Use enough tokens to preserve the marker at the start (100 tokens ≈ 350 chars)
        var largeText = "STARTMARKER" + new string('X', 10000);
        var (content, wasTruncated) = TokenHelper.TruncateToTokenLimit(largeText, 100);
        wasTruncated.Should().BeTrue();
        content.Should().StartWith("STARTMARKER");
    }

    [Fact]
    public void TruncateToTokenLimit_WithPreserveStartFalse_KeepsEnd()
    {
        var largeText = new string('X', 10000) + "\nENDING";
        var (content, wasTruncated) = TokenHelper.TruncateToTokenLimit(largeText, 5, preserveStart: false);
        wasTruncated.Should().BeTrue();
        content.Should().Contain("ENDING");
    }

    #endregion

    #region TruncateCobolIntelligently

    [Fact]
    public void TruncateCobolIntelligently_WithEmptyContent_ReturnsEmptyUnchanged()
    {
        var (content, wasTruncated, summary) = TokenHelper.TruncateCobolIntelligently(string.Empty, 100);
        content.Should().Be(string.Empty);
        wasTruncated.Should().BeFalse();
        summary.Should().Be("Empty content");
    }

    [Fact]
    public void TruncateCobolIntelligently_WithContentUnderLimit_ReturnsFull()
    {
        var cobol = "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. MYPROG.";
        var (content, wasTruncated, _) = TokenHelper.TruncateCobolIntelligently(cobol, 10000);
        wasTruncated.Should().BeFalse();
        content.Should().Be(cobol);
    }

    [Fact]
    public void TruncateCobolIntelligently_WithLargeContent_TruncatesAndAddsNotice()
    {
        // Create a large COBOL file (many lines)
        var lines = Enumerable.Range(1, 500).Select(i => $"       LINE-{i:D3}. MOVE 0 TO WS-VAR-{i:D3}.");
        var cobol = string.Join("\n", lines);
        var (content, wasTruncated, summary) = TokenHelper.TruncateCobolIntelligently(cobol, 100);
        wasTruncated.Should().BeTrue();
        content.Should().Contain("TRUNCATED");
        summary.Should().Contain("Truncated");
    }

    #endregion

    #region CalculateRequestDelay

    [Fact]
    public void CalculateRequestDelay_WithZeroTpm_ReturnsMaxDelay()
    {
        // Zero TPM → safeRequestsPerMinute = 0, should return 60000
        var result = TokenHelper.CalculateRequestDelay(0, 1000, 1000);
        result.Should().Be(60000);
    }

    [Fact]
    public void CalculateRequestDelay_WithHighTpm_ReturnsLowDelay()
    {
        // 300K TPM with 1K input + 1K output = plenty of headroom
        var result = TokenHelper.CalculateRequestDelay(300000, 500, 500);
        result.Should().BeLessThan(10000);
    }

    [Fact]
    public void CalculateRequestDelay_WithLowTpm_ReturnsHigherDelay()
    {
        var lowTpmResult = TokenHelper.CalculateRequestDelay(10000, 5000, 5000);
        var highTpmResult = TokenHelper.CalculateRequestDelay(300000, 5000, 5000);
        lowTpmResult.Should().BeGreaterThan(highTpmResult);
    }

    [Fact]
    public void CalculateRequestDelay_RespectsMinimumDelay()
    {
        // Even with very high TPM, minimum floor should be respected
        var result = TokenHelper.CalculateRequestDelay(10_000_000, 1, 1, minDelayMs: 3000);
        result.Should().BeGreaterThanOrEqualTo(500); // absolute min
    }

    [Fact]
    public void CalculateRequestDelay_NeverExceedsMaximum()
    {
        // Very low TPM should be capped at 120 seconds
        var result = TokenHelper.CalculateRequestDelay(1, 100000, 100000);
        result.Should().BeLessThanOrEqualTo(120000);
    }

    #endregion

    #region GetRateLimitSummary

    [Fact]
    public void GetRateLimitSummary_ReturnsNonEmptyString()
    {
        var result = TokenHelper.GetRateLimitSummary(300000, 20000, 10000);
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetRateLimitSummary_ContainsKeyMetrics()
    {
        var result = TokenHelper.GetRateLimitSummary(300000, 20000, 10000);
        result.Should().Contain("300,000");
        result.Should().Contain("20,000");
        result.Should().Contain("10,000");
    }

    #endregion
}
