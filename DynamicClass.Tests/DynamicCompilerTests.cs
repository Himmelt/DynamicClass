using DynamicClass.Core;

namespace DynamicClass.Tests {
    public class DynamicCompilerTests {
        [Fact]
        public void CompileCode_ValidCode_ReturnsSuccess() {
            // Arrange
            string validCode = "using System; public static class Calculator { public static int Add(int a, int b) { return a + b; } }";

            // Act
            var result = DynamicCompiler.CompileCode(validCode);

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
        public void GetPublicStaticMethods_ExcludesPropertyAccessors() {
            // Arrange：静态属性会生成 get_Value 访问器，不应被当作普通方法返回
            string code = "public static class Demo { public static int Value => 42; public static int Get() => Value; }";

            // Act
            var result = DynamicCompiler.CompileCode(code);
            var methods = DynamicCompiler.GetPublicStaticMethods(result.Assembly);

            // Assert
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Single(methods);
            Assert.Equal("Get", methods[0].Name);
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
            Assert.IsType<Delegate>(funcDelegate, exactMatch: false);
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
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestFile.cs");
            filePath = Path.GetFullPath(filePath);
            Assert.True(File.Exists(filePath), "测试文件不存在: " + filePath);

            // Act
            var result = DynamicCompiler.CompileFromFile(filePath);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
            Assert.Empty(result.ErrorMessage);
        }

        [Fact]
        public void CompileCode_EmptyOrWhitespace_ThrowsException() {
            Assert.Throws<ArgumentException>(() => DynamicCompiler.CompileCode([]));
            Assert.Throws<ArgumentNullException>(() => DynamicCompiler.CompileCode(string.Empty));
            Assert.Throws<ArgumentNullException>(() => DynamicCompiler.CompileCode("   "));
        }

        [Fact]
        public void CompileFromFile_NonExistentFile_ThrowsException() {
            string nonExistentFile = Path.Combine(Directory.GetCurrentDirectory(), "NonExistentFile.cs");

            Assert.Throws<FileNotFoundException>(() => DynamicCompiler.CompileFromFile(nonExistentFile));
        }

        [Fact]
        public void CompileFromFile_EmptyOrWhitespacePath_ThrowsException() {
            Assert.Throws<ArgumentException>(() => DynamicCompiler.CompileFromFiles([]));
            Assert.Throws<ArgumentNullException>(() => DynamicCompiler.CompileFromFile(string.Empty));
            Assert.Throws<ArgumentNullException>(() => DynamicCompiler.CompileFromFile("   "));
        }
    }
}
