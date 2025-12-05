namespace DynamicClass.Models {
    /// <summary>
    /// 验证结果类
    /// </summary>
    public class ValidationResult {
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
