using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// Provides the necessary data to apply a single Harmony patch.
    /// </summary>
    public class HPatchOperation
    {
        /// <summary>
        /// The target method which will be dynamically patched to use the stub method.
        /// </summary>
        public MethodBase TargetMethod { get; private set; }

        /// <summary>
        /// The stub method which will be appended or prepended to the target method.
        /// </summary>
        public MethodInfo StubMethod { get; private set; }
        
        /// <summary>
        /// Whether the stub method will be patched as a prefix or postfix on the target method.
        /// </summary>
        public HPatchLocation PatchLocation { get; private set; }

        /// <summary>
        /// The priority of the patched stub method. Higher numbers go first. If -1, the default priority will be used (typically 400).
        /// </summary>
        public int PatchPriority { get; private set; } = -1;
        

        /// <summary>
        /// Creates a new patch operation using the supplied target method, stub method, and patch location.
        /// </summary>
        /// <param name="targetMethod">The target method or constructor that will be patched.</param>
        /// <param name="stubMethod">The stub method that will be either prepended or appended to the target method. Must be a static method!</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        /// <param name="patchPriority">The priority of this patch, as used by Harmony to order multiple patches on the same method. Patches with higher numbers go first. Set to -1 to use default priority (typically = 400).</param>
        public HPatchOperation(MethodBase targetMethod, MethodInfo stubMethod, HPatchLocation patchLocation, int patchPriority = -1)
        {
            TargetMethod = targetMethod;
            StubMethod = stubMethod;
            PatchLocation = patchLocation;
            PatchPriority = patchPriority;
        }

        /// <summary>
        /// Creates a new patch operation using the supplied target type &amp; method name, stub method, and patch location.
        /// </summary>
        /// <param name="targetType">The target type that contains the target method.</param>
        /// <param name="targetMethodName">The name of the target method.</param>
        /// <param name="stubMethod">The stub method that will be either prepended or appended to the target method. Must be a static method!</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        /// <param name="patchPriority">The priority of this patch, as used by Harmony to order multiple patches on the same method. Patches with higher numbers go first. Set to -1 to use default priority (typically = 400).</param>
        public HPatchOperation(Type targetType, string targetMethodName, MethodInfo stubMethod, HPatchLocation patchLocation, int patchPriority = -1)
        {
            MethodBase targetMethod = targetType.GetRuntimeMethods().Where(m => m.Name == targetMethodName).FirstOrDefault(); // Search regular methods first
            if (targetMethod == null) // Search constructors next if nothing was found
                targetMethod = targetType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Where(m => m.Name == targetMethodName).FirstOrDefault();
            if (targetMethod == null)
                throw new Exception("No method or constructor with the name \"" + targetMethodName + "\" was found in target type \"" + targetType.FullName + "\"");

            TargetMethod = targetMethod;
            StubMethod = stubMethod;
            PatchLocation = patchLocation;
            PatchPriority = patchPriority;
        }
    }
}
