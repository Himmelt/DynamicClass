using DynamicClass;
using System;
using System.Linq;
using Xunit;

namespace DynamicClass.Tests {
    public class DynamicCompilerTests {
        private readonly DynamicCompiler _compiler;

        public DynamicCompilerTests() {
            _compiler = new DynamicCompiler();
        }

        [Fact]
        public void CompileCode_ValidCode_ReturnsSuccess() {
            // Arrange
            string validCode = @"using System;

public static class Calculator
{
    public static int Add(int a, int b)
    {
        return a + b;
    }
    
    public static int Subtract(int a, int b)
    {
        return a - b;
    }
    
    public static int Multiply(int a, int b)
    {
        return a * b;
    }
    
    public static double Divide(int a, int b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException(""除数不能为零"");
        }
        return (double)a / b;
    }
    
    public static string GetHelloMessage()
    {
        return ""Hello, Dynamic Compilation!"";
    }
}";

            // Act
            var result = _compiler.CompileCode(validCode);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
            Assert.Empty(result.ErrorMessage);
        }

        [Fact]
        public void CompileCode_InvalidCode_ReturnsFailure() {
            // Arrange
            string invalidCode = @"public static class InvalidCalculator
{
    public static int Add(int a, int b)
    {
        return a + b  // 缺少分号
    }
    
    public static int Subtract(int a, int b)
    {
        return a - b;
    }
}";

            // Act
            var result = _compiler.CompileCode(invalidCode);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Assembly);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public void GetPublicStaticMethods_ReturnsAllMethods() {
            // Arrange
            string validCode = @"using System;

public static class Calculator
{
    public static int Add(int a, int b)
    {
        return a + b;
    }
    
    public static int Subtract(int a, int b)
    {
        return a - b;
    }
    
    public static int Multiply(int a, int b)
    {
        return a * b;
    }
    
    public static double Divide(int a, int b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException(""除数不能为零"");
        }
        return (double)a / b;
    }
    
    public static string GetHelloMessage()
    {
        return ""Hello, Dynamic Compilation!"";
    }
}";
            var compilationResult = _compiler.CompileCode(validCode);

            // Act
            var methods = DynamicCompiler.GetPublicStaticMethods(compilationResult.Assembly);

            // Assert
            Assert.NotNull(methods);
            Assert.Equal(5, methods.Count);
            Assert.Contains(methods, m => m.Name == "Add");
            Assert.Contains(methods, m => m.Name == "Subtract");
            Assert.Contains(methods, m => m.Name == "Multiply");
            Assert.Contains(methods, m => m.Name == "Divide");
            Assert.Contains(methods, m => m.Name == "GetHelloMessage");
        }

        [Fact]
        public void ConvertToFuncDelegate_ValidMethod_ReturnsDelegate() {
            // Arrange
            string validCode = @"using System;

public static class Calculator
{
    public static int Add(int a, int b)
    {
        return a + b;
    }
}";
            var compilationResult = _compiler.CompileCode(validCode);
            var methods = DynamicCompiler.GetPublicStaticMethods(compilationResult.Assembly);
            var addMethod = methods.First(m => m.Name == "Add");

            // Act
            var funcDelegate = _compiler.ConvertToFuncDelegate(addMethod);

            // Assert
            Assert.NotNull(funcDelegate);
            Assert.IsAssignableFrom<Delegate>(funcDelegate);
        }

        [Fact]
        public void ValidateFuncDelegate_ValidDelegate_ExecutesSuccessfully() {
            // Arrange
            string validCode = @"using System;

public static class Calculator
{
    public static int Add(int a, int b)
    {
        return a + b;
    }
    
    public static int Subtract(int a, int b)
    {
        return a - b;
    }
    
    public static string GetHelloMessage()
    {
        return ""Hello, Dynamic Compilation!"";
    }
}";
            var compilationResult = _compiler.CompileCode(validCode);
            var methods = DynamicCompiler.GetPublicStaticMethods(compilationResult.Assembly);
            var addMethod = methods.First(m => m.Name == "Add");
            var funcDelegate = _compiler.ConvertToFuncDelegate(addMethod);

            // Act
            var validationResult = DynamicCompiler.ValidateFuncDelegate(funcDelegate, 10, 5);

            // Assert
            Assert.True(validationResult.Success);
            Assert.Equal(15, validationResult.Result);
            Assert.Empty(validationResult.ErrorMessage);
        }

        [Fact]
        public void ValidateFuncDelegate_DivideByZero_ThrowsException() {
            // Arrange
            string validCode = @"using System;

public static class Calculator
{
    public static double Divide(int a, int b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException(""除数不能为零"");
        }
        return (double)a / b;
    }
}";
            var compilationResult = _compiler.CompileCode(validCode);
            var methods = DynamicCompiler.GetPublicStaticMethods(compilationResult.Assembly);
            var divideMethod = methods.First(m => m.Name == "Divide");
            var funcDelegate = _compiler.ConvertToFuncDelegate(divideMethod);

            // Act
            var validationResult = DynamicCompiler.ValidateFuncDelegate(funcDelegate, 10, 0);

            // Assert
            Assert.False(validationResult.Success);
            Assert.Null(validationResult.Result);
            Assert.NotEmpty(validationResult.ErrorMessage);
            Assert.Contains("除数不能为零", validationResult.ErrorMessage);
        }

        [Fact]
        public void ConvertToTypedFuncDelegate_ValidMethod_ReturnsTypedDelegate() {
            // Arrange
            string validCode = @"using System;

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
}";
            var compilationResult = _compiler.CompileCode(validCode);
            var methods = DynamicCompiler.GetPublicStaticMethods(compilationResult.Assembly);
            var squareMethod = methods.First(m => m.Name == "Square");

            // Act
            var funcDelegate = _compiler.ConvertToTypedFuncDelegate<Func<int, int>>(squareMethod);

            // Assert
            Assert.NotNull(funcDelegate);
            Assert.IsType<Func<int, int>>(funcDelegate);
            int result = funcDelegate(5);
            Assert.Equal(25, result);
        }

        [Fact]
        public void ConvertToTypedFuncDelegate_DoubleAdd_ReturnsCorrectResult() {
            // Arrange
            string validCode = @"using System;

public static class MathOperations
{
    public static double Add(double a, double b)
    {
        return a + b;
    }
}";
            var compilationResult = _compiler.CompileCode(validCode);
            var methods = DynamicCompiler.GetPublicStaticMethods(compilationResult.Assembly);
            var addMethod = methods.First(m => m.Name == "Add");

            // Act
            var funcDelegate = _compiler.ConvertToTypedFuncDelegate<Func<double, double, double>>(addMethod);

            // Assert
            Assert.NotNull(funcDelegate);
            Assert.IsType<Func<double, double, double>>(funcDelegate);
            double result = funcDelegate(3.5, 2.5);
            Assert.Equal(6.0, result);
        }

        [Fact]
        public void GetPublicStaticMethods_ValidAssembly_ReturnsMethods() {
            // Arrange
            string validCode = @"using System;

public static class TestClass
{
    public static int Method1()
    {
        return 1;
    }
    
    public static string Method2(string input)
    {
        return input;
    }
    
    public static bool Method3(bool flag)
    {
        return flag;
    }
}";
            var compilationResult = _compiler.CompileCode(validCode);

            // Act
            var methods = DynamicCompiler.GetPublicStaticMethods(compilationResult.Assembly);

            // Assert
            Assert.NotNull(methods);
            Assert.Equal(3, methods.Count);
            Assert.Contains(methods, m => m.Name == "Method1");
            Assert.Contains(methods, m => m.Name == "Method2");
            Assert.Contains(methods, m => m.Name == "Method3");
        }

        [Fact]
        public void CompileCode_CollectionTypes_CompilesSuccessfully() {
            // Arrange
            string collectionCode = @"using System;
using System.Collections.Generic;

public static class CollectionDemo
{
    public static List<int> CreateList()
    {
        return new List<int> { 1, 2, 3, 4, 5 };
    }
    
    public static HashSet<string> CreateSet()
    {
        return new HashSet<string> { ""apple"", ""banana"", ""cherry"" };
    }
    
    public static int SumList(List<int> list)
    {
        int sum = 0;
        foreach (var item in list)
        {
            sum += item;
        }
        return sum;
    }
}";

            // Act
            var result = _compiler.CompileCode(collectionCode);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
        }

        [Fact]
        public void CompileFromFile_ValidFile_ReturnsSuccess() {
            // Arrange
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestFile.cs.txt");
            filePath = Path.GetFullPath(filePath);
            Assert.True(File.Exists(filePath), "测试文件不存在: " + filePath);

            // Act
            var result = _compiler.CompileFromFile(filePath);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
            Assert.Empty(result.ErrorMessage);

            // 验证可以获取方法并执行
            var methods = DynamicCompiler.GetPublicStaticMethods(result.Assembly);
            Assert.NotNull(methods);
            Assert.Equal(3, methods.Count);

            // 验证Add方法
            var addMethod = methods.First(m => m.Name == "Add");
            var addDelegate = _compiler.ConvertToTypedFuncDelegate<Func<int, int, int>>(addMethod);
            int addResult = addDelegate(10, 5);
            Assert.Equal(15, addResult);

            // 验证GetMessage方法
            var messageMethod = methods.First(m => m.Name == "GetMessage");
            var messageDelegate = _compiler.ConvertToTypedFuncDelegate<Func<string>>(messageMethod);
            string messageResult = messageDelegate();
            Assert.Equal("Hello from file!", messageResult);
        }
    }
}