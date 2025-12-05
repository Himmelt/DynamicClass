using System.Reflection;

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
        /// 编译错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
