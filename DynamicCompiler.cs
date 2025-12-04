using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DynamicClass
{
    /// <summary>
    /// 动态编译工具类，用于编译C#静态类代码并将方法转换为Func委托
    /// </summary>
    public class DynamicCompiler
    {
        /// <summary>
        /// 编译C#静态类代码并返回编译结果
        /// </summary>
        /// <param name="code">要编译的C#静态类代码</param>
        /// <returns>编译结果，包含程序集和编译错误信息</returns>
        public CompilationResult CompileCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException(nameof(code), "代码不能为空");
            }

            // 创建语法树
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

            // 定义程序集名称
            string assemblyName = Path.GetRandomFileName();

            // 引用的程序集
            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System").Location)
            };

            // 创建编译选项
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Debug,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);

            // 创建编译
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: compilationOptions);

            // 编译到内存流
            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());
                    return new CompilationResult { Assembly = assembly, Success = true, ErrorMessage = string.Empty };
                }
                else
                {
                    // 收集编译错误
                    var errors = new StringBuilder();
                    foreach (Diagnostic diagnostic in result.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error))
                    {
                        errors.AppendLine($"Error ({diagnostic.Id}): {diagnostic.GetMessage()} at line {diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}");
                    }
                    return new CompilationResult { Success = false, ErrorMessage = errors.ToString(), Assembly = null };
                }
            }
        }

        /// <summary>
        /// 从编译后的程序集中获取所有公共静态方法
        /// </summary>
        /// <param name="assembly">编译后的程序集</param>
        /// <returns>公共静态方法列表</returns>
        public List<MethodInfo> GetPublicStaticMethods(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly), "程序集不能为空");
            }

            var methods = new List<MethodInfo>();

            // 获取所有类型
            foreach (Type type in assembly.GetTypes())
            {
                // 获取所有公共静态方法
                MethodInfo[] typeMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                methods.AddRange(typeMethods);
            }

            return methods;
        }

        /// <summary>
        /// 将方法转换为对应的Func委托
        /// </summary>
        /// <param name="method">要转换的方法信息</param>
        /// <returns>转换后的Func委托</returns>
        public Delegate ConvertToFuncDelegate(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method), "方法信息不能为空");
            }

            // 获取方法的参数类型和返回类型
            Type[] parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
            Type returnType = method.ReturnType;

            // 构建Func委托类型
            Type funcType;
            if (parameterTypes.Length == 0)
            {
                // Func<TResult>
                funcType = typeof(Func<>).MakeGenericType(returnType);
            }
            else
            {
                // Func<T1, T2, ..., TResult>
                Type[] funcTypeArgs = parameterTypes.Concat(new[] { returnType }).ToArray();
                funcType = GetFuncType(funcTypeArgs.Length);
                if (funcType == null)
                {
                    throw new NotSupportedException($"不支持超过16个参数的方法转换: {method.Name}");
                }
                funcType = funcType.MakeGenericType(funcTypeArgs);
            }

            // 创建委托
            return Delegate.CreateDelegate(funcType, method);
        }

        /// <summary>
        /// 验证生成的Func委托是否能够正确执行
        /// </summary>
        /// <param name="funcDelegate">要验证的Func委托</param>
        /// <param name="parameters">执行委托所需的参数</param>
        /// <returns>验证结果，包含执行结果和错误信息</returns>
        public ValidationResult ValidateFuncDelegate(Delegate funcDelegate, params object[] parameters)
        {
            if (funcDelegate == null)
            {
                throw new ArgumentNullException(nameof(funcDelegate), "委托不能为空");
            }

            try
            {
                // 执行委托
                object? result = funcDelegate.DynamicInvoke(parameters);
                return new ValidationResult { Success = true, Result = result, ErrorMessage = string.Empty };
            }
            catch (Exception ex)
            {
                // 处理执行异常
                string errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new ValidationResult { Success = false, ErrorMessage = errorMessage, Result = null };
            }
        }

        /// <summary>
        /// 获取对应的Func委托类型
        /// </summary>
        /// <param name="genericArgumentCount">泛型参数数量（参数数量+1）</param>
        /// <returns>Func委托类型</returns>
        private Type? GetFuncType(int genericArgumentCount)
        {
            switch (genericArgumentCount)
            {
                case 1: return typeof(Func<>);
                case 2: return typeof(Func<,>);
                case 3: return typeof(Func<,,>);
                case 4: return typeof(Func<,,,>);
                case 5: return typeof(Func<,,,,>);
                case 6: return typeof(Func<,,,,,>);
                case 7: return typeof(Func<,,,,,,>);
                case 8: return typeof(Func<,,,,,,,>);
                case 9: return typeof(Func<,,,,,,,,>);
                case 10: return typeof(Func<,,,,,,,,,>);
                case 11: return typeof(Func<,,,,,,,,,,>);
                case 12: return typeof(Func<,,,,,,,,,,,>);
                case 13: return typeof(Func<,,,,,,,,,,,,>);
                case 14: return typeof(Func<,,,,,,,,,,,,,>);
                case 15: return typeof(Func<,,,,,,,,,,,,,,>);
                case 16: return typeof(Func<,,,,,,,,,,,,,,,>);
                case 17: return typeof(Func<,,,,,,,,,,,,,,,,>);
                default: return null;
            }
        }
    }

    /// <summary>
    /// 编译结果类
    /// </summary>
    public class CompilationResult
    {
        /// <summary>
        /// 编译是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 编译后的程序集
        /// </summary>
        public Assembly? Assembly { get; set; }

        /// <summary>
        /// 编译错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 验证结果类
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 验证是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 执行结果
        /// </summary>
        public object? Result { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}