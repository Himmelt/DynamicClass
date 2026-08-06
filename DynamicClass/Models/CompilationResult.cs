using System.Reflection;
using System.Runtime.Loader;

namespace DynamicClass.Models {
    /// <summary>
    /// 编译结果类
    /// </summary>
    public class CompilationResult {
        /// <summary>
        /// 编译是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 编译后的程序集
        /// </summary>
        public Assembly? Assembly { get; set; }

        /// <summary>
        /// 加载 <see cref="Assembly"/> 所用的可回收程序集加载上下文。
        /// <para>调用方应在重新编译前断开对旧程序集内 Type/MethodInfo/Delegate/实例的全部强引用，
        /// 然后调用 <see cref="AssemblyLoadContext.Unload"/> 释放旧程序集占用的内存。</para>
        /// <para>编译失败时为 <see langword="null"/>。</para>
        /// </summary>
        public AssemblyLoadContext? LoadContext { get; set; }

        /// <summary>
        /// 编译错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
