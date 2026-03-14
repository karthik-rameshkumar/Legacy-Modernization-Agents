using Xunit;
using FluentAssertions;
using CobolToQuarkusMigration.Chunking.Core;
using CobolToQuarkusMigration.Models;

namespace CobolToQuarkusMigration.Tests.Chunking;

public class NamingConventionEnforcerTests
{
    private static NamingConventionEnforcer CreateEnforcer(
        string classNamePrefix = "",
        string classNameSuffix = "")
    {
        var settings = new ConversionSettings
        {
            ClassNamePrefix = classNamePrefix,
            ClassNameSuffix = classNameSuffix
        };
        return new NamingConventionEnforcer(settings);
    }

    // ── ConvertNameDeterministic ──────────────────────────────────────────────

    [Fact]
    public void ConvertNameDeterministic_ClassName_ReturnsPascalCase()
    {
        var enforcer = CreateEnforcer();

        var result = enforcer.ConvertNameDeterministic(
            "VALIDATE-CUSTOMER-DATA", NameKind.ClassName, TargetLanguage.CSharp);

        result.Should().Be("ValidateCustomerData");
    }

    [Fact]
    public void ConvertNameDeterministic_MethodName_ReturnsCamelCase()
    {
        var enforcer = CreateEnforcer();

        var result = enforcer.ConvertNameDeterministic(
            "PROCESS-ORDER", NameKind.MethodName, TargetLanguage.CSharp);

        result.Should().Be("processOrder");
    }

    [Fact]
    public void ConvertNameDeterministic_PropertyName_ReturnsPascalCase()
    {
        var enforcer = CreateEnforcer();

        var result = enforcer.ConvertNameDeterministic(
            "CUSTOMER-ID", NameKind.PropertyName, TargetLanguage.CSharp);

        result.Should().Be("CustomerId");
    }

    [Fact]
    public void ConvertNameDeterministic_FieldName_ReturnsCamelCase()
    {
        var enforcer = CreateEnforcer();

        var result = enforcer.ConvertNameDeterministic(
            "ACCOUNT-NUMBER", NameKind.FieldName, TargetLanguage.Java);

        result.Should().Be("accountNumber");
    }

    [Fact]
    public void ConvertNameDeterministic_ParameterName_ReturnsCamelCase()
    {
        var enforcer = CreateEnforcer();

        var result = enforcer.ConvertNameDeterministic(
            "INPUT-VALUE", NameKind.ParameterName, TargetLanguage.CSharp);

        result.Should().Be("inputValue");
    }

    [Fact]
    public void ConvertNameDeterministic_ConstantName_ReturnsUpperSnakeCase()
    {
        var enforcer = CreateEnforcer();

        var result = enforcer.ConvertNameDeterministic(
            "MAX-RETRY-COUNT", NameKind.ConstantName, TargetLanguage.CSharp);

        result.Should().Be("MAX_RETRY_COUNT");
    }

    [Fact]
    public void ConvertNameDeterministic_EnumMemberName_ReturnsPascalCase()
    {
        var enforcer = CreateEnforcer();

        var result = enforcer.ConvertNameDeterministic(
            "STATUS-ACTIVE", NameKind.EnumMemberName, TargetLanguage.CSharp);

        result.Should().Be("StatusActive");
    }

    [Fact]
    public void ConvertNameDeterministic_UnknownNameKind_ReturnsCamelCase()
    {
        var enforcer = CreateEnforcer();

        // Use a cast to an undefined enum value to hit the default branch
        var result = enforcer.ConvertNameDeterministic(
            "SOME-VALUE", (NameKind)999, TargetLanguage.CSharp);

        result.Should().Be("someValue");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConvertNameDeterministic_NullOrWhitespace_ReturnsEmpty(string input)
    {
        var enforcer = CreateEnforcer();

        var result = enforcer.ConvertNameDeterministic(input, NameKind.ClassName, TargetLanguage.CSharp);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ConvertNameDeterministic_WithClassNameSuffix_AppendsSuffix()
    {
        var enforcer = CreateEnforcer(classNameSuffix: "Service");

        var result = enforcer.ConvertNameDeterministic(
            "CUSTOMER-DATA", NameKind.ClassName, TargetLanguage.CSharp);

        result.Should().Be("CustomerDataService");
    }

    [Fact]
    public void ConvertNameDeterministic_WithClassNamePrefix_PrependsPrefix()
    {
        var enforcer = CreateEnforcer(classNamePrefix: "Legacy");

        var result = enforcer.ConvertNameDeterministic(
            "CUSTOMER", NameKind.ClassName, TargetLanguage.CSharp);

        result.Should().Be("LegacyCustomer");
    }

    [Fact]
    public void ConvertNameDeterministic_WithPrefixAndSuffix_AppliesBoth()
    {
        var enforcer = CreateEnforcer(classNamePrefix: "Legacy", classNameSuffix: "Service");

        var result = enforcer.ConvertNameDeterministic(
            "ORDER", NameKind.ClassName, TargetLanguage.CSharp);

        result.Should().Be("LegacyOrderService");
    }

    // ── COBOL prefix stripping ────────────────────────────────────────────────

    [Theory]
    [InlineData("WS-CUSTOMER-DATA", "CustomerData")]
    [InlineData("LS-ORDER-ID", "OrderId")]
    [InlineData("WK-TEMP-VALUE", "TempValue")]
    [InlineData("LK-INPUT-PARAM", "InputParam")]
    [InlineData("FD-FILE-NAME", "FileName")]
    [InlineData("SD-SORT-KEY", "SortKey")]
    public void ConvertNameDeterministic_StripsKnownCobolPrefixes(string input, string expected)
    {
        var enforcer = CreateEnforcer();

        var result = enforcer.ConvertNameDeterministic(input, NameKind.PropertyName, TargetLanguage.CSharp);

        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertNameDeterministic_UnknownPrefix_IsNotStripped()
    {
        var enforcer = CreateEnforcer();

        var result = enforcer.ConvertNameDeterministic("XX-SOME-VALUE", NameKind.PropertyName, TargetLanguage.CSharp);

        result.Should().Be("XxSomeValue");
    }

    // ── Reserved word escaping ────────────────────────────────────────────────

    [Fact]
    public void ConvertNameDeterministic_CSharpReservedWord_GetsAtPrefix()
    {
        var enforcer = CreateEnforcer();

        // COBOL name "CLASS" → PascalCase → "Class" which is a C# keyword
        var result = enforcer.ConvertNameDeterministic("CLASS", NameKind.ClassName, TargetLanguage.CSharp);

        result.Should().Be("@Class");
    }

    [Fact]
    public void ConvertNameDeterministic_JavaReservedWord_GetsUnderscoreSuffix()
    {
        var enforcer = CreateEnforcer();

        // COBOL name "CLASS" → PascalCase → "Class" which is a Java keyword
        var result = enforcer.ConvertNameDeterministic("CLASS", NameKind.ClassName, TargetLanguage.Java);

        result.Should().Be("Class_");
    }

    [Fact]
    public void ConvertNameDeterministic_NonReservedWord_NoEscaping()
    {
        var enforcer = CreateEnforcer();

        var result = enforcer.ConvertNameDeterministic("CUSTOMER", NameKind.ClassName, TargetLanguage.CSharp);

        result.Should().Be("Customer");
    }

    // ── ValidateName ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("CustomerService", NameKind.ClassName, true)]
    [InlineData("customerService", NameKind.ClassName, false)]
    [InlineData("Customer123", NameKind.ClassName, true)]
    [InlineData("123Customer", NameKind.ClassName, false)]
    public void ValidateName_ClassName_ValidatesCorrectly(string name, NameKind kind, bool expected)
    {
        var enforcer = CreateEnforcer();

        enforcer.ValidateName(name, kind).Should().Be(expected);
    }

    [Theory]
    [InlineData("processOrder", NameKind.MethodName, true)]
    [InlineData("ProcessOrder", NameKind.MethodName, false)]
    [InlineData("process123", NameKind.MethodName, true)]
    public void ValidateName_MethodName_ValidatesCorrectly(string name, NameKind kind, bool expected)
    {
        var enforcer = CreateEnforcer();

        enforcer.ValidateName(name, kind).Should().Be(expected);
    }

    [Theory]
    [InlineData("CustomerId", NameKind.PropertyName, true)]
    [InlineData("customerId", NameKind.PropertyName, false)]
    public void ValidateName_PropertyName_ValidatesCorrectly(string name, NameKind kind, bool expected)
    {
        var enforcer = CreateEnforcer();

        enforcer.ValidateName(name, kind).Should().Be(expected);
    }

    [Theory]
    [InlineData("accountNumber", NameKind.FieldName, true)]
    [InlineData("AccountNumber", NameKind.FieldName, false)]
    public void ValidateName_FieldName_ValidatesCorrectly(string name, NameKind kind, bool expected)
    {
        var enforcer = CreateEnforcer();

        enforcer.ValidateName(name, kind).Should().Be(expected);
    }

    [Theory]
    [InlineData("inputValue", NameKind.ParameterName, true)]
    [InlineData("InputValue", NameKind.ParameterName, false)]
    public void ValidateName_ParameterName_ValidatesCorrectly(string name, NameKind kind, bool expected)
    {
        var enforcer = CreateEnforcer();

        enforcer.ValidateName(name, kind).Should().Be(expected);
    }

    [Theory]
    [InlineData("MAX_RETRY", NameKind.ConstantName, true)]
    [InlineData("maxRetry", NameKind.ConstantName, false)]
    [InlineData("MAX_123", NameKind.ConstantName, true)]
    public void ValidateName_ConstantName_ValidatesCorrectly(string name, NameKind kind, bool expected)
    {
        var enforcer = CreateEnforcer();

        enforcer.ValidateName(name, kind).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateName_EmptyOrWhitespace_ReturnsFalse(string name)
    {
        var enforcer = CreateEnforcer();

        enforcer.ValidateName(name, NameKind.ClassName).Should().BeFalse();
    }

    [Fact]
    public void ValidateName_UnknownNameKind_ReturnsTrue()
    {
        var enforcer = CreateEnforcer();

        // Unknown NameKind hits the default case which returns true
        enforcer.ValidateName("anything", (NameKind)999).Should().BeTrue();
    }

    // ── SuggestCorrectedName ──────────────────────────────────────────────────

    [Fact]
    public void SuggestCorrectedName_InvalidCobolName_ReturnsCorrectedName()
    {
        var enforcer = CreateEnforcer();

        var result = enforcer.SuggestCorrectedName(
            "VALIDATE-CUSTOMER-DATA", NameKind.MethodName, TargetLanguage.CSharp);

        result.Should().Be("validateCustomerData");
    }

    [Fact]
    public void SuggestCorrectedName_IsEquivalentToConvertNameDeterministic()
    {
        var enforcer = CreateEnforcer();
        const string name = "PROCESS-ORDER-ITEMS";

        var suggested = enforcer.SuggestCorrectedName(name, NameKind.ClassName, TargetLanguage.Java);
        var converted = enforcer.ConvertNameDeterministic(name, NameKind.ClassName, TargetLanguage.Java);

        suggested.Should().Be(converted);
    }

    // ── Underscore separator support ──────────────────────────────────────────

    [Fact]
    public void ConvertNameDeterministic_UnderscoreSeparators_AreSplitCorrectly()
    {
        var enforcer = CreateEnforcer();

        var result = enforcer.ConvertNameDeterministic(
            "VALIDATE_CUSTOMER_DATA", NameKind.ClassName, TargetLanguage.CSharp);

        result.Should().Be("ValidateCustomerData");
    }

    [Fact]
    public void ConvertNameDeterministic_SingleWordName_ReturnsCorrectCase()
    {
        var enforcer = CreateEnforcer();

        var result = enforcer.ConvertNameDeterministic("CUSTOMER", NameKind.MethodName, TargetLanguage.CSharp);

        result.Should().Be("customer");
    }
}
