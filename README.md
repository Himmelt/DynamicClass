# DynamicClass

DynamicClass是一个轻量级的C#动态代码编译库，允许您在运行时编译C#静态类代码，并将方法转换为强类型或弱类型的Func委托。该库具有智能的程序集引用检测功能，可以自动识别并添加代码所需的程序集引用。

> **注意**：本项目是由AI生成的，主要使用了Trae等工具辅助开发。

## 功能特性

- ✅ 运行时动态编译C#静态类代码
- ✅ 支持从字符串或文件编译代码
- ✅ 智能检测并添加所需的程序集引用
- ✅ 将编译后的方法转换为Delegate委托
- ✅ 支持强类型的Func<>委托转换
- ✅ 可扩展的程序集检测规则
- ✅ 提供清晰的编译结果和错误信息

## 支持的程序集

DynamicClass内置了对多种常用.NET程序集的检测支持：

- 核心.NET程序集（System.Linq、System.Collections.Generic等）
- JSON处理（System.Text.Json、Newtonsoft.Json）
- HTTP和网络（System.Net.Http）
- 正则表达式（System.Text.RegularExpressions）
- 诊断和调试（System.Diagnostics）
- 数据库访问（System.Data、System.Data.SqlClient）
- Entity Framework Core
- Microsoft.Extensions系列库（Configuration、DependencyInjection、Logging）
- ASP.NET Core（Microsoft.AspNetCore.Http）

## 安装

您可以通过NuGet安装DynamicClass：

```bash
PM> Install-Package DynamicClass
```

或使用.NET CLI：

```bash
dotnet add package DynamicClass
```

## 快速开始

### 1. 编译代码并执行

```csharp
using DynamicClass.Core;
using System;

// 定义要编译的C#代码
string code = @"
using System;

public static class Calculator
{
    public static int Add(int a, int b)
    {
        return a + b;
    }
    
    public static string Greet(string name)
    {
        return $"Hello, {name}!";
    }
}";

// 编译代码
var result = DynamicCompiler.CompileCode(code);

if (result.Success)
{
    // 获取所有公共静态方法
    var methods = DynamicCompiler.GetPublicStaticMethods(result.Assembly);
    
    // 转换为Delegate并调用
    foreach (var method in methods)
    {
        if (method.Name == "Add")
        {
            var addDelegate = DynamicCompiler.ConvertToDelegate(method);
            int sum = (int)addDelegate.DynamicInvoke(5, 3);
            Console.WriteLine($"Add(5, 3) = {sum}");
        }
        else if (method.Name == "Greet")
        {
            var greetDelegate = DynamicCompiler.ConvertToDelegate(method);
            string greeting = (string)greetDelegate.DynamicInvoke("World");
            Console.WriteLine(greeting);
        }
    }
}
else
{
    Console.WriteLine("编译失败：");
    Console.WriteLine(result.ErrorMessage);
}
```

### 2. 使用强类型Func委托

```csharp
if (result.Success)
{
    // 获取Add方法
    var addMethod = DynamicCompiler.GetPublicStaticMethods(result.Assembly)
        .First(m => m.Name == "Add");
    
    // 转换为强类型Func委托
    Func<int, int, int> addFunc = DynamicCompiler.ConvertToTypedFunc<Func<int, int, int>>(addMethod);
    
    // 直接调用强类型委托
    int sum = addFunc(10, 20);
    Console.WriteLine($"Add(10, 20) = {sum}");
}
```

### 3. 从文件编译代码

```csharp
// 从文件编译代码
string filePath = "Calculator.cs";
var result = DynamicCompiler.CompileFromFile(filePath);

if (result.Success)
{
    Console.WriteLine("代码编译成功！");
    // 执行方法...
}
else
{
    Console.WriteLine("编译失败：");
    Console.WriteLine(result.ErrorMessage);
}
```

### 4. 扩展程序集检测规则

```csharp
// 注册自定义程序集检测规则
DynamicCompiler.RegisterAssemblyRule(
    assemblyName: "MyCustomLibrary",
    namespacePattern: "MyCustomLibrary",
    typePatterns: new[] { @"MyCustomType", @"MyOtherType" }
);
```

## API参考

### DynamicCompiler 类

#### 编译方法

- `CompilationResult CompileCode(string code)`: 编译C#静态类代码字符串
- `CompilationResult CompileFromFile(string filePath)`: 从文件编译C#静态类代码

#### 方法转换

- `Delegate ConvertToDelegate(MethodInfo method)`: 将方法转换为Delegate委托
- `TFunc ConvertToTypedFunc<TFunc>(MethodInfo method)`: 将方法转换为强类型Func委托
- `List<MethodInfo> GetPublicStaticMethods(Assembly? assembly)`: 获取程序集中的所有公共静态方法

#### 程序集规则

- `void RegisterAssemblyRule(string assemblyName, string namespacePattern, params string[] typePatterns)`: 注册自定义程序集检测规则

### CompilationResult 类

- `bool Success`: 编译是否成功
- `Assembly? Assembly`: 编译后的程序集（成功时）
- `string ErrorMessage`: 编译错误信息（失败时）

## 注意事项

1. 该库仅支持编译静态类代码
2. 编译后的代码在内存中执行，不会生成物理文件
3. 请确保编译的代码符合C#语法规范
4. 某些高级功能可能需要额外的程序集引用

## 示例

查看`DynamicClass.Tests`项目以获取更多使用示例。

## 贡献

欢迎提交Issue和Pull Request！

## 许可证

MIT许可证 - 详见LICENSE文件。

## 作者

Himmelt
