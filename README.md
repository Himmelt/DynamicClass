# DynamicClass

DynamicClass是一个轻量级的C#动态代码编译库，允许您在运行时编译C#静态类代码，并将方法转换为强类型或弱类型的Func委托。编译引用采用**运行时闭包**策略：自动引用当前进程可见的全部程序集，无需配置任何检测规则。

> **注意**：本项目是由AI生成的，主要使用了 Trae IDE、Codex、GLM-5.2、DeepSeek-V4 等工具辅助开发。

## 功能特性

- ✅ 运行时动态编译C#静态类代码
- ✅ 支持从字符串、文件或多个文件编译代码
- ✅ 自动引用运行时可见的全部程序集（框架、NuGet 包、宿主程序集）
- ✅ 将编译后的方法转换为Delegate委托
- ✅ 支持强类型的Func<>委托转换
- ✅ 提供清晰的编译结果和错误信息

## 引用策略

动态编译时，编译器会引用运行时实际能解析的全部程序集，让编译代码看到的类型宇宙与运行时一致：

1. `TRUSTED_PLATFORM_ASSEMBLIES`：.NET 运行时框架的全部程序集
2. `AppContext.BaseDirectory` 下的全部 `*.dll`：应用本地程序集与 NuGet 包（含未列入依赖清单的散装 DLL）
3. 已加载程序集：兜底覆盖不在上述两处的程序集

按程序集简单名去重，引用列表在进程生命周期内构建一次并缓存。因此动态代码可以**直接**使用任意运行时可见的库（如 `MathNet.Numerics`）或宿主程序集的公开 API，不需要预先注册规则。

> **限制**：宿主程序不能以单文件方式发布（`PublishSingleFile`）。单文件会把托管程序集打进可执行文件，程序集不再以独立 DLL 文件存在（`Assembly.Location` 为空、应用目录下没有 DLL），运行时闭包将无法提供这些程序集的引用，动态代码编译会失败。

> **限制**：宿主启用发布裁剪（`PublishTrimmed` 等）时，未被宿主代码引用的 NuGet 程序集（例如仅用于动态编译的 `MathNet.Numerics`）可能被裁剪并从输出目录移除，动态代码将无法引用它们。动态编译所需的程序集应通过裁剪配置显式保留。

## 安装

您可以通过NuGet安装DynamicClass：

支持 .NET 8、.NET 9、.NET 10。

```bash
PM> Install-Package Himmelt.DynamicClass
```

或使用.NET CLI：

```bash
dotnet add package Himmelt.DynamicClass
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

### 4. 多文件编译

```csharp
// 从多个字符串编译
var result = DynamicCompiler.CompileCode([
    "public static class Helper { public static int Twice(int x) => x * 2; }",
    "public static class MainClass { public static int Calc(int x) => Helper.Twice(x) + 1; }"
]);

// 或从多个文件编译
// var result = DynamicCompiler.CompileFromFiles(["Helper.cs", "Main.cs"]);
```

## API参考

### DynamicCompiler 类

#### 编译方法

- `CompilationResult CompileCode(string code)`: 编译C#静态类代码字符串
- `CompilationResult CompileCode(string[] codes)`: 编译多段C#静态类代码
- `CompilationResult CompileFromFile(string filePath)`: 从文件编译C#静态类代码
- `CompilationResult CompileFromFiles(string[] filePaths)`: 从多个文件编译C#静态类代码

#### 方法转换

- `Delegate ConvertToDelegate(MethodInfo method)`: 将方法转换为Delegate委托
- `TFunc ConvertToTypedFunc<TFunc>(MethodInfo method)`: 将方法转换为强类型Func委托
- `List<MethodInfo> GetPublicStaticMethods(Assembly? assembly)`: 获取程序集中的所有公共静态方法（不含属性访问器与运算符）

### CompilationResult 类

- `bool Success`: 编译是否成功
- `Assembly? Assembly`: 编译后的程序集（成功时）
- `string ErrorMessage`: 编译错误信息（失败时）

## 注意事项

1. 该库仅支持编译静态类代码
2. 编译后的代码在内存中执行，不会生成物理文件
3. 请确保编译的代码符合C#语法规范
4. 动态代码只能使用运行时可见的公开类型；编译结果通过 `Assembly.Load(byte[])` 加载到默认程序集加载上下文，与宿主类型统一
5. 动态编译产物以 Release 优化级别生成且不包含 PDB 符号，无法对动态代码进行源码级调试

## 贡献

欢迎提交Issue和Pull Request！

## 许可证

MIT许可证 - 详见LICENSE文件。

## 作者

Himmelt
