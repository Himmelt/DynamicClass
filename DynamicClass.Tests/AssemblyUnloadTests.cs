using DynamicClass.Core;
using System.Reflection;
using System.Runtime.Loader;

namespace DynamicClass.Tests {
    public class AssemblyUnloadTests {
        /// <summary>
        /// 编译成功时 LoadContext 不为空，且是可回收的。
        /// </summary>
        [Fact]
        public void CompileCode_Success_LoadContextIsCollectible() {
            string code = "public static class C { public static int Add(int a, int b) => a + b; }";

            var result = DynamicCompiler.CompileCode(code);

            Assert.True(result.Success, result.ErrorMessage);
            Assert.NotNull(result.LoadContext);
            // 可回收 ALC 的 IsCollectible 为 true
            Assert.True(result.LoadContext!.IsCollectible);
        }

        /// <summary>
        /// 编译失败时 LoadContext 为 null。
        /// </summary>
        [Fact]
        public void CompileCode_Failure_LoadContextIsNull() {
            string invalidCode = "public static class C { public static int Add(int a, int b) => a + b }";

            var result = DynamicCompiler.CompileCode(invalidCode);

            Assert.False(result.Success);
            Assert.Null(result.LoadContext);
        }

        /// <summary>
        /// 卸载后，ALC 处于卸载中状态，后续加载操作会抛出 <see cref="InvalidOperationException"/>。
        /// <para>注意：<see cref="AssemblyLoadContext.Unload"/> 是异步的，真正的回收发生在 GC 回收所有强引用之后。
        /// 测试验证卸载后 ALC 的功能性变化（无法再加载新程序集），并释放引用以便 GC 回收。</para>
        /// </summary>
        [Fact]
        public void Unload_AfterRelease_MakesAssemblyInaccessible() {
            string code = "public static class C { public static int Add(int a, int b) => a + b; }";

            var result = DynamicCompiler.CompileCode(code);
            Assert.True(result.Success);

            AssemblyLoadContext? alc = result.LoadContext;
            Assert.NotNull(alc);

            // 卸载
            alc!.Unload();

            // 卸载后，在 ALC 中加载新程序集应抛出 InvalidOperationException（ALC 处于卸载中状态）
            using var ms2 = new MemoryStream(new byte[] { 0, 1, 2, 3 });
            Assert.Throws<InvalidOperationException>(() => alc.LoadFromStream(ms2));

            // 释放所有强引用，以便 GC 后续回收 ALC（回收是异步的，这里仅释放引用）
            result.Assembly = null;
            result.LoadContext = null;
            alc = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>
        /// 卸载后 IsCollectible 仍为 true（属性不随卸载改变），且 ALC 处于卸载中状态。
        /// <para>通过尝试在已卸载的 ALC 中加载新程序集来验证：调用 <see cref="AssemblyLoadContext.Unload"/>
        /// 后，<see cref="AssemblyLoadContext.LoadFromStream(Stream)"/> 会抛出
        /// <see cref="InvalidOperationException"/>。</para>
        /// </summary>
        [Fact]
        public void Unload_SetsIsUnloading() {
            string code = "public static class C { public static int F() => 1; }";

            var result = DynamicCompiler.CompileCode(code);
            var alc = result.LoadContext!;

            // IsCollectible 在卸载前为 true
            Assert.True(alc.IsCollectible);

            alc.Unload();

            // IsCollectible 不随卸载改变
            Assert.True(alc.IsCollectible);
            // 卸载后，在 ALC 中加载新程序集应抛出 InvalidOperationException（ALC 处于卸载中状态）
            using var ms2 = new MemoryStream(new byte[] { 0, 1, 2, 3 });
            Assert.Throws<InvalidOperationException>(() => alc.LoadFromStream(ms2));
        }

        /// <summary>
        /// 多次编译产生不同的 ALC，互不影响；卸载其中一个不影响另一个。
        /// </summary>
        [Fact]
        public void MultipleCompilations_ProduceIndependentLoadContexts() {
            string code1 = "public static class C1 { public static int F() => 1; }";
            string code2 = "public static class C2 { public static int F() => 2; }";

            var result1 = DynamicCompiler.CompileCode(code1);
            var result2 = DynamicCompiler.CompileCode(code2);

            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.NotSame(result1.LoadContext, result2.LoadContext);
            Assert.NotSame(result1.Assembly, result2.Assembly);

            // 卸载第一个
            result1.LoadContext!.Unload();
            // 卸载后，在第一个 ALC 中加载新程序集应抛出 InvalidOperationException
            using var ms2 = new MemoryStream(new byte[] { 0, 1, 2, 3 });
            Assert.Throws<InvalidOperationException>(() => result1.LoadContext!.LoadFromStream(ms2));

            // 第二个仍正常工作
            var methods = DynamicCompiler.GetPublicStaticMethods(result2.Assembly);
            Assert.Single(methods);
            Assert.Equal("F", methods[0].Name);
        }

        /// <summary>
        /// 委托在被强持有时，ALC 无法真正回收（验证引用持有的影响）。
        /// 这里仅验证：即使持有委托，Unload() 调用本身不会抛异常。
        /// </summary>
        [Fact]
        public void Unload_WithDelegateHeld_DoesNotThrow() {
            string code = "public static class C { public static int Add(int a, int b) => a + b; }";

            var result = DynamicCompiler.CompileCode(code);
            var addMethod = DynamicCompiler.GetPublicStaticMethods(result.Assembly)
                .First(m => m.Name == "Add");
            var del = DynamicCompiler.ConvertToDelegate(addMethod);

            // 持有委托的情况下调用 Unload 不会抛异常（实际回收要等委托释放后）
            result.LoadContext!.Unload();

            // 委托仍可调用（因为底层 Type 还被委托持有，未真正回收）
            int v = (int)del.DynamicInvoke(3, 4)!;
            Assert.Equal(7, v);
        }

        /// <summary>
        /// 断开委托引用并卸载后，ALC 进入卸载中状态，后续加载操作会抛出 <see cref="InvalidOperationException"/>。
        /// <para><see cref="AssemblyLoadContext.Unload"/> 是异步的，真正的回收发生在 GC 回收所有强引用之后。
        /// 持有委托会阻止 ALC 被回收；释放委托后 ALC 才能被回收。
        /// 但由于可回收 ALC 的卸载在后台线程异步进行，GC.Collect 后未必立即完成回收，
        /// 因此本测试仅断言卸载中状态，不断言 GC 回收结果。</para>
        /// </summary>
        [Fact]
        public void Unload_AfterReleasingDelegate_EntersUnloadingState() {
            string code = "public static class C { public static int Add(int a, int b) => a + b; }";

            var result = DynamicCompiler.CompileCode(code);
            var addMethod = DynamicCompiler.GetPublicStaticMethods(result.Assembly)
                .First(m => m.Name == "Add");

            // 转换为强类型委托并立即释放中间引用
            var del = DynamicCompiler.ConvertToTypedFunc<Func<int, int, int>>(addMethod);
            addMethod = null!;

            var alc = result.LoadContext!;

            // 卸载
            alc.Unload();

            // 卸载后，在 ALC 中加载新程序集应抛出 InvalidOperationException（ALC 处于卸载中状态）
            using var ms2 = new MemoryStream(new byte[] { 0, 1, 2, 3 });
            Assert.Throws<InvalidOperationException>(() => alc.LoadFromStream(ms2));

            // 释放所有强引用（包括委托），以便 GC 后续回收 ALC（回收是异步的，此处不断言）
            del = null!;
            result.Assembly = null;
            result.LoadContext = null;
            alc = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
