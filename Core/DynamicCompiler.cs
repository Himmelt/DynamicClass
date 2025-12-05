using DynamicClass.Models;
using System.Reflection;

namespace DynamicClass.Core {
    /// <summary>
    /// 动态编译工具类，用于编译C#静态类代码并将方法转换为Func委托（对外统一接口）
    /// </summary>
    public static class DynamicCompiler {
        /// <summary>
        /// 编译C#静态类代码并返回编译结果
        /// </summary>
        /// <param name="code">要编译的C#静态类代码</param>
        /// <returns>编译结果，包含程序集和编译错误信息</returns>
        public static CompilationResult CompileCode(string code) {
            return Compiler.CompileCode(code);
        }

        /// <summary>
        /// 从文本文件编译C#静态类代码并返回编译结果
        /// </summary>
        /// <param name="filePath">要编译的文本文件路径</param>
        /// <returns>编译结果，包含程序集和编译错误信息</returns>
        public static CompilationResult CompileFromFile(string filePath) {
            return Compiler.CompileFromFile(filePath);
        }

        /// <summary>
        /// 从编译后的程序集中获取所有公共静态方法
        /// </summary>
        /// <param name="assembly">编译后的程序集</param>
        /// <returns>公共静态方法列表</returns>
        public static List<MethodInfo> GetPublicStaticMethods(Assembly assembly) {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly), "程序集不能为空");
            }

            var methods = new List<MethodInfo>();

            // 获取所有类型
            foreach (Type type in assembly.GetTypes()) {
                // 获取所有公共静态方法
                MethodInfo[] typeMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                methods.AddRange(typeMethods);
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
        /// 将方法转换为强类型的 Func<> 委托
        /// </summary>
        /// <typeparam name="TFunc">Func<> 委托类型，如 Func<int, string>、Func<double, double, bool> 等</typeparam>
        /// <param name="method">要转换的方法信息</param>
        /// <returns>强类型的 Func<> 委托</returns>
        public static TFunc ConvertToTypedFunc<TFunc>(MethodInfo method) where TFunc : Delegate {
            return DelegateConverter.ConvertToTypedFunc<TFunc>(method);
        }

        /// <summary>
        /// 动态扩展检测规则的方法
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <param name="namespacePattern">命名空间模式</param>
        /// <param name="typePatterns">类型模式</param>
        public static void RegisterAssemblyRule(string assemblyName, string namespacePattern, params string[] typePatterns) {
            CodeAnalyzer.RegisterAssemblyRule(assemblyName, namespacePattern, typePatterns);
        }
    }
}
