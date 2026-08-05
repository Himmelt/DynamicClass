using DynamicClass.Core;
using System.Reflection;

namespace DynamicClass.Tests {
    public class DelegateConverterTests {
        [Fact]
        public void ConvertToDelegate_ValidMethod_ReturnsDelegate() {
            // Arrange
            var addMethod = typeof(Calculator).GetMethod("Add", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(addMethod);

            // Act
            var funcDelegate = DelegateConverter.ConvertToDelegate(addMethod);

            // Assert
            Assert.NotNull(funcDelegate);
            Assert.IsAssignableFrom<Delegate>(funcDelegate);
        }

        [Fact]
        public void ConvertToDelegate_MethodWithNoParameters_ReturnsDelegate() {
            // Arrange
            var helloMethod = typeof(Calculator).GetMethod("GetHelloMessage", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(helloMethod);

            // Act
            var funcDelegate = DelegateConverter.ConvertToDelegate(helloMethod);

            // Assert
            Assert.NotNull(funcDelegate);
        }

        [Fact]
        public void ConvertToDelegate_VoidReturn_ReturnsDelegate() {
            // Arrange
            var greetMethod = typeof(Calculator).GetMethod("Greet", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(greetMethod);

            // Act
            var funcDelegate = DelegateConverter.ConvertToDelegate(greetMethod);

            // Assert
            Assert.NotNull(funcDelegate);
            funcDelegate.DynamicInvoke(null);
        }

        [Fact]
        public void ConvertToDelegate_NullMethod_ThrowsException() {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DelegateConverter.ConvertToDelegate(null));
        }

        [Fact]
        public void ConvertToTypedFunc_ValidMethod_ReturnsTypedDelegate() {
            // Arrange
            var squareMethod = typeof(MathOperations).GetMethod("Square", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(squareMethod);

            // Act
            var funcDelegate = DelegateConverter.ConvertToTypedFunc<Func<int, int>>(squareMethod);

            // Assert
            Assert.NotNull(funcDelegate);
            Assert.IsType<Func<int, int>>(funcDelegate);
            int result = funcDelegate(5);
            Assert.Equal(25, result);
        }

        [Fact]
        public void ConvertToTypedFunc_DoubleAdd_ReturnsCorrectResult() {
            // Arrange
            var addMethod = typeof(MathOperations).GetMethod("Add", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(addMethod);

            // Act
            var funcDelegate = DelegateConverter.ConvertToTypedFunc<Func<double, double, double>>(addMethod);

            // Assert
            Assert.NotNull(funcDelegate);
            Assert.IsType<Func<double, double, double>>(funcDelegate);
            double result = funcDelegate(3.5, 2.5);
            Assert.Equal(6.0, result);
        }

        [Fact]
        public void ConvertToTypedFunc_NullMethod_ThrowsException() {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DelegateConverter.ConvertToTypedFunc<Func<int, int>>(null));
        }

        // 辅助测试类
        public static class Calculator {
            public static int Add(int a, int b) {
                return a + b;
            }

            public static string GetHelloMessage() {
                return "Hello, World!";
            }

            public static void Greet() {
            }
        }

        public static class MathOperations {
            public static int Square(int x) {
                return x * x;
            }

            public static double Add(double a, double b) {
                return a + b;
            }
        }
    }
}
