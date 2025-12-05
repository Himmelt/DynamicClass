using System.Reflection;

namespace DynamicClass.Core {
    /// <summary>
    /// 委托转换器，负责将方法信息转换为Func委托
    /// </summary>
    /// <remarks>
    /// 初始化委托转换器
    /// </remarks>
    /// <param name="methodValidator">方法验证器实例</param>
    internal class DelegateConverter(MethodValidator methodValidator) {
        private readonly MethodValidator _methodValidator = methodValidator ?? throw new ArgumentNullException(nameof(methodValidator));

        /// <summary>
        /// 将方法转换为对应的Func委托
        /// </summary>
        /// <param name="method">要转换的方法信息</param>
        /// <returns>转换后的Func委托</returns>
        internal Delegate ConvertToFuncDelegate(MethodInfo method) {
            if (method == null) {
                throw new ArgumentNullException(nameof(method), "方法信息不能为空");
            }

            // 验证方法是否适合转换为Func委托
            var validation = MethodValidator.ValidateMethodForFuncConversion(method);
            if (!validation.IsValid) {
                throw new ArgumentException($"方法 {method.Name} 不适合转换为Func委托: {validation.ErrorMessage}");
            }

            // 获取方法的参数类型和返回类型
            Type[] parameterTypes = [.. method.GetParameters().Select(p => p.ParameterType)];
            Type returnType = method.ReturnType;

            // 构建Func委托类型
            Type? funcType;
            if (parameterTypes.Length == 0) {
                // Func<TResult>
                funcType = typeof(Func<>).MakeGenericType(returnType);
            } else {
                // Func<T1, T2, ..., TResult>
                Type[] funcTypeArgs = [.. parameterTypes, .. new[] { returnType }];
                funcType = GetFuncType(funcTypeArgs.Length);
                if (funcType == null) {
                    throw new NotSupportedException($"不支持超过16个参数的方法转换: {method.Name}");
                }
                funcType = funcType.MakeGenericType(funcTypeArgs);
            }

            // 创建委托
            return Delegate.CreateDelegate(funcType, method);
        }

        /// <summary>
        /// 将方法转换为强类型的Func委托（泛型版本）
        /// </summary>
        /// <typeparam name="TFunc">Func委托类型，如 Func<int, string>、Func<double, double, bool> 等</typeparam>
        /// <param name="method">要转换的方法信息</param>
        /// <returns>强类型的Func委托</returns>
        internal TFunc ConvertToTypedFuncDelegate<TFunc>(MethodInfo method) where TFunc : Delegate {
            if (method == null) {
                throw new ArgumentNullException(nameof(method), "方法信息不能为空");
            }

            // 验证方法是否适合转换为Func委托
            var validation = MethodValidator.ValidateMethodForFuncConversion(method);
            if (!validation.IsValid) {
                throw new ArgumentException($"方法 {method.Name} 不适合转换为Func委托: {validation.ErrorMessage}");
            }

            // 验证TFunc是否是有效的Func委托类型
            if (!MethodValidator.IsValidFuncDelegateType(typeof(TFunc))) {
                throw new ArgumentException($"泛型类型 TFunc 必须是有效的Func委托类型");
            }

            // 创建委托并强类型返回
            Delegate delegateInstance = ConvertToFuncDelegate(method);

            // 尝试转换为强类型
            TFunc? typedDelegate = delegateInstance as TFunc ?? throw new InvalidCastException($"无法将类型为 {delegateInstance.GetType().Name} 的委托转换为 TFunc 类型");
            return typedDelegate;
        }

        /// <summary>
        /// 获取对应的Func委托类型
        /// </summary>
        /// <param name="genericArgumentCount">泛型参数数量（参数数量+1）</param>
        /// <returns>Func委托类型</returns>
        internal static Type? GetFuncType(int genericArgumentCount) {
            return genericArgumentCount switch {
                1 => typeof(Func<>),
                2 => typeof(Func<,>),
                3 => typeof(Func<,,>),
                4 => typeof(Func<,,,>),
                5 => typeof(Func<,,,,>),
                6 => typeof(Func<,,,,,,>),
                7 => typeof(Func<,,,,,,>),
                8 => typeof(Func<,,,,,,,>),
                9 => typeof(Func<,,,,,,,,>),
                10 => typeof(Func<,,,,,,,,,>),
                11 => typeof(Func<,,,,,,,,,,>),
                12 => typeof(Func<,,,,,,,,,,,>),
                13 => typeof(Func<,,,,,,,,,,,,>),
                14 => typeof(Func<,,,,,,,,,,,,,>),
                15 => typeof(Func<,,,,,,,,,,,,,,>),
                16 => typeof(Func<,,,,,,,,,,,,,,,>),
                17 => typeof(Func<,,,,,,,,,,,,,,,,>),
                _ => null,
            };
        }
    }
}
