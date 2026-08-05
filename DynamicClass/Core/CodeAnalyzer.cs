using Microsoft.CodeAnalysis;
using System.Runtime.Loader;

namespace DynamicClass.Core {
    /// <summary>
    /// 代码分析器：编译引用 = 运行时可见世界。
    /// 枚举运行时实际能解析的全部程序集，让编译器看到的类型宇宙与运行时一致。
    /// </summary>
    internal static class CodeAnalyzer {
        /// <summary>
        /// 运行时闭包在进程生命周期内不变，构建一次后缓存。
        /// </summary>
        private static readonly Lazy<MetadataReference[]> CachedReferences = new(BuildRuntimeReferences);

        /// <summary>
        /// 返回运行时闭包的全部程序集引用。
        /// </summary>
        internal static MetadataReference[] GetRequiredReferences() {
            return CachedReferences.Value;
        }

        private static MetadataReference[] BuildRuntimeReferences() {
            var refs = new List<MetadataReference>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(string path) {
                try {
                    var name = Path.GetFileNameWithoutExtension(path);
                    if (seen.Add(name)) {
                        refs.Add(MetadataReference.CreateFromFile(path));
                    }
                } catch {
                    // 单个坏引用（如原生 DLL）不阻断整体
                }
            }

            // 1) 运行时框架程序集
            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpa) {
                foreach (var p in tpa.Split(Path.PathSeparator)) Add(p);
            }
            // 2) 应用目录（已声明依赖及未列入依赖清单的散装 DLL）
            if (Directory.Exists(AppContext.BaseDirectory)) {
                foreach (var p in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll")) Add(p);
            }
            // 3) 已加载程序集兜底（覆盖不在上述两处的程序集）
            foreach (var asm in AssemblyLoadContext.Default.Assemblies) {
                if (!string.IsNullOrEmpty(asm.Location)) Add(asm.Location);
            }
            return [.. refs];
        }
    }
}
