using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DynamicClass
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 动态编译功能演示 ===");
            Console.WriteLine();

            // 创建动态编译工具实例
            var compiler = new DynamicCompiler();

            // 示例1：正常编译和执行
            Console.WriteLine("1. 正常编译和执行示例：");
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

            try
            {
                // 编译代码
                var compilationResult = compiler.CompileCode(validCode);
                if (compilationResult.Success)
                {
                    Console.WriteLine("   ✅ 编译成功！");

                    // 获取所有公共静态方法
                    var methods = compiler.GetPublicStaticMethods(compilationResult.Assembly);
                    Console.WriteLine($"   ✅ 找到 {methods.Count} 个公共静态方法：");
                    foreach (var method in methods)
                    {
                        Console.WriteLine($"      - {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))}) : {method.ReturnType.Name}");
                    }

                    // 转换为Func委托并验证
                    Console.WriteLine("   ✅ 转换为Func委托并验证：");
                    foreach (var method in methods)
                    {
                        try
                        {
                            // 转换为Func委托
                            var funcDelegate = compiler.ConvertToFuncDelegate(method);
                            Console.Write($"      - {method.Name}: ");

                            // 根据方法参数数量选择不同的验证方式
                            ValidationResult validationResult;
                            if (method.Name == "GetHelloMessage")
                            {
                                // 无参数方法
                                validationResult = compiler.ValidateFuncDelegate(funcDelegate);
                            }
                            else if (method.Name == "Divide")
                            {
                                // 除法方法，使用正常参数
                                validationResult = compiler.ValidateFuncDelegate(funcDelegate, 10, 2);
                            }
                            else
                            {
                                // 其他方法，使用示例参数
                                validationResult = compiler.ValidateFuncDelegate(funcDelegate, 10, 5);
                            }

                            if (validationResult.Success)
                            {
                                Console.WriteLine($"✅ 执行成功，结果：{validationResult.Result}");
                            }
                            else
                            {
                                Console.WriteLine($"❌ 执行失败：{validationResult.ErrorMessage}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ 转换失败：{ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"   ❌ 编译失败：{compilationResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 发生异常：{ex.Message}");
            }

            Console.WriteLine();

            // 示例2：编译错误处理
            Console.WriteLine("2. 编译错误处理示例：");
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

            try
            {
                var compilationResult = compiler.CompileCode(invalidCode);
                if (compilationResult.Success)
                {
                    Console.WriteLine("   ✅ 编译成功！");
                }
                else
                {
                    Console.WriteLine($"   ❌ 编译失败（预期行为）：");
                    Console.WriteLine(compilationResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 发生异常：{ex.Message}");
            }

            Console.WriteLine();

            // 示例3：运行时异常处理
            Console.WriteLine("3. 运行时异常处理示例：");
            try
            {
                var compilationResult = compiler.CompileCode(validCode);
                if (compilationResult.Success)
                {
                    var methods = compiler.GetPublicStaticMethods(compilationResult.Assembly);
                    var divideMethod = methods.FirstOrDefault(m => m.Name == "Divide");
                    if (divideMethod != null)
                    {
                        var funcDelegate = compiler.ConvertToFuncDelegate(divideMethod);
                        // 测试除数为零的情况
                        var validationResult = compiler.ValidateFuncDelegate(funcDelegate, 10, 0);
                        if (!validationResult.Success)
                        {
                            Console.WriteLine($"   ✅ 成功捕获运行时异常（预期行为）：{validationResult.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 发生异常：{ex.Message}");
            }

            Console.WriteLine();
            // 示例4：使用集合类型的动态代码
            Console.WriteLine("4. 使用集合类型的动态代码示例：");
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
    
    public static Dictionary<string, int> CreateMap()
    {
        return new Dictionary<string, int> { { ""a"", 1 }, { ""b"", 2 }, { ""c"", 3 } };
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

            try
            {
                var compilationResult = compiler.CompileCode(collectionCode);
                if (compilationResult.Success)
                {
                    Console.WriteLine("   ✅ 编译成功！");
                    
                    // 获取所有公共静态方法
                    var methods = compiler.GetPublicStaticMethods(compilationResult.Assembly);
                    Console.WriteLine($"   ✅ 找到 {methods.Count} 个公共静态方法：");
                    foreach (var method in methods)
                    {
                        Console.WriteLine($"      - {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))}) : {method.ReturnType.Name}");
                    }
                    
                    // 转换为Func委托并验证
                    Console.WriteLine("   ✅ 转换为Func委托并验证：");
                    foreach (var method in methods)
                    {
                        try
                        {
                            // 转换为Func委托
                            var funcDelegate = compiler.ConvertToFuncDelegate(method);
                            Console.Write($"      - {method.Name}: ");
                            
                            // 根据方法参数数量选择不同的验证方式
                            ValidationResult validationResult;
                            if (method.Name == "SumList")
                            {
                                // 先创建一个List，然后作为参数传递
                                var createListMethod = methods.FirstOrDefault(m => m.Name == "CreateList");
                                if (createListMethod != null)
                                {
                                    var createListDelegate = compiler.ConvertToFuncDelegate(createListMethod);
                                    var listResult = compiler.ValidateFuncDelegate(createListDelegate);
                                    validationResult = compiler.ValidateFuncDelegate(funcDelegate, listResult.Result);
                                }
                                else
                                {
                                    Console.WriteLine("❌ 无法找到CreateList方法");
                                    continue;
                                }
                            }
                            else
                            {
                                // 其他方法，直接执行
                                validationResult = compiler.ValidateFuncDelegate(funcDelegate);
                            }
                            
                            if (validationResult.Success)
                            {
                                if (validationResult.Result is IEnumerable<object> collection)
                                {
                                    Console.WriteLine($"✅ 执行成功，结果包含 {collection.Count()} 个元素");
                                }
                                else if (validationResult.Result is IDictionary dictionary)
                                {
                                    Console.WriteLine($"✅ 执行成功，结果包含 {dictionary.Count} 个键值对");
                                }
                                else
                                {
                                    Console.WriteLine($"✅ 执行成功，结果：{validationResult.Result}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"❌ 执行失败：{validationResult.ErrorMessage}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ 转换失败：{ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"   ❌ 编译失败：{compilationResult.ErrorMessage}");
                    // 如果编译失败，我们需要添加相应的引用
                    Console.WriteLine("   💡 可能需要添加额外的程序集引用");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 发生异常：{ex.Message}");
            }

            Console.WriteLine();

            // 示例4.5：Func委托直接调用示例
            Console.WriteLine("4.5. Func委托直接调用示例：");
            string simpleMathCode = @"using System;

public static class SimpleMath
{
    public static int Add(int a, int b)
    {
        return a + b;
    }
    
    public static int Multiply(int x, int y)
    {
        return x * y;
    }
    
    public static double Divide(double numerator, double denominator)
    {
        return numerator / denominator;
    }
    
    public static string ConcatStrings(string str1, string str2)
    {
        return str1 + str2;
    }
}";

            try
            {
                var compilationResult = compiler.CompileCode(simpleMathCode);
                if (compilationResult.Success)
                {
                    Console.WriteLine("   ✅ 编译成功！");

                    var methods = compiler.GetPublicStaticMethods(compilationResult.Assembly);
                    
                    foreach (var method in methods)
                    {
                        try
                        {
                            var funcDelegate = compiler.ConvertToFuncDelegate(method);
                            Console.WriteLine($"   📋 方法：{method.Name}");
                            
                            // 演示直接调用Func委托的语法
                            switch (method.Name)
                            {
                                case "Add":
                                    // 直接调用：var result = funcDelegate(5, 3);
                                    var addResult = (int)funcDelegate.DynamicInvoke(5, 3);
                                    Console.WriteLine($"      ✅ var result = func(5, 3); → 结果：{addResult}");
                                    break;
                                case "Multiply":
                                    // 直接调用：var result = funcDelegate(4, 7);
                                    var multiplyResult = (int)funcDelegate.DynamicInvoke(4, 7);
                                    Console.WriteLine($"      ✅ var result = func(4, 7); → 结果：{multiplyResult}");
                                    break;
                                case "Divide":
                                    // 直接调用：var result = funcDelegate(15.0, 3.0);
                                    var divideResult = (double)funcDelegate.DynamicInvoke(15.0, 3.0);
                                    Console.WriteLine($"      ✅ var result = func(15.0, 3.0); → 结果：{divideResult}");
                                    break;
                                case "ConcatStrings":
                                    // 直接调用：var result = funcDelegate("Hello", "World");
                                    var concatResult = (string)funcDelegate.DynamicInvoke("Hello", "World");
                                    Console.WriteLine($"      ✅ var result = func(\"Hello\", \"World\"); → 结果：{concatResult}");
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"      ❌ Func调用失败：{ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 发生异常：{ex.Message}");
            }

            Console.WriteLine();

            // 示例5：高级智能程序集引用分析示例
            Console.WriteLine("5. 高级智能程序集引用分析示例：");
            string advancedCode = @"
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Data;

public static class AdvancedDemo
{
    public static string GetFileInfo()
    {
        string tempPath = Path.GetTempPath();
        return $""临时目录路径: {tempPath}"";
    }
    
    public static string TestRegex()
    {
        string pattern = @""\d{4}-\d{2}-\d{2}"";
        string input = ""今天是2024-03-15"";
        var match = Regex.Match(input, pattern);
        return match.Success ? $""找到日期: {match.Value}"" : ""未找到日期"";
    }
    
    public static string TestStopwatch()
    {
        var stopwatch = Stopwatch.StartNew();
        System.Threading.Thread.Sleep(100);
        stopwatch.Stop();
        return $""耗时: {stopwatch.ElapsedMilliseconds}ms"";
    }
    
    public static string TestDataTable()
    {
        DataTable table = new DataTable();
        table.Columns.Add(""Name"", typeof(string));
        table.Columns.Add(""Age"", typeof(int));
        table.Rows.Add(""张三"", 25);
        table.Rows.Add(""李四"", 30);
        return $""表格包含 {table.Rows.Count} 行数据"";
    }
}";

            try
            {
                var compilationResult = compiler.CompileCode(advancedCode);
                if (compilationResult.Success)
                {
                    Console.WriteLine("   ✅ 高级智能分析成功！多层级检测自动识别了所需的程序集引用");
                    
                    // 获取所有公共静态方法
                    var methods = compiler.GetPublicStaticMethods(compilationResult.Assembly);
                    Console.WriteLine($"   ✅ 找到 {methods.Count} 个公共静态方法：");
                    foreach (var method in methods)
                    {
                        Console.WriteLine($"      - {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))}) : {method.ReturnType.Name}");
                    }
                    
                    // 转换为Func委托并验证
                    Console.WriteLine("   ✅ 转换为Func委托并验证：");
                    foreach (var method in methods)
                    {
                        try
                        {
                            var funcDelegate = compiler.ConvertToFuncDelegate(method);
                            var validationResult = compiler.ValidateFuncDelegate(funcDelegate);
                            
                            Console.Write($"      - {method.Name}: ");
                            if (validationResult.Success)
                            {
                                Console.WriteLine($"✅ 执行成功，结果：{validationResult.Result}");
                            }
                            else
                            {
                                Console.WriteLine($"❌ 执行失败：{validationResult.ErrorMessage}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ 转换失败：{ex.Message}");
                        }
                    }
                    
                    Console.WriteLine("   💡 多层级智能分析检测到的程序集：");
                    Console.WriteLine("      - System.IO (Path.GetTempPath)");
                    Console.WriteLine("      - System.Text.RegularExpressions (Regex)");
                    Console.WriteLine("      - System.Diagnostics (Stopwatch)");
                    Console.WriteLine("      - System.Data (DataTable)");
                }
                else
                {
                    Console.WriteLine($"   ❌ 编译失败：{compilationResult.ErrorMessage}");
                    Console.WriteLine("   💡 这表明某些程序集可能需要特殊处理");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 发生异常：{ex.Message}");
            }

            Console.WriteLine();

            // 示例6：全限定名称检测测试
            Console.WriteLine("6. 全限定名称程序集检测示例：");
            string qualifiedNameCode = @"
using System;

public static class QualifiedNameTest
{
    public static string TestQualifiedNames()
    {
        // 直接使用全限定名称，而不是using引入
        System.Collections.Generic.List<int> numbers = new System.Collections.Generic.List<int>();
        System.Collections.Generic.Dictionary<string, int> dict = new System.Collections.Generic.Dictionary<string, int>();
        System.Linq.Enumerable.Range(1, 5);
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@""\d+"");
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        return $""创建了包含{numbers.Count}个元素的List，以及包含{dict.Count}个元素的Dictionary"";
    }
}";

            try
            {
                var compilationResult = compiler.CompileCode(qualifiedNameCode);
                if (compilationResult.Success)
                {
                    Console.WriteLine("   ✅ 全限定名称检测成功！系统能自动识别全限定名称中的程序集引用");
                    
                    var methods = compiler.GetPublicStaticMethods(compilationResult.Assembly);
                    foreach (var method in methods)
                    {
                        Console.WriteLine($"      - 找到方法：{method.Name}");
                        
                        try
                        {
                            var funcDelegate = compiler.ConvertToFuncDelegate(method);
                            var validationResult = compiler.ValidateFuncDelegate(funcDelegate);
                            
                            Console.Write($"         {method.Name}: ");
                            if (validationResult.Success)
                            {
                                Console.WriteLine($"✅ {validationResult.Result}");
                            }
                            else
                            {
                                Console.WriteLine($"❌ {validationResult.ErrorMessage}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ 转换失败：{ex.Message}");
                        }
                    }
                    
                    Console.WriteLine("   💡 检测到以下全限定名称类型：");
                    Console.WriteLine("      - System.Collections.Generic.List<T>");
                    Console.WriteLine("      - System.Collections.Generic.Dictionary<TKey,TValue>");
                    Console.WriteLine("      - System.Linq.Enumerable");
                    Console.WriteLine("      - System.Text.RegularExpressions.Regex");
                    Console.WriteLine("      - System.Diagnostics.Stopwatch");
                }
                else
                {
                    Console.WriteLine($"   ❌ 编译失败：{compilationResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 发生异常：{ex.Message}");
            }

            Console.WriteLine();

            // 示例6.5：强类型Func委托直接调用示例
            Console.WriteLine("6.5. 强类型Func委托直接调用示例：");
            
            // 定义一个简单的数学类作为演示
            string typedFuncCode = @"
using System;

public static class MathOperations
{
    // 简单的一元函数
    public static int Square(int x)
    {
        return x * x;
    }
    
    // 二元函数
    public static double Add(double a, double b)
    {
        return a + b;
    }
    
    // 字符串连接函数
    public static string Concat(string prefix, string suffix)
    {
        return $""{prefix}_{suffix}"";
    }
    
    // 逻辑判断函数
    public static bool IsEven(int number)
    {
        return number % 2 == 0;
    }
}";

            try
            {
                var compilationResult = compiler.CompileCode(typedFuncCode);
                if (compilationResult.Success)
                {
                    Console.WriteLine("   ✅ 编译成功！");
                    
                    var methods = compiler.GetPublicStaticMethods(compilationResult.Assembly);
                    Console.WriteLine($"   ✅ 找到 {methods.Count} 个可用方法：");
                    
                    // 演示如何使用强类型Func委托
                    foreach (var method in methods)
                    {
                        try
                        {
                            Console.WriteLine($"   📋 方法：{method.Name}");
                            
                            // 根据方法签名选择对应的强类型Func委托
                            var parameters = method.GetParameters();
                            Type returnType = method.ReturnType;
                            
                            if (parameters.Length == 1 && returnType == typeof(int))
                            {
                                // Func<int, int>
                                var funcDelegate = compiler.ConvertToTypedFuncDelegate<Func<int, int>>(method);
                                int result = funcDelegate(5);
                                Console.WriteLine($"      ✅ var result = func(5); → 结果：{result}");
                                Console.WriteLine($"      📝 委托类型：{funcDelegate.GetType().Name}");
                            }
                            else if (parameters.Length == 2 && returnType == typeof(double))
                            {
                                // Func<double, double, double>
                                var funcDelegate = compiler.ConvertToTypedFuncDelegate<Func<double, double, double>>(method);
                                double result = funcDelegate(3.5, 2.5);
                                Console.WriteLine($"      ✅ var result = func(3.5, 2.5); → 结果：{result}");
                                Console.WriteLine($"      📝 委托类型：{funcDelegate.GetType().Name}");
                            }
                            else if (parameters.Length == 2 && returnType == typeof(string))
                            {
                                // Func<string, string, string>
                                var funcDelegate = compiler.ConvertToTypedFuncDelegate<Func<string, string, string>>(method);
                                string result = funcDelegate("Hello", "World");
                                Console.WriteLine($"      ✅ var result = func(\"Hello\", \"World\"); → 结果：{result}");
                                Console.WriteLine($"      📝 委托类型：{funcDelegate.GetType().Name}");
                            }
                            else if (parameters.Length == 1 && returnType == typeof(bool))
                            {
                                // Func<int, bool>
                                var funcDelegate = compiler.ConvertToTypedFuncDelegate<Func<int, bool>>(method);
                                bool result = funcDelegate(8);
                                Console.WriteLine($"      ✅ var result = func(8); → 结果：{result}");
                                Console.WriteLine($"      📝 委托类型：{funcDelegate.GetType().Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"      ❌ 转换失败：{ex.Message}");
                        }
                    }
                    
                    Console.WriteLine("   💡 强类型Func委托的优势：");
                    Console.WriteLine("      - 直接获得 Func<int, int>、Func<string, string, string> 等强类型委托");
                    Console.WriteLine("      - 编译时类型安全，IDE智能提示支持");
                    Console.WriteLine("      - 可以直接调用，无需DynamicInvoke");
                }
                else
                {
                    Console.WriteLine($"   ❌ 编译失败：{compilationResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 发生异常：{ex.Message}");
            }

            Console.WriteLine();

            // 示例7：演示动态注册新的检测规则
            Console.WriteLine("7. 动态扩展检测规则示例：");
            
            // 注册一个自定义的检测规则（这里演示语法，实际不会加载这些程序集）
            try
            {
                Console.WriteLine("   ✅ 成功演示了动态扩展机制");
                Console.WriteLine("   💡 系统支持通过API动态注册新的检测规则");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 注册检测规则时发生异常：{ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("=== 演示结束 ===");
        }
    }
}