using DynamicClass.Core;

namespace DynamicClass.Tests {
    public class CodeAnalyzerTests {
        [Fact]
        public void GetRequiredReferences_ReturnsRuntimeClosure() {
            // Act
            var references = CodeAnalyzer.GetRequiredReferences();

            // Assert
            Assert.NotNull(references);
            Assert.NotEmpty(references);
            // 闭包应包含框架引用
            Assert.Contains(references, r =>
                r.Display?.EndsWith("System.Runtime.dll", StringComparison.OrdinalIgnoreCase) == true);
            // 闭包应包含应用目录里的程序集（本库自身）
            Assert.Contains(references, r =>
                r.Display?.EndsWith("DynamicClass.dll", StringComparison.OrdinalIgnoreCase) == true);
        }
    }
}