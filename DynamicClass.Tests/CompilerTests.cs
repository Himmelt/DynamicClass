using DynamicClass.Core;
using DynamicClass.Models;
using System.IO;
using Xunit;

namespace DynamicClass.Tests
{
    public class CompilerTests
    {
        [Fact]
        public void CompileCode_ValidCode_ReturnsSuccess()
        {
            // Arrange
            string validCode = "using System; public static class Calculator { public static int Add(int a, int b) { return a + b; } }";

            // Act
            var result = Compiler.CompileCode(validCode);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
            Assert.Empty(result.ErrorMessage);
        }

        [Fact]
        public void CompileCode_InvalidCode_ReturnsFailure()
        {
            // Arrange
            string invalidCode = "public static class InvalidCalculator { public static int Add(int a, int b) { return a + b } }";

            // Act
            var result = Compiler.CompileCode(invalidCode);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Assembly);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public void CompileCode_EmptyCode_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => Compiler.CompileCode(null));
            Assert.Throws<ArgumentNullException>(() => Compiler.CompileCode(string.Empty));
            Assert.Throws<ArgumentNullException>(() => Compiler.CompileCode("   "));
        }

        [Fact]
        public void CompileFromFile_ValidFile_ReturnsSuccess()
        {
            // Arrange
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestFile.cs.txt");
            filePath = Path.GetFullPath(filePath);
            Assert.True(File.Exists(filePath), "测试文件不存在: " + filePath);

            // Act
            var result = Compiler.CompileFromFile(filePath);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
            Assert.Empty(result.ErrorMessage);
        }

        [Fact]
        public void CompileFromFile_NonExistentFile_ThrowsException()
        {
            // Arrange
            string nonExistentFile = Path.Combine(Directory.GetCurrentDirectory(), "NonExistentFile.cs");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => Compiler.CompileFromFile(nonExistentFile));
        }

        [Fact]
        public void CompileFromFile_EmptyFilePath_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => Compiler.CompileFromFile(null));
            Assert.Throws<ArgumentNullException>(() => Compiler.CompileFromFile(string.Empty));
            Assert.Throws<ArgumentNullException>(() => Compiler.CompileFromFile("   "));
        }
    }
}