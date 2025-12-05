using DynamicClass.Models;
using System.Reflection;

namespace DynamicClass.Core {
    /// <summary>
    /// 方法验证器，负责验证方法是否适合转换为Func委托
    /// </summary>
    internal static class MethodValidator {
        /// <summary>
        /// 验证方法是否适合转换为Func委托
        /// </summary>
        /// <param name="method">要验证的方法</param>
        /// <returns>验证结果</returns>
        internal static MethodValidationResult ValidateMethodForFuncConversion(MethodInfo method) {
            // 检查方法是否必须为静态方法
            if (!method.IsStatic) {
                return MethodValidationResult.Failed("方法必须是静态方法");
            }

            // 检查返回类型
            if (!IsAllowedReturnType(method.ReturnType)) {
                return MethodValidationResult.Failed($"返回类型 {method.ReturnType.Name} 不被允许，只支持基础类型和string");
            }

            // 检查参数数量
            var parameters = method.GetParameters();
            if (parameters.Length > 16) {
                return MethodValidationResult.Failed($"参数数量超过16个限制，当前为 {parameters.Length} 个");
            }

            // 检查参数类型
            for (int i = 0; i < parameters.Length; i++) {
                if (!IsAllowedParameterType(parameters[i].ParameterType)) {
                    return MethodValidationResult.Failed($"参数 {parameters[i].Name} 类型 {parameters[i].ParameterType.Name} 不被允许，只支持基础类型和string");
                }
            }

            return MethodValidationResult.Success();
        }

        /// <summary>
        /// 检查类型是否为允许的参数类型
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <returns>是否为允许的类型</returns>
        internal static bool IsAllowedParameterType(Type type) {
            return IsAllowedBasicType(type) || type == typeof(string);
        }

        /// <summary>
        /// 检查类型是否为允许的返回类型
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <returns>是否为允许的类型</returns>
        internal static bool IsAllowedReturnType(Type type) {
            return IsAllowedBasicType(type) || type == typeof(string);
        }

        /// <summary>
        /// 检查类型是否为允许的基础类型
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <returns>是否为允许的基础类型</returns>
        internal static bool IsAllowedBasicType(Type type) {
            if (type.IsPrimitive) {
                return true; // bool, byte, char, short, int, long, float, double, decimal, nint, nuint
            }

            // 添加对DateTime和Guid的支持
            if (type == typeof(DateTime) || type == typeof(Guid) || type == typeof(TimeSpan)) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查类型是否为有效的Func委托类型
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <returns>是否为有效的Func委托类型</returns>
        internal static bool IsValidFuncDelegateType(Type type) {
            if (!typeof(Delegate).IsAssignableFrom(type)) {
                return false;
            }

            // 检查是否是Func委托类型
            if (type.Name.StartsWith("Func`")) {
                return true;
            }

            return false;
        }
    }
}
