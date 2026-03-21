using Xunit;
using FluentAssertions;
using CobolToQuarkusMigration.Helpers;

namespace CobolToQuarkusMigration.Tests.Helpers;

public class NamingHelperTests
{
    #region ToPascalCase

    [Theory]
    [InlineData("my_var", "MyVar")]
    [InlineData("ABC-DEF", "AbcDef")]
    [InlineData("hello", "Hello")]
    [InlineData("HELLO_WORLD", "HelloWorld")]
    [InlineData("a", "A")]
    [InlineData("abc123", "Abc123")]
    public void ToPascalCase_ValidInput_ReturnsExpectedResult(string input, string expected)
    {
        NamingHelper.ToPascalCase(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void ToPascalCase_EmptyOrWhitespace_ReturnsEmpty(string input)
    {
        NamingHelper.ToPascalCase(input).Should().BeEmpty();
    }

    [Fact]
    public void ToPascalCase_SpecialCharactersOnly_ReturnsEmpty()
    {
        NamingHelper.ToPascalCase("---").Should().BeEmpty();
    }

    [Fact]
    public void ToPascalCase_MixedSeparators_CapitalizesAfterEach()
    {
        NamingHelper.ToPascalCase("a-b_c").Should().Be("ABC");
    }

    #endregion

    #region DeriveClassNameFromCobolFile

    [Theory]
    [InlineData("RGNB649.cbl", "Rgnb649")]
    [InlineData("synthetic_50k_loc_cobol.cbl", "Synthetic50kLocCobol")]
    [InlineData("MY-PROGRAM.CBL", "MyProgram")]
    [InlineData("customer_processor.cob", "CustomerProcessor")]
    public void DeriveClassNameFromCobolFile_ValidFileName_ReturnsExpectedClassName(string fileName, string expected)
    {
        NamingHelper.DeriveClassNameFromCobolFile(fileName).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void DeriveClassNameFromCobolFile_EmptyOrWhitespace_ReturnsFallback(string fileName)
    {
        NamingHelper.DeriveClassNameFromCobolFile(fileName).Should().Be("ConvertedCobolProgram");
    }

    [Fact]
    public void DeriveClassNameFromCobolFile_FileStartsWithDigit_PrefixedWithCobol()
    {
        // A file that produces a name starting with a digit gets "Cobol" prefix
        var result = NamingHelper.DeriveClassNameFromCobolFile("123prog.cbl");
        result.Should().StartWith("Cobol");
    }

    [Fact]
    public void DeriveClassNameFromCobolFile_FullPath_ExtractsFilenameOnly()
    {
        // "MyProgram" has no separators so it converts to "Myprogram" (only first char capitalized)
        var result = NamingHelper.DeriveClassNameFromCobolFile("/some/path/MyProgram.cbl");
        result.Should().Be("Myprogram");
    }

    [Fact]
    public void DeriveClassNameFromCobolFile_NoExtension_UsesFullName()
    {
        // "MyProgram" has no separators so it converts to "Myprogram" (only first char capitalized)
        var result = NamingHelper.DeriveClassNameFromCobolFile("MyProgram");
        result.Should().Be("Myprogram");
    }

    #endregion

    #region GetFallbackClassName

    [Fact]
    public void GetFallbackClassName_ValidFile_ReturnsClassNameWithFallbackSuffix()
    {
        var result = NamingHelper.GetFallbackClassName("RGNB649.cbl");
        result.Should().Be("Rgnb649Fallback");
    }

    [Fact]
    public void GetFallbackClassName_AlwaysEndsWithFallback()
    {
        var result = NamingHelper.GetFallbackClassName("my_program.cbl");
        result.Should().EndWith("Fallback");
    }

    #endregion

    #region GetOutputFileName

    [Theory]
    [InlineData("RGNB649.cbl", ".cs", "Rgnb649.cs")]
    [InlineData("my_program.cbl", ".java", "MyProgram.java")]
    public void GetOutputFileName_ValidInput_ReturnsExpectedFileName(string cobolFile, string ext, string expected)
    {
        NamingHelper.GetOutputFileName(cobolFile, ext).Should().Be(expected);
    }

    #endregion

    #region IsValidIdentifier

    [Theory]
    [InlineData("MyClass", true)]
    [InlineData("_MyClass", true)]
    [InlineData("MyClass123", true)]
    [InlineData("a", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("123Class", false)]
    [InlineData("My-Class", false)]
    [InlineData("My.Class", false)]
    public void IsValidIdentifier_VariousInputs_ReturnsExpectedResult(string input, bool expected)
    {
        NamingHelper.IsValidIdentifier(input).Should().Be(expected);
    }

    #endregion

    #region IsSemanticClassName

    [Theory]
    [InlineData("PaymentBatchValidator", true)]
    [InlineData("CustomerAccountProcessor", true)]
    [InlineData("OrderService", true)]
    [InlineData("DataHandler", true)]
    [InlineData("ConvertedCobolProgram", false)]
    [InlineData("CobolProgram", false)]
    [InlineData("Program", false)]
    [InlineData("Main", false)]
    [InlineData("", false)]
    public void IsSemanticClassName_VariousClassNames_ReturnsExpectedResult(string className, bool expected)
    {
        NamingHelper.IsSemanticClassName(className).Should().Be(expected);
    }

    [Fact]
    public void IsSemanticClassName_ShortNameWithSuffix_ReturnsTrue()
    {
        // Even short names with semantic suffixes count
        NamingHelper.IsSemanticClassName("DataService").Should().BeTrue();
    }

    [Fact]
    public void IsSemanticClassName_FilenameStyleWithNumbers_ReturnsFalse()
    {
        // Filename-derived names like "Rgnb649" should not be semantic
        NamingHelper.IsSemanticClassName("Rgnb649").Should().BeFalse();
    }

    #endregion

    #region ExtractCSharpClassName

    [Fact]
    public void ExtractCSharpClassName_CodeWithClass_ExtractsClassName()
    {
        var code = "public class PaymentProcessor : IProcessor { }";
        var result = NamingHelper.ExtractCSharpClassName(code, "fallback.cbl");
        result.Should().Be("PaymentProcessor");
    }

    [Fact]
    public void ExtractCSharpClassName_GenericClassName_FallsBackToCobolFileName()
    {
        var code = "public class ConvertedCobolProgram { }";
        var result = NamingHelper.ExtractCSharpClassName(code, "RGNB649.cbl");
        result.Should().Be("Rgnb649");
    }

    [Fact]
    public void ExtractCSharpClassName_NoClassKeyword_FallsBackToCobolFileName()
    {
        // Using code that has no "class " substring at all
        var code = "// Just a comment with no definition";
        var result = NamingHelper.ExtractCSharpClassName(code, "RGNB649.cbl");
        result.Should().Be("Rgnb649");
    }

    [Fact]
    public void ExtractCSharpClassName_EmptyCode_FallsBackToCobolFileName()
    {
        var result = NamingHelper.ExtractCSharpClassName("", "MY_PROG.cbl");
        result.Should().Be("MyProg");
    }

    #endregion

    #region ExtractJavaClassName

    [Fact]
    public void ExtractJavaClassName_CodeWithPublicClass_ExtractsClassName()
    {
        var code = "public class PaymentBatchService { }";
        var result = NamingHelper.ExtractJavaClassName(code, "fallback.cbl");
        result.Should().Be("PaymentBatchService");
    }

    [Fact]
    public void ExtractJavaClassName_GenericClassName_FallsBackToCobolFileName()
    {
        var code = "public class ConvertedCobolProgram { }";
        var result = NamingHelper.ExtractJavaClassName(code, "RGNB649.cbl");
        result.Should().Be("Rgnb649");
    }

    [Fact]
    public void ExtractJavaClassName_CodeWithNonPublicClass_ExtractsClassName()
    {
        var code = "class InternalProcessor { }";
        var result = NamingHelper.ExtractJavaClassName(code, "fallback.cbl");
        result.Should().Be("InternalProcessor");
    }

    #endregion

    #region ReplaceGenericClassName

    [Fact]
    public void ReplaceGenericClassName_SameName_ReturnsUnchangedCode()
    {
        var code = "class MyClass { new MyClass(); }";
        var result = NamingHelper.ReplaceGenericClassName(code, "MyClass", "MyClass");
        result.Should().Be(code);
    }

    [Fact]
    public void ReplaceGenericClassName_DifferentNames_ReplacesAllOccurrences()
    {
        var code = "class ConvertedCobolProgram { new ConvertedCobolProgram(); ConvertedCobolProgram(;}";
        var result = NamingHelper.ReplaceGenericClassName(code, "ConvertedCobolProgram", "PaymentProcessor");
        result.Should().Contain("class PaymentProcessor");
        result.Should().Contain("new PaymentProcessor");
        result.Should().NotContain("ConvertedCobolProgram");
    }

    #endregion
}
