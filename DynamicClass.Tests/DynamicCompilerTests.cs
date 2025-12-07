using DynamicClass.Core;

namespace DynamicClass.Tests {
    public class DynamicCompilerTests {
        [Fact]
        public void CompileCode_ValidCode_ReturnsSuccess() {
            // Arrange
            string validCode = "using System; public static class Calculator { public static int Add(int a, int b) { return a + b; } }";

            // Act
            var result = DynamicClass.Core.DynamicCompiler.CompileCode(validCode);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
            Assert.Empty(result.ErrorMessage);
        }

        [Fact]
        public void CompileCode_InvalidCode_ReturnsFailure() {
            // Arrange
            string invalidCode = "public static class InvalidCalculator { public static int Add(int a, int b) { return a + b } }";

            // Act
            var result = DynamicCompiler.CompileCode(invalidCode);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Assembly);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public void GetPublicStaticMethods_ReturnsAllMethods() {
            // Arrange
            string validCode = "using System; public static class Calculator { public static int Add(int a, int b) { return a + b; } public static int Subtract(int a, int b) { return a - b; } }";
            var compilationResult = DynamicCompiler.CompileCode(validCode);

            // Act
            var methods = DynamicCompiler.GetPublicStaticMethods(compilationResult.Assembly);

            // Assert
            Assert.NotNull(methods);
            Assert.Equal(2, methods.Count);
            Assert.Contains(methods, m => m.Name == "Add");
            Assert.Contains(methods, m => m.Name == "Subtract");
        }

        [Fact]
        public void ConvertToDelegate_ValidMethod_ReturnsDelegate() {
            // Arrange
            string validCode = "using System; public static class Calculator { public static int Add(int a, int b) { return a + b; } }";
            var compilationResult = DynamicCompiler.CompileCode(validCode);
            var methods = DynamicCompiler.GetPublicStaticMethods(compilationResult.Assembly);
            var addMethod = methods.First(m => m.Name == "Add");

            // Act
            var funcDelegate = DynamicCompiler.ConvertToDelegate(addMethod);

            // Assert
            Assert.NotNull(funcDelegate);
            Assert.IsAssignableFrom<Delegate>(funcDelegate);
        }

        [Fact]
        public void ConvertToTypedFunc_ValidMethod_ReturnsTypedDelegate() {
            // Arrange
            string validCode = "using System; public static class MathOperations { public static int Square(int x) { return x * x; } }";
            var compilationResult = DynamicCompiler.CompileCode(validCode);
            var methods = DynamicCompiler.GetPublicStaticMethods(compilationResult.Assembly);
            var squareMethod = methods.First(m => m.Name == "Square");

            // Act
            var funcDelegate = DynamicCompiler.ConvertToTypedFunc<Func<int, int>>(squareMethod);

            // Assert
            Assert.NotNull(funcDelegate);
            Assert.IsType<Func<int, int>>(funcDelegate);
            int result = funcDelegate(5);
            Assert.Equal(25, result);
        }

        [Fact]
        public void CompileFromFile_ValidFile_ReturnsSuccess() {
            // Arrange
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestFile.cs.txt");
            filePath = Path.GetFullPath(filePath);
            Assert.True(File.Exists(filePath), "测试文件不存在: " + filePath);

            // Act
            var result = DynamicCompiler.CompileFromFile(filePath);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
            Assert.Empty(result.ErrorMessage);
        }
    }
}
