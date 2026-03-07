using Xunit;
using FluentAssertions;
using CobolToQuarkusMigration.Helpers;

namespace CobolToQuarkusMigration.Tests.Helpers;

public class NamingHelperTests
{
    #region ToPascalCase

    [Fact]
    public void ToPascalCase_WithUnderscoreSeparatedWords_ReturnsPascalCase()
    {
        var result = NamingHelper.ToPascalCase("my_var");
        result.Should().Be("MyVar");
    }

    [Fact]
    public void ToPascalCase_WithHyphenSeparatedWords_ReturnsPascalCase()
    {
        var result = NamingHelper.ToPascalCase("ABC-DEF");
        result.Should().Be("AbcDef");
    }

    [Fact]
    public void ToPascalCase_WithNullInput_ReturnsEmpty()
    {
        var result = NamingHelper.ToPascalCase(null!);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToPascalCase_WithEmptyInput_ReturnsEmpty()
    {
        var result = NamingHelper.ToPascalCase(string.Empty);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToPascalCase_WithWhitespaceOnly_ReturnsEmpty()
    {
        var result = NamingHelper.ToPascalCase("   ");
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToPascalCase_WithSingleWord_CapitalizesFirstLetter()
    {
        var result = NamingHelper.ToPascalCase("hello");
        result.Should().Be("Hello");
    }

    [Fact]
    public void ToPascalCase_WithAllUppercase_ReturnsCapitalizedFirst()
    {
        var result = NamingHelper.ToPascalCase("HELLO");
        result.Should().Be("Hello");
    }

    [Fact]
    public void ToPascalCase_WithDigits_PreservesDigits()
    {
        var result = NamingHelper.ToPascalCase("var_123");
        result.Should().Be("Var123");
    }

    #endregion

    #region DeriveClassNameFromCobolFile

    [Fact]
    public void DeriveClassNameFromCobolFile_WithUpperCaseCobolName_ReturnsPascalCase()
    {
        var result = NamingHelper.DeriveClassNameFromCobolFile("RGNB649.cbl");
        result.Should().Be("Rgnb649");
    }

    [Fact]
    public void DeriveClassNameFromCobolFile_WithUnderscoreFileName_ReturnsPascalCase()
    {
        var result = NamingHelper.DeriveClassNameFromCobolFile("synthetic_50k_loc_cobol.cbl");
        result.Should().Be("Synthetic50kLocCobol");
    }

    [Fact]
    public void DeriveClassNameFromCobolFile_WithFullPath_UsesFileNameOnly()
    {
        var result = NamingHelper.DeriveClassNameFromCobolFile("/some/path/PAYMENT.cbl");
        result.Should().Be("Payment");
    }

    [Fact]
    public void DeriveClassNameFromCobolFile_WithNullInput_ReturnsDefault()
    {
        var result = NamingHelper.DeriveClassNameFromCobolFile(null!);
        result.Should().Be("ConvertedCobolProgram");
    }

    [Fact]
    public void DeriveClassNameFromCobolFile_WithEmptyInput_ReturnsDefault()
    {
        var result = NamingHelper.DeriveClassNameFromCobolFile(string.Empty);
        result.Should().Be("ConvertedCobolProgram");
    }

    [Fact]
    public void DeriveClassNameFromCobolFile_WithDigitStart_PrefixesWithCobol()
    {
        // Extension-less name that starts with a digit
        var result = NamingHelper.DeriveClassNameFromCobolFile("123program.cbl");
        result.Should().StartWith("Cobol");
    }

    [Fact]
    public void DeriveClassNameFromCobolFile_WithHyphenatedName_ReturnsPascalCase()
    {
        var result = NamingHelper.DeriveClassNameFromCobolFile("VALIDATE-CUSTOMER.cbl");
        result.Should().Be("ValidateCustomer");
    }

    #endregion

    #region GetFallbackClassName

    [Fact]
    public void GetFallbackClassName_AppendsRallbackSuffix()
    {
        var result = NamingHelper.GetFallbackClassName("RGNB649.cbl");
        result.Should().Be("Rgnb649Fallback");
    }

    #endregion

    #region GetOutputFileName

    [Fact]
    public void GetOutputFileName_WithCsharpExtension_ReturnsCorrectFileName()
    {
        var result = NamingHelper.GetOutputFileName("RGNB649.cbl", ".cs");
        result.Should().Be("Rgnb649.cs");
    }

    [Fact]
    public void GetOutputFileName_WithJavaExtension_ReturnsCorrectFileName()
    {
        var result = NamingHelper.GetOutputFileName("PAYMENT-PROCESS.cbl", ".java");
        result.Should().Be("PaymentProcess.java");
    }

    #endregion

    #region IsValidIdentifier

    [Theory]
    [InlineData("MyClass", true)]
    [InlineData("_privateField", true)]
    [InlineData("ClassName123", true)]
    [InlineData("123Invalid", false)]
    [InlineData("has-hyphen", false)]
    [InlineData("has space", false)]
    [InlineData("", false)]
    public void IsValidIdentifier_VariousInputs_ReturnsExpected(string identifier, bool expected)
    {
        var result = NamingHelper.IsValidIdentifier(identifier);
        result.Should().Be(expected);
    }

    [Fact]
    public void IsValidIdentifier_WithNullInput_ReturnsFalse()
    {
        var result = NamingHelper.IsValidIdentifier(null!);
        result.Should().BeFalse();
    }

    #endregion

    #region IsSemanticClassName

    [Theory]
    [InlineData("PaymentBatchValidator", true)]
    [InlineData("CustomerAccountProcessor", true)]
    [InlineData("DataExporter", true)]
    [InlineData("ConvertedCobolProgram", false)]
    [InlineData("Program", false)]
    [InlineData("Main", false)]
    [InlineData("Rgnb649", false)] // short with digit
    [InlineData("", false)]
    public void IsSemanticClassName_VariousInputs_ReturnsExpected(string className, bool expected)
    {
        var result = NamingHelper.IsSemanticClassName(className);
        result.Should().Be(expected);
    }

    [Fact]
    public void IsSemanticClassName_WithNullInput_ReturnsFalse()
    {
        var result = NamingHelper.IsSemanticClassName(null!);
        result.Should().BeFalse();
    }

    #endregion

    #region ExtractCSharpClassName

    [Fact]
    public void ExtractCSharpClassName_WithClassDeclaration_ExtractsName()
    {
        var code = "public class PaymentProcessor : IProcessor\n{\n}";
        var result = NamingHelper.ExtractCSharpClassName(code, "PAYMENT.cbl");
        result.Should().Be("PaymentProcessor");
    }

    [Fact]
    public void ExtractCSharpClassName_WithNoClassDeclaration_FallsBackToFileName()
    {
        var code = "// this is a comment with no declaration";
        var result = NamingHelper.ExtractCSharpClassName(code, "RGNB649.cbl");
        result.Should().Be("Rgnb649");
    }

    [Fact]
    public void ExtractCSharpClassName_WithGenericClassName_FallsBackToFileName()
    {
        var code = "public class ConvertedCobolProgram\n{\n}";
        var result = NamingHelper.ExtractCSharpClassName(code, "RGNB649.cbl");
        result.Should().Be("Rgnb649");
    }

    #endregion

    #region ExtractJavaClassName

    [Fact]
    public void ExtractJavaClassName_WithPublicClassDeclaration_ExtractsName()
    {
        var code = "public class AccountService {\n}";
        var result = NamingHelper.ExtractJavaClassName(code, "ACCOUNT.cbl");
        result.Should().Be("AccountService");
    }

    [Fact]
    public void ExtractJavaClassName_WithNoClassDeclaration_FallsBackToFileName()
    {
        var code = "// this is a comment with no declaration";
        var result = NamingHelper.ExtractJavaClassName(code, "RGNB649.cbl");
        result.Should().Be("Rgnb649");
    }

    #endregion

    #region ReplaceGenericClassName

    [Fact]
    public void ReplaceGenericClassName_ReplacesClassDeclaration()
    {
        var code = "public class ConvertedCobolProgram { }";
        var result = NamingHelper.ReplaceGenericClassName(code, "ConvertedCobolProgram", "PaymentProcessor");
        result.Should().Contain("class PaymentProcessor");
    }

    [Fact]
    public void ReplaceGenericClassName_WhenSameName_ReturnsSameCode()
    {
        var code = "public class MyClass { }";
        var result = NamingHelper.ReplaceGenericClassName(code, "MyClass", "MyClass");
        result.Should().Be(code);
    }

    [Fact]
    public void ReplaceGenericClassName_ReplacesConstructorCall()
    {
        var code = "var obj = new ConvertedCobolProgram();";
        var result = NamingHelper.ReplaceGenericClassName(code, "ConvertedCobolProgram", "PaymentProcessor");
        result.Should().Contain("new PaymentProcessor");
    }

    #endregion
}
