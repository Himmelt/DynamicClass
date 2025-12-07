using DynamicClass.Core;

namespace DynamicClass.Tests {
    public class CodeAnalyzerTests {
        [Fact]
        public void ExtractUsingStatements_ValidCode_ReturnsUsingStatements() {
            // Arrange
            string codeWithUsings = "using System; using System.Collections.Generic; using System.Linq; public static class Calculator { public static int Add(int a, int b) { return a + b; } }";

            // Act
            var usingStatements = CodeAnalyzer.ExtractUsingStatements(codeWithUsings);

            // Assert
            Assert.NotNull(usingStatements);
            Assert.Equal(3, usingStatements.Count);
            Assert.Contains("System", usingStatements);
            Assert.Contains("System.Collections.Generic", usingStatements);
            Assert.Contains("System.Linq", usingStatements);
        }

        [Fact]
        public void ExtractUsingStatements_NoUsings_ReturnsEmptySet() {
            // Arrange
            string codeWithoutUsings = "public static class Calculator { public static int Add(int a, int b) { return a + b; } }";

            // Act
            var usingStatements = CodeAnalyzer.ExtractUsingStatements(codeWithoutUsings);

            // Assert
            Assert.NotNull(usingStatements);
            Assert.Empty(usingStatements);
        }

        [Fact]
        public void AnalyzeUsedTypes_ValidCode_ReturnsUsedTypes() {
            // Arrange
            string codeWithTypes = "using System; using System.Collections.Generic; public static class CollectionDemo { public static List<int> CreateList() { return new List<int> { 1, 2, 3, 4, 5 }; } }";

            // Act
            var usedTypes = CodeAnalyzer.AnalyzeUsedTypes(codeWithTypes);

            // Assert
            Assert.NotNull(usedTypes);
            Assert.Contains("System", usedTypes);
            Assert.Contains("System.Collections.Generic", usedTypes);
        }

        [Fact]
        public void RegisterAssemblyRule_AddsNewRule() {
            // Arrange
            string testAssembly = "TestAssembly";
            string testNamespace = "TestNamespace";
            string[] testTypes = { "TestType1", "TestType2" };

            // Act
            CodeAnalyzer.RegisterAssemblyRule(testAssembly, testNamespace, testTypes);

            // Assert
            // 验证规则已注册，通过测试GetRequiredReferences间接验证
            string codeWithTestType = "using TestNamespace; public static class TestClass { public TestType1 Method1() { return new TestType1(); } }";

            // 调用GetRequiredReferences，应该能正常执行，不会抛出异常
            var references = CodeAnalyzer.GetRequiredReferences(codeWithTestType);
            Assert.NotNull(references);
        }

        [Fact]
        public void GetRequiredReferences_ReturnsBaseReferences() {
            // Arrange
            string simpleCode = "public static class SimpleClass { public static int Method1(int a, int b) { return a + b; } }";

            // Act
            var references = CodeAnalyzer.GetRequiredReferences(simpleCode);

            // Assert
            Assert.NotNull(references);
            Assert.NotEmpty(references);
        }
    }
}
