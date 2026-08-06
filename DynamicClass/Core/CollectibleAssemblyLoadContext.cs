using System.Reflection;
using System.Runtime.Loader;

namespace DynamicClass.Core {
    /// <summary>
    /// 可回收的程序集加载上下文。
    /// 动态编译产物加载到此上下文中，调用 <see cref="AssemblyLoadContext.Unload"/> 即可卸载，
    /// 前提是宿主已断开对上下文内所有 Type/MethodInfo/Delegate/实例的强引用。
    /// </summary>
    internal sealed class CollectibleAssemblyLoadContext : AssemblyLoadContext {
        public CollectibleAssemblyLoadContext() : base(isCollectible: true) {
        }

        protected override Assembly? Load(AssemblyName assemblyName) {
            return null;
        }
    }
}
