using DynamicClass.Core;

namespace DynamicClass.Tests {
    public class MultiFileCompilationTests {
        [Fact]
        public void CompileCode_MultipleCodes_ReturnsSuccess() {
            string code1 = "using System; public static class Helper { public static int Multiply(int a, int b) { return a * b; } }";
            string code2 = "public static class Calculator { public static int Add(int a, int b) { return a + b; } }";

            var result = DynamicCompiler.CompileCode([code1, code2]);

            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
            Assert.Empty(result.ErrorMessage);
        }

        [Fact]
        public void CompileCode_EmptyArray_ThrowsException() {
            Assert.Throws<ArgumentException>(() => DynamicCompiler.CompileCode([]));
        }

        [Fact]
        public void CompileCode_ContainsNull_ThrowsException() {
            Assert.Throws<ArgumentException>(() => DynamicCompiler.CompileCode(["public static class A { }", null!]));
        }

        [Fact]
        public void CompileCode_ContainsWhitespace_ThrowsException() {
            Assert.Throws<ArgumentException>(() => DynamicCompiler.CompileCode(["public static class A { }", "   "]));
        }

        [Fact]
        public void CompileFromFiles_MultipleFiles_ReturnsSuccess() {
            string tempDir = Path.GetTempPath();
            string file1 = Path.Combine(tempDir, $"test1_{Guid.NewGuid()}.cs");
            string file2 = Path.Combine(tempDir, $"test2_{Guid.NewGuid()}.cs");

            try {
                File.WriteAllText(file1, "using System; public static class Helper { public static int Multiply(int a, int b) { return a * b; } }");
                File.WriteAllText(file2, "public static class Calculator { public static int Add(int a, int b) { return a + b; } }");

                var result = DynamicCompiler.CompileFromFiles([file1, file2]);

                Assert.True(result.Success);
                Assert.NotNull(result.Assembly);
                Assert.Empty(result.ErrorMessage);
            } finally {
                if (File.Exists(file1)) File.Delete(file1);
                if (File.Exists(file2)) File.Delete(file2);
            }
        }

        [Fact]
        public void CompileFromFiles_EmptyArray_ThrowsException() {
            Assert.Throws<ArgumentException>(() => DynamicCompiler.CompileFromFiles([]));
        }

        [Fact]
        public void CompileFromFiles_NonExistentFile_ThrowsException() {
            string nonExistent = Path.Combine(Path.GetTempPath(), "nonexistent.cs");
            Assert.Throws<FileNotFoundException>(() => DynamicCompiler.CompileFromFiles([nonExistent]));
        }

        [Fact]
        public void CompileFromFiles_WithDependencies_ReturnsSuccess() {
            string tempDir = Path.GetTempPath();
            string file1 = Path.Combine(tempDir, $"base_{Guid.NewGuid()}.cs");
            string file2 = Path.Combine(tempDir, $"derived_{Guid.NewGuid()}.cs");

            try {
                string baseCode = @"
using System;
public static class BaseClass {
    public static string GetMessage() {
        return ""Hello from BaseClass"";
    }
}";
                string derivedCode = @"
public static class DerivedClass {
    public static string GetGreeting() {
        return BaseClass.GetMessage() + "" - Greeting!"";
    }
}";

                File.WriteAllText(file1, baseCode);
                File.WriteAllText(file2, derivedCode);

                var result = DynamicCompiler.CompileFromFiles([file1, file2]);

                Assert.True(result.Success, result.ErrorMessage);
                Assert.NotNull(result.Assembly);
            } finally {
                if (File.Exists(file1)) File.Delete(file1);
                if (File.Exists(file2)) File.Delete(file2);
            }
        }

        [Fact]
        public void CompileFromFiles_WithNamespaceDependencies_ReturnsSuccess() {
            string tempDir = Path.GetTempPath();
            string file1 = Path.Combine(tempDir, $"namespace1_{Guid.NewGuid()}.cs");
            string file2 = Path.Combine(tempDir, $"namespace2_{Guid.NewGuid()}.cs");

            try {
                string code1 = @"
using System;
using System.Linq;
namespace Utilities {
    public static class StringHelper {
        public static string Reverse(string input) {
            if (input == null) return null;
            return new string(input.Reverse().ToArray());
        }
    }
}";
                string code2 = @"
using Utilities;
public static class MainClass {
    public static string Process(string input) {
        return StringHelper.Reverse(input);
    }
}";

                File.WriteAllText(file1, code1);
                File.WriteAllText(file2, code2);

                var result = DynamicCompiler.CompileFromFiles([file1, file2]);

                Assert.True(result.Success, result.ErrorMessage);
                Assert.NotNull(result.Assembly);
            } finally {
                if (File.Exists(file1)) File.Delete(file1);
                if (File.Exists(file2)) File.Delete(file2);
            }
        }
    }
}