using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins
{
    /// <summary>
    /// Provides the necessary data to apply a single Harmony patch.
    /// </summary>
    public class HPatchOperation
    {
        /// <summary>
        /// The target method which will be dynamically patched to use the stub method.
        /// </summary>
        public MethodInfo TargetMethod { get; set; }

        /// <summary>
        /// The stub method which will be appended or prepended to the target method.
        /// </summary>
        public MethodInfo StubMethod { get; set; }
        
        /// <summary>
        /// Whether the stub method will be patched as a prefix or postfix on the target method.
        /// </summary>
        public HPatchLocation PatchLocation { get; set; }
        

        /// <summary>
        /// Creates a new patch operation using the supplied target method, stub method, and patch location.
        /// </summary>
        /// <param name="targetMethod">The target method that will be patched.</param>
        /// <param name="stubMethod">The stub method that will be either prepended or appended to the target method.</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        public HPatchOperation(MethodInfo targetMethod, MethodInfo stubMethod, HPatchLocation patchLocation)
        {
            TargetMethod = targetMethod;
            StubMethod = stubMethod;
            PatchLocation = patchLocation;
        }

        /// <summary>
        /// Creates a new patch operation using the supplied target type & method name, stub method, and patch location.
        /// </summary>
        /// <param name="targetType">The target type that contains the target method.</param>
        /// <param name="targetMethodName">The name of the target method.</param>
        /// <param name="stubMethod">The stub method that will be either prepended or appended to the target method.</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        public HPatchOperation(Type targetType, string targetMethodName, MethodInfo stubMethod, HPatchLocation patchLocation)
        {
            MethodInfo targetMethod = targetType.GetMethods().Where(m => m.Name == targetMethodName).FirstOrDefault();
            if (targetMethodName == null)
                targetMethod = targetType.GetRuntimeMethods().Where(m => m.Name == targetMethodName).FirstOrDefault();
            if (targetMethodName == null)
                throw new Exception("Method name \"" + targetMethodName + "\" was not found in target type \"" + targetType.FullName + "\"");

            TargetMethod = targetMethod;
            StubMethod = stubMethod;
            PatchLocation = patchLocation;
        }
    }
}
