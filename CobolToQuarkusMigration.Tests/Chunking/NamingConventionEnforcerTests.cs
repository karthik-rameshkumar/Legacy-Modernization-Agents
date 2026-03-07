using Xunit;
using FluentAssertions;
using CobolToQuarkusMigration.Chunking.Core;
using CobolToQuarkusMigration.Models;

namespace CobolToQuarkusMigration.Tests.Chunking;

public class NamingConventionEnforcerTests
{
    private readonly ConversionSettings _defaultSettings;
    private readonly NamingConventionEnforcer _enforcer;

    public NamingConventionEnforcerTests()
    {
        _defaultSettings = new ConversionSettings
        {
            ClassNamePrefix = string.Empty,
            ClassNameSuffix = string.Empty
        };
        _enforcer = new NamingConventionEnforcer(_defaultSettings);
    }

    #region ConvertNameDeterministic - ClassName

    [Fact]
    public void ConvertNameDeterministic_ClassName_ConvertsToPascalCase()
    {
        var result = _enforcer.ConvertNameDeterministic("VALIDATE-CUSTOMER", NameKind.ClassName, TargetLanguage.CSharp);
        result.Should().Be("ValidateCustomer");
    }

    [Fact]
    public void ConvertNameDeterministic_ClassName_WithSuffix_AppendsSuffix()
    {
        var settings = new ConversionSettings { ClassNameSuffix = "Service" };
        var enforcer = new NamingConventionEnforcer(settings);
        var result = enforcer.ConvertNameDeterministic("PAYMENT", NameKind.ClassName, TargetLanguage.CSharp);
        result.Should().Be("PaymentService");
    }

    [Fact]
    public void ConvertNameDeterministic_ClassName_WithPrefix_PrependPrefix()
    {
        var settings = new ConversionSettings { ClassNamePrefix = "Legacy" };
        var enforcer = new NamingConventionEnforcer(settings);
        var result = enforcer.ConvertNameDeterministic("PAYMENT", NameKind.ClassName, TargetLanguage.CSharp);
        result.Should().Be("LegacyPayment");
    }

    [Fact]
    public void ConvertNameDeterministic_ClassName_StripsWorkingStoragePrefix()
    {
        // WS- prefix should be stripped
        var result = _enforcer.ConvertNameDeterministic("WS-CUSTOMER-NAME", NameKind.ClassName, TargetLanguage.CSharp);
        result.Should().Be("CustomerName");
    }

    [Fact]
    public void ConvertNameDeterministic_ClassName_StripsLinkageSectionPrefix()
    {
        var result = _enforcer.ConvertNameDeterministic("LS-ACCOUNT-ID", NameKind.ClassName, TargetLanguage.CSharp);
        result.Should().Be("AccountId");
    }

    #endregion

    #region ConvertNameDeterministic - MethodName

    [Fact]
    public void ConvertNameDeterministic_MethodName_ConvertsToCamelCase()
    {
        var result = _enforcer.ConvertNameDeterministic("VALIDATE-CUSTOMER-DATA", NameKind.MethodName, TargetLanguage.CSharp);
        result.Should().Be("validateCustomerData");
    }

    [Fact]
    public void ConvertNameDeterministic_MethodName_SingleWord_LowerCase()
    {
        var result = _enforcer.ConvertNameDeterministic("PROCESS", NameKind.MethodName, TargetLanguage.CSharp);
        result.Should().Be("process");
    }

    #endregion

    #region ConvertNameDeterministic - PropertyName

    [Fact]
    public void ConvertNameDeterministic_PropertyName_ConvertsToPascalCase()
    {
        var result = _enforcer.ConvertNameDeterministic("ACCOUNT-BALANCE", NameKind.PropertyName, TargetLanguage.CSharp);
        result.Should().Be("AccountBalance");
    }

    #endregion

    #region ConvertNameDeterministic - ConstantName

    [Fact]
    public void ConvertNameDeterministic_ConstantName_ConvertsToUpperSnakeCase()
    {
        var result = _enforcer.ConvertNameDeterministic("MAX-RETRY-COUNT", NameKind.ConstantName, TargetLanguage.CSharp);
        result.Should().Be("MAX_RETRY_COUNT");
    }

    #endregion

    #region ConvertNameDeterministic - Reserved Words

    [Fact]
    public void ConvertNameDeterministic_CSharpReservedWord_EscapesWithAtPrefix()
    {
        // "CLASS" as a COBOL name should become "@Class" in C#
        var result = _enforcer.ConvertNameDeterministic("CLASS", NameKind.ClassName, TargetLanguage.CSharp);
        result.Should().StartWith("@");
    }

    [Fact]
    public void ConvertNameDeterministic_JavaReservedWord_EscapesWithUnderscore()
    {
        // "CLASS" as a COBOL name should become "Class_" in Java
        var result = _enforcer.ConvertNameDeterministic("CLASS", NameKind.ClassName, TargetLanguage.Java);
        result.Should().EndWith("_");
    }

    [Fact]
    public void ConvertNameDeterministic_NonReservedWord_NotEscaped()
    {
        var result = _enforcer.ConvertNameDeterministic("PAYMENT", NameKind.ClassName, TargetLanguage.CSharp);
        result.Should().NotStartWith("@");
        result.Should().NotEndWith("_");
    }

    #endregion

    #region ConvertNameDeterministic - Edge Cases

    [Fact]
    public void ConvertNameDeterministic_WithEmptyInput_ReturnsEmpty()
    {
        var result = _enforcer.ConvertNameDeterministic(string.Empty, NameKind.MethodName, TargetLanguage.CSharp);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ConvertNameDeterministic_WithNullInput_ReturnsEmpty()
    {
        var result = _enforcer.ConvertNameDeterministic(null!, NameKind.MethodName, TargetLanguage.CSharp);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ConvertNameDeterministic_WithUnderscoreSeparators_ConvertsCorrectly()
    {
        var result = _enforcer.ConvertNameDeterministic("WK_TOTAL_AMOUNT", NameKind.PropertyName, TargetLanguage.CSharp);
        // WK- prefix stripping only applies to hyphen-prefixed WK-, not underscore
        result.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region ValidateName

    [Theory]
    [InlineData("MyClass", NameKind.ClassName, true)]
    [InlineData("myClass", NameKind.ClassName, false)]
    [InlineData("123Class", NameKind.ClassName, false)]
    [InlineData("myMethod", NameKind.MethodName, true)]
    [InlineData("MyMethod", NameKind.MethodName, false)]
    [InlineData("MY_CONSTANT", NameKind.ConstantName, true)]
    [InlineData("myConstant", NameKind.ConstantName, false)]
    [InlineData("MyProperty", NameKind.PropertyName, true)]
    [InlineData("myParam", NameKind.ParameterName, true)]
    [InlineData("MyParam", NameKind.ParameterName, false)]
    public void ValidateName_VariousNamesAndTypes_ReturnsExpected(string name, NameKind kind, bool expected)
    {
        var result = _enforcer.ValidateName(name, kind);
        result.Should().Be(expected);
    }

    [Fact]
    public void ValidateName_WithEmptyName_ReturnsFalse()
    {
        var result = _enforcer.ValidateName(string.Empty, NameKind.ClassName);
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateName_WithNullName_ReturnsFalse()
    {
        var result = _enforcer.ValidateName(null!, NameKind.ClassName);
        result.Should().BeFalse();
    }

    #endregion

    #region SuggestCorrectedName

    [Fact]
    public void SuggestCorrectedName_ForInvalidClassName_ReturnsValidPascalCase()
    {
        // An invalid class name (e.g. lowercase) should be corrected
        var result = _enforcer.SuggestCorrectedName("VALIDATE-ACCOUNT", NameKind.ClassName, TargetLanguage.CSharp);
        result.Should().Be("ValidateAccount");
        _enforcer.ValidateName(result, NameKind.ClassName).Should().BeTrue();
    }

    [Fact]
    public void SuggestCorrectedName_ForInvalidMethodName_ReturnsValidCamelCase()
    {
        var result = _enforcer.SuggestCorrectedName("PROCESS-PAYMENT", NameKind.MethodName, TargetLanguage.CSharp);
        result.Should().Be("processPayment");
        _enforcer.ValidateName(result, NameKind.MethodName).Should().BeTrue();
    }

    #endregion
}
