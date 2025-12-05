using DynamicClass.Core;
using System.Reflection;
using Xunit;

namespace DynamicClass.Tests
{
    public class DelegateConverterTests
    {
        [Fact]
        public void ConvertToDelegate_ValidMethod_ReturnsDelegate()
        {
            // Arrange
            MethodInfo addMethod = typeof(Calculator).GetMethod("Add", BindingFlags.Public | BindingFlags.Static);

            // Act
            var funcDelegate = DelegateConverter.ConvertToDelegate(addMethod);

            // Assert
            Assert.NotNull(funcDelegate);
            Assert.IsAssignableFrom<Delegate>(funcDelegate);
        }

        [Fact]
        public void ConvertToDelegate_MethodWithNoParameters_ReturnsDelegate()
        {
            // Arrange
            MethodInfo helloMethod = typeof(Calculator).GetMethod("GetHelloMessage", BindingFlags.Public | BindingFlags.Static);

            // Act
            var funcDelegate = DelegateConverter.ConvertToDelegate(helloMethod);

            // Assert
            Assert.NotNull(funcDelegate);
        }

        [Fact]
        public void ConvertToDelegate_NullMethod_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DelegateConverter.ConvertToDelegate(null));
        }

        [Fact]
        public void ConvertToTypedFunc_ValidMethod_ReturnsTypedDelegate()
        {
            // Arrange
            MethodInfo squareMethod = typeof(MathOperations).GetMethod("Square", BindingFlags.Public | BindingFlags.Static);

            // Act
            var funcDelegate = DelegateConverter.ConvertToTypedFunc<Func<int, int>>(squareMethod);

            // Assert
            Assert.NotNull(funcDelegate);
            Assert.IsType<Func<int, int>>(funcDelegate);
            int result = funcDelegate(5);
            Assert.Equal(25, result);
        }

        [Fact]
        public void ConvertToTypedFunc_DoubleAdd_ReturnsCorrectResult()
        {
            // Arrange
            MethodInfo addMethod = typeof(MathOperations).GetMethod("Add", BindingFlags.Public | BindingFlags.Static);

            // Act
            var funcDelegate = DelegateConverter.ConvertToTypedFunc<Func<double, double, double>>(addMethod);

            // Assert
            Assert.NotNull(funcDelegate);
            Assert.IsType<Func<double, double, double>>(funcDelegate);
            double result = funcDelegate(3.5, 2.5);
            Assert.Equal(6.0, result);
        }

        [Fact]
        public void ConvertToTypedFunc_NullMethod_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DelegateConverter.ConvertToTypedFunc<Func<int, int>>(null));
        }

        // 辅助测试类
        public static class Calculator
        {
            public static int Add(int a, int b)
            {
                return a + b;
            }

            public static string GetHelloMessage()
            {
                return "Hello, World!";
            }
        }

        public static class MathOperations
        {
            public static int Square(int x)
            {
                return x * x;
            }

            public static double Add(double a, double b)
            {
                return a + b;
            }
        }
    }
}