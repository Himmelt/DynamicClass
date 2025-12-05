using DynamicClass.Models;
using System.Reflection;

namespace DynamicClass {
    /// <summary>
    /// 动态编译工具类，用于编译C#静态类代码并将方法转换为Func委托（对外统一接口，保持向后兼容）
    /// </summary>
    public static class DynamicCompiler {

        /// <summary>
        /// 编译C#静态类代码并返回编译结果
        /// </summary>
        /// <param name="code">要编译的C#静态类代码</param>
        /// <returns>编译结果，包含程序集和编译错误信息</returns>
        public static CompilationResult CompileCode(string code) {
            return Core.DynamicCompiler.CompileCode(code);
        }

        /// <summary>
        /// 从文本文件编译C#静态类代码并返回编译结果
        /// </summary>
        /// <param name="filePath">要编译的文本文件路径</param>
        /// <returns>编译结果，包含程序集和编译错误信息</returns>
        public static CompilationResult CompileFromFile(string filePath) {
            return Core.DynamicCompiler.CompileFromFile(filePath);
        }

        /// <summary>
        /// 从编译后的程序集中获取所有公共静态方法
        /// </summary>
        /// <param name="assembly">编译后的程序集</param>
        /// <returns>公共静态方法列表</returns>
        public static List<MethodInfo> GetPublicStaticMethods(Assembly assembly) {
            return Core.DynamicCompiler.GetPublicStaticMethods(assembly);
        }

        /// <summary>
        /// 将方法转换为对应的Func委托
        /// </summary>
        /// <param name="method">要转换的方法信息</param>
        /// <returns>转换后的Func委托</returns>
        public static Delegate ConvertToFuncDelegate(MethodInfo method) {
            return Core.DynamicCompiler.ConvertToFuncDelegate(method);
        }

        /// <summary>
        /// 将方法转换为强类型的Func委托（泛型版本）
        /// </summary>
        /// <typeparam name="TFunc">Func委托类型，如 Func<int, string>、Func<double, double, bool> 等</typeparam>
        /// <param name="method">要转换的方法信息</param>
        /// <returns>强类型的Func委托</returns>
        public static TFunc ConvertToTypedFuncDelegate<TFunc>(MethodInfo method) where TFunc : Delegate {
            return Core.DynamicCompiler.ConvertToTypedFuncDelegate<TFunc>(method);
        }

        /// <summary>
        /// 验证生成的Func委托是否能够正确执行
        /// </summary>
        /// <param name="funcDelegate">要验证的Func委托</param>
        /// <param name="parameters">执行委托所需的参数</param>
        /// <returns>验证结果，包含执行结果和错误信息</returns>
        public static ValidationResult ValidateFuncDelegate(Delegate funcDelegate, params object[] parameters) {
            return Core.DynamicCompiler.ValidateFuncDelegate(funcDelegate, parameters);
        }

        /// <summary>
        /// 动态扩展检测规则的方法
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <param name="namespacePattern">命名空间模式</param>
        /// <param name="typePatterns">类型模式</param>
        public static void RegisterAssemblyRule(string assemblyName, string namespacePattern, params string[] typePatterns) {
            Core.DynamicCompiler.RegisterAssemblyRule(assemblyName, namespacePattern, typePatterns);
        }
    }
}
