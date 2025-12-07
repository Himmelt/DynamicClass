using System.Linq.Expressions;
using System.Reflection;

namespace DynamicClass.Core {
    /// <summary>
    /// 委托转换器，负责将方法信息转换为Func委托
    /// </summary>
    /// <remarks>
    /// 初始化委托转换器
    /// </remarks>
    /// <param name="methodValidator">方法验证器实例</param>
    internal static class DelegateConverter {
        /// <summary>
        /// 将方法转换为 Delegate 委托
        /// </summary>
        /// <param name="method">要转换的方法信息</param>
        /// <returns>转换后的 Delegate 委托</returns>
        internal static Delegate ConvertToDelegate(MethodInfo method) {
            if (method == null) {
                throw new ArgumentNullException(nameof(method), "方法信息不能为空");
            }

            // 获取方法的参数类型和返回类型
            Type[] parameterTypes = [.. method.GetParameters().Select(p => p.ParameterType)];
            Type returnType = method.ReturnType;

            // 构建委托类型
            Type delegateType;
            if (parameterTypes.Length == 0) {
                // 参数为0的情况
                delegateType = Expression.GetDelegateType([returnType]);
            } else {
                // 有参数的情况，将参数类型和返回类型合并
                delegateType = Expression.GetDelegateType([.. parameterTypes, returnType]);
            }

            // 创建委托
            return method.CreateDelegate(delegateType);
        }

        /// <summary>
        /// 将方法转换为强类型的 Func<> 委托
        /// </summary>
        /// <typeparam name="TFunc">Func<> 委托类型，如 Func<int, string>、Func<double, double, bool> 等</typeparam>
        /// <param name="method">要转换的方法信息</param>
        /// <returns>强类型的 Func<> 委托</returns>
        internal static TFunc ConvertToTypedFunc<TFunc>(MethodInfo method) where TFunc : Delegate {
            if (method == null) {
                throw new ArgumentNullException(nameof(method), "方法信息不能为空");
            }

            // 直接创建委托并转换为强类型，不进行额外验证
            // 使用method.CreateDelegate直接创建TFunc类型的委托，这种方式会自动处理类型转换
            return method.CreateDelegate<TFunc>() ?? throw new InvalidCastException($"无法将方法 {method.Name} 转换为 {typeof(TFunc).Name} 类型的委托");
        }
    }
}
