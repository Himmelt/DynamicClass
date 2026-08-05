using DynamicClass.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;

namespace DynamicClass.Core {
    /// <summary>
    /// 动态编译工具类：编译C#静态类代码，并将方法转换为 Func 委托（对外统一接口）
    /// </summary>
    public static class DynamicCompiler {
        /// <summary>
        /// 编译C#静态类代码并返回编译结果
        /// </summary>
        /// <param name="code">要编译的C#静态类代码</param>
        /// <returns>编译结果，包含程序集和编译错误信息</returns>
        public static CompilationResult CompileCode(string code) {
            if (string.IsNullOrWhiteSpace(code)) {
                throw new ArgumentNullException(nameof(code), "代码不能为空");
            }

            // 创建语法树
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

            // 运行时闭包引用
            var references = CodeAnalyzer.GetRequiredReferences();

            // 创建编译
            CSharpCompilation compilation = CSharpCompilation.Create(
                Path.GetRandomFileName(),
                syntaxTrees: [syntaxTree],
                references: references,
                options: CreateCompilationOptions());

            return EmitCompilation(compilation);
        }

        /// <summary>
        /// 编译多个C#静态类代码并返回编译结果
        /// </summary>
        /// <param name="codes">要编译的C#静态类代码数组</param>
        /// <returns>编译结果，包含程序集和编译错误信息</returns>
        public static CompilationResult CompileCode(string[] codes) {
            if (codes == null || codes.Length == 0) {
                throw new ArgumentException("代码数组不能为空", nameof(codes));
            }

            foreach (var code in codes) {
                if (string.IsNullOrWhiteSpace(code)) {
                    throw new ArgumentException("代码数组不能包含空元素", nameof(codes));
                }
            }

            var syntaxTrees = new List<SyntaxTree>();
            foreach (var code in codes) {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(code));
            }

            CSharpCompilation compilation = CSharpCompilation.Create(
                Path.GetRandomFileName(),
                syntaxTrees: syntaxTrees,
                references: CodeAnalyzer.GetRequiredReferences(),
                options: CreateCompilationOptions());

            return EmitCompilation(compilation);
        }

        /// <summary>
        /// 从文本文件编译C#静态类代码并返回编译结果
        /// </summary>
        /// <param name="filePath">要编译的文本文件路径</param>
        /// <returns>编译结果，包含程序集和编译错误信息</returns>
        public static CompilationResult CompileFromFile(string filePath) {
            if (string.IsNullOrWhiteSpace(filePath)) {
                throw new ArgumentNullException(nameof(filePath), "文件路径不能为空");
            }

            if (!File.Exists(filePath)) {
                throw new FileNotFoundException("指定的文件不存在", filePath);
            }

            return CompileCode(File.ReadAllText(filePath));
        }

        /// <summary>
        /// 从多个文本文件编译C#静态类代码并返回编译结果
        /// </summary>
        /// <param name="filePaths">要编译的文本文件路径数组</param>
        /// <returns>编译结果，包含程序集和编译错误信息</returns>
        public static CompilationResult CompileFromFiles(string[] filePaths) {
            if (filePaths == null || filePaths.Length == 0) {
                throw new ArgumentException("文件路径数组不能为空", nameof(filePaths));
            }

            foreach (var filePath in filePaths) {
                if (string.IsNullOrWhiteSpace(filePath)) {
                    throw new ArgumentException("文件路径数组不能包含空路径", nameof(filePaths));
                }

                if (!File.Exists(filePath)) {
                    throw new FileNotFoundException("指定的文件不存在", filePath);
                }
            }

            var codes = new List<string>();
            foreach (var filePath in filePaths) {
                codes.Add(File.ReadAllText(filePath));
            }

            return CompileCode([.. codes]);
        }

        /// <summary>
        /// 从编译后的程序集中获取所有公共静态方法（不含属性访问器与运算符）
        /// </summary>
        /// <param name="assembly">编译后的程序集</param>
        /// <returns>公共静态方法列表</returns>
        public static List<MethodInfo> GetPublicStaticMethods(Assembly? assembly) {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly), "程序集不能为空");
            }

            var methods = new List<MethodInfo>();
            foreach (Type type in assembly.GetTypes()) {
                MethodInfo[] typeMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                // 排除属性访问器（get_X/set_X）与运算符方法（op_*）
                methods.AddRange(typeMethods.Where(m => !m.IsSpecialName));
            }

            return methods;
        }

        /// <summary>
        /// 将方法转换为 Delegate 委托
        /// </summary>
        /// <param name="method">要转换的方法信息</param>
        /// <returns>转换后的 Delegate 委托</returns>
        public static Delegate ConvertToDelegate(MethodInfo method) {
            return DelegateConverter.ConvertToDelegate(method);
        }

        /// <summary>
        /// 将方法转换为强类型的 Func&lt;&gt; 委托
        /// </summary>
        /// <typeparam name="TFunc">Func&lt;&gt; 委托类型，如 Func&lt;int, string&gt;、Func&lt;double, double, bool&gt; 等</typeparam>
        /// <param name="method">要转换的方法信息</param>
        /// <returns>强类型的 Func&lt;&gt; 委托</returns>
        public static TFunc ConvertToTypedFunc<TFunc>(MethodInfo method) where TFunc : Delegate {
            return DelegateConverter.ConvertToTypedFunc<TFunc>(method);
        }

        private static CSharpCompilationOptions CreateCompilationOptions() {
            return new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
        }

        private static CompilationResult EmitCompilation(CSharpCompilation compilation) {
            using var ms = new MemoryStream();
            EmitResult result = compilation.Emit(ms);

            if (result.Success) {
                Assembly assembly = Assembly.Load(ms.ToArray());
                return new CompilationResult { Assembly = assembly, Success = true, ErrorMessage = string.Empty };
            } else {
                var errors = new System.Text.StringBuilder();
                foreach (Diagnostic diagnostic in result.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)) {
                    errors.AppendLine($"Error ({diagnostic.Id}): {diagnostic.GetMessage()} at line {diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}");
                }
                return new CompilationResult { Success = false, ErrorMessage = errors.ToString(), Assembly = null };
            }
        }
    }
}
