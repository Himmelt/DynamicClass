namespace DynamicClass.Models {
    /// <summary>
    /// 方法验证结果类
    /// </summary>
    public class MethodValidationResult {
        /// <summary>
        /// 验证是否成功
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; private set; } = string.Empty;

        private MethodValidationResult(bool isValid, string errorMessage) {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        /// <returns>成功的结果对象</returns>
        public static MethodValidationResult Success() {
            return new MethodValidationResult(true, string.Empty);
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>失败的结果对象</returns>
        public static MethodValidationResult Failed(string errorMessage) {
            return new MethodValidationResult(false, errorMessage);
        }
    }
}
