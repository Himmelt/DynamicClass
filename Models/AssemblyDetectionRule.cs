namespace DynamicClass.Models {
    /// <summary>
    /// 程序集检测规则类
    /// </summary>
    /// <remarks>
    /// 初始化程序集检测规则
    /// </remarks>
    /// <param name="assemblyName">程序集名称</param>
    /// <param name="detectionFunction">检测函数</param>
    public class AssemblyDetectionRule(string assemblyName, Func<string, bool> detectionFunction) {
        /// <summary>
        /// 程序集名称
        /// </summary>
        public string AssemblyName { get; } = assemblyName;

        /// <summary>
        /// 检测函数，用于确定是否需要该程序集
        /// </summary>
        public Func<string, bool> DetectionFunction { get; } = detectionFunction;
    }
}
