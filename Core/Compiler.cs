using DynamicClass.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;

namespace DynamicClass.Core {
    /// <summary>
    /// 动态编译器，负责执行实际的代码编译操作
    /// </summary>
    /// <remarks>
    /// 初始化编译器
    /// </remarks>
    /// <param name="codeAnalyzer">代码分析器实例</param>
    internal static class Compiler {
        /// <summary>
        /// 编译C#静态类代码并返回编译结果
        /// </summary>
        /// <param name="code">要编译的C#静态类代码</param>
        /// <returns>编译结果，包含程序集和编译错误信息</returns>
        internal static CompilationResult CompileCode(string code) {
            if (string.IsNullOrWhiteSpace(code)) {
                throw new ArgumentNullException(nameof(code), "代码不能为空");
            }

            // 创建语法树
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

            // 定义程序集名称
            string assemblyName = Path.GetRandomFileName();

            // 智能分析代码并获取所需的程序集引用
            var references = CodeAnalyzer.GetRequiredReferences(code);

            // 创建编译选项
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Debug,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);

            // 创建编译
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: [syntaxTree],
                references: references,
                options: compilationOptions);

            // 编译到内存流
            return EmitCompilation(compilation);
        }

        /// <summary>
        /// 编译语法树并返回结果
        /// </summary>
        /// <param name="compilation">编译对象</param>
        /// <returns>编译结果</returns>
        private static CompilationResult EmitCompilation(CSharpCompilation compilation) {
            using var ms = new MemoryStream();
            EmitResult result = compilation.Emit(ms);

            if (result.Success) {
                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());
                return new CompilationResult { Assembly = assembly, Success = true, ErrorMessage = string.Empty };
            } else {
                // 收集编译错误
                var errors = new System.Text.StringBuilder();
                foreach (Diagnostic diagnostic in result.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)) {
                    errors.AppendLine($"Error ({diagnostic.Id}): {diagnostic.GetMessage()} at line {diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}");
                }
                return new CompilationResult { Success = false, ErrorMessage = errors.ToString(), Assembly = null };
            }
        }

        /// <summary>
        /// 从文本文件编译代码
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>编译结果</returns>
        internal static CompilationResult CompileFromFile(string filePath) {
            if (string.IsNullOrWhiteSpace(filePath)) {
                throw new ArgumentNullException(nameof(filePath), "文件路径不能为空");
            }

            if (!File.Exists(filePath)) {
                throw new FileNotFoundException("指定的文件不存在", filePath);
            }

            // 读取文件内容
            string code = File.ReadAllText(filePath);

            // 调用字符串编译方法
            return CompileCode(code);
        }
    }
}
