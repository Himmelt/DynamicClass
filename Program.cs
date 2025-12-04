using System;
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
            Console.WriteLine("=== 演示结束 ===");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}