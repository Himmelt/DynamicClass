using System.Reflection;

namespace DynamicClass.Core {
    /// <summary>
    /// 动态编译工具类，用于编译C#静态类代码并将方法转换为Func委托（对外统一接口）
    /// </summary>
    public class DynamicCompiler {
        private readonly CodeAnalyzer _codeAnalyzer;
        private readonly Compiler _compiler;
        private readonly DelegateConverter _delegateConverter;
        private readonly MethodValidator _methodValidator;

        /// <summary>
        /// 初始化动态编译器
        /// </summary>
        public DynamicCompiler() {
            _codeAnalyzer = new CodeAnalyzer();
            _methodValidator = new MethodValidator();
            _compiler = new Compiler(_codeAnalyzer);
            _delegateConverter = new DelegateConverter(_methodValidator);
        }

        /// <summary>
        /// 编译C#静态类代码并返回编译结果
        /// </summary>
        /// <param name="code">要编译的C#静态类代码</param>
        /// <returns>编译结果，包含程序集和编译错误信息</returns>
        public CompilationResult CompileCode(string code) {
            return Compiler.CompileCode(code);
        }

        /// <summary>
        /// 从文本文件编译C#静态类代码并返回编译结果
        /// </summary>
        /// <param name="filePath">要编译的文本文件路径</param>
        /// <returns>编译结果，包含程序集和编译错误信息</returns>
        public CompilationResult CompileFromFile(string filePath) {
            return _compiler.CompileFromFile(filePath);
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
        /// 将方法转换为对应的Func委托
        /// </summary>
        /// <param name="method">要转换的方法信息</param>
        /// <returns>转换后的Func委托</returns>
        public Delegate ConvertToFuncDelegate(MethodInfo method) {
            return _delegateConverter.ConvertToFuncDelegate(method);
        }

        /// <summary>
        /// 将方法转换为强类型的Func委托（泛型版本）
        /// </summary>
        /// <typeparam name="TFunc">Func委托类型，如 Func<int, string>、Func<double, double, bool> 等</typeparam>
        /// <param name="method">要转换的方法信息</param>
        /// <returns>强类型的Func委托</returns>
        public TFunc ConvertToTypedFuncDelegate<TFunc>(MethodInfo method) where TFunc : Delegate {
            return _delegateConverter.ConvertToTypedFuncDelegate<TFunc>(method);
        }

        /// <summary>
        /// 验证生成的Func委托是否能够正确执行
        /// </summary>
        /// <param name="funcDelegate">要验证的Func委托</param>
        /// <param name="parameters">执行委托所需的参数</param>
        /// <returns>验证结果，包含执行结果和错误信息</returns>
        public static ValidationResult ValidateFuncDelegate(Delegate funcDelegate, params object[] parameters) {
            if (funcDelegate == null) {
                throw new ArgumentNullException(nameof(funcDelegate), "委托不能为空");
            }

            try {
                // 执行委托
                object? result = funcDelegate.DynamicInvoke(parameters);
                return new ValidationResult { Success = true, Result = result, ErrorMessage = string.Empty };
            } catch (Exception ex) {
                // 处理执行异常
                string errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new ValidationResult { Success = false, ErrorMessage = errorMessage, Result = null };
            }
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
