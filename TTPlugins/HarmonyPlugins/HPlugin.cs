using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// The base class which provides the means to create a Harmony-based plugin.
    /// </summary>
    public class HPlugin
    {
        #region Properties

        /// <summary>
        /// An informational object which describes the identity of this plugin.
        /// This property is used to identify plugin savedata and must be unique.
        /// </summary>
        public HPluginIdentity Identity { get; private set; }

        /// <summary>
        /// The list of patch operations that constitute this HPlugin's functionality.
        /// </summary>
        public List<HPatchOperation> PatchOperations { get; private set; }

        /// <summary>
        /// Contains the plugin's pesistent savedata, which includes user preferences from savedata (if any).
        /// </summary>
        public HPluginConfiguration Configuration { get; private set; }

        /// <summary>
        /// Whether or not this plugin needs to write persistent savedata to disk (such as for user preferences).
        /// </summary>
        public bool HasPersistentSavedata { get; protected set; }

        /// <summary>
        /// The Assembly which contains this plugin. Can be used to get embedded resources and assembly attributes.
        /// </summary>
        protected Assembly PluginAssembly { get; private set; }

        #endregion


        #region Ctor

        /// <summary>
        /// Creates a new HPlugin with default values.
        /// </summary>
        public HPlugin()
        {
            Identity = new HPluginIdentity();
            PatchOperations = new List<HPatchOperation>();
            Configuration = null;
            HasPersistentSavedata = false;
        }

        #endregion


        #region Convenient HPatchOperation creators

        /// <summary>
        /// Creates a new patch operation using the supplied target method, stub method, and patch location.
        /// </summary>
        /// <param name="targetMethod">The target method (in Terraria) that will be patched.</param>
        /// <param name="stubMethod">The stub method (in your plugin) that will be either prepended or appended to the target method. Must be a static method!</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        /// <param name="patchPriority">The priority of this patch, as used by Harmony to order multiple patches on the same method. Patches with higher numbers go first. Set to -1 to use default priority (typically = 400).</param>
        protected void CreateHPatchOperation(MethodBase targetMethod, MethodInfo stubMethod, HPatchLocation patchLocation, int patchPriority = -1)
        {
            PatchOperations.Add(new HPatchOperation(targetMethod, stubMethod, patchLocation, patchPriority));
        }

        /// <summary>
        /// Creates a new patch operation using the supplied target type and method name, stub method, and patch location.
        /// </summary>
        /// <param name="targetType">The target type (in Terraria) that contains the target method.</param>
        /// <param name="targetMethodName">The name of the target method.</param>
        /// <param name="stubMethod">The stub method (in your plugin) that will be either prepended or appended to the target method. Must be a static method!</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        /// <param name="patchPriority">The priority of this patch, as used by Harmony to order multiple patches on the same method. Patches with higher numbers go first. Set to -1 to use default priority (typically = 400).</param>
        protected void CreateHPatchOperation(Type targetType, string targetMethodName, MethodInfo stubMethod, HPatchLocation patchLocation, int patchPriority = -1)
        {
            PatchOperations.Add(new HPatchOperation(targetType, targetMethodName, stubMethod, patchLocation, patchPriority));
        }

        /// <summary>
        /// Creates a new patch operation using the supplied target method, stub method name from this class, and patch location.
        /// </summary>
        /// <param name="targetMethod">The target method (in Terraria) that will be patched.</param>
        /// <param name="stubMethodName">The name of the stub method IN THIS CLASS that will be either prepended or appended to the target method. Must be a static method!</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        /// <param name="patchPriority">The priority of this patch, as used by Harmony to order multiple patches on the same method. Patches with higher numbers go first. Set to -1 to use default priority (typically = 400).</param>
        protected void CreateHPatchOperation(MethodBase targetMethod, string stubMethodName, HPatchLocation patchLocation, int patchPriority = -1)
        {
            MethodInfo stubMethod = this.GetType().GetRuntimeMethods().Where(x => x.Name == stubMethodName).FirstOrDefault();
            if (stubMethod == null)
                throw new Exception("Invalid stubMethodName. This type does not contain a method named \"" + stubMethodName + "\".");
            if (!stubMethod.Attributes.HasFlag(MethodAttributes.Static))
                throw new Exception("Invalid stub method. The stub method specified is not static.");

            CreateHPatchOperation(targetMethod, stubMethod, patchLocation, patchPriority);
        }

        /// <summary>
        /// Creates a new patch operation using the supplied target type and method name, stub method name, and patch location.
        /// </summary>
        /// <param name="targetType">The target type (in Terraria) that contains the target method.</param>
        /// <param name="targetMethodName">The name of the target method.</param>
        /// <param name="stubMethodName">The name of the stub method IN THIS CLASS that will be either prepended or appended to the target method. Must be a static method!</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        /// <param name="patchPriority">The priority of this patch, as used by Harmony to order multiple patches on the same method. Patches with higher numbers go first. Set to -1 to use default priority (typically = 400).</param>
        protected void CreateHPatchOperation(Type targetType, string targetMethodName, string stubMethodName, HPatchLocation patchLocation, int patchPriority = -1)
        {
            MethodInfo stubMethod = this.GetType().GetRuntimeMethods().Where(x => x.Name == stubMethodName).FirstOrDefault();
            if (stubMethod == null)
                throw new Exception("Invalid stubMethodName. This type does not contain a method named \"" + stubMethodName + "\".");
            if (!stubMethod.Attributes.HasFlag(MethodAttributes.Static))
                throw new Exception("Invalid stub method. The stub method specified is not static.");

            CreateHPatchOperation(targetType, targetMethodName, stubMethod, patchLocation, patchPriority);
        }

        /// <summary>
        /// Creates a new patch operation using the supplied target type name, target method name, stub method, and patch location.
        /// </summary>
        /// <param name="targetTypeFullName">The full name of target type (in Terraria) that contains the target method, e.g. "Terraria.Main".</param>
        /// <param name="targetMethodName">The name of the target method.</param>
        /// <param name="stubMethod">The stub method (in your plugin) that will be either prepended or appended to the target method. Must be a static method!</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        /// <param name="patchPriority">The priority of this patch, as used by Harmony to order multiple patches on the same method. Patches with higher numbers go first. Set to -1 to use default priority (typically = 400).</param>
        protected void CreateHPatchOperation(string targetTypeFullName, string targetMethodName, MethodInfo stubMethod, HPatchLocation patchLocation, int patchPriority = -1)
        {
            Type targetType = null;
            if (!HHelpers.TryGetTerrariaType(targetTypeFullName, out targetType))
                throw new Exception("Invalid targetTypeFullName. Terraria does not contain a type named \"" + targetTypeFullName + "\".");

            CreateHPatchOperation(targetType, targetMethodName, stubMethod, patchLocation, patchPriority);
        }

        /// <summary>
        /// Creates a new patch operation using the supplied target type name, target method name, stub method name, and patch location.
        /// </summary>
        /// <param name="targetTypeFullName">The full name of target type (in Terraria) that contains the target method, e.g. "Terraria.Main".</param>
        /// <param name="targetMethodName">The name of the target method.</param>
        /// <param name="stubMethodName">The name of the stub method IN THIS CLASS that will be either prepended or appended to the target method. Must be a static method!</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        /// <param name="patchPriority">The priority of this patch, as used by Harmony to order multiple patches on the same method. Patches with higher numbers go first. Set to -1 to use default priority (typically = 400).</param>
        protected void CreateHPatchOperation(string targetTypeFullName, string targetMethodName, string stubMethodName, HPatchLocation patchLocation, int patchPriority = -1)
        {
            Type targetType = null;
            if (!HHelpers.TryGetTerrariaType(targetTypeFullName, out targetType))
                throw new Exception("Invalid targetTypeFullName. Terraria does not contain a type named \"" + targetTypeFullName + "\".");

            CreateHPatchOperation(targetType, targetMethodName, stubMethodName, patchLocation, patchPriority);
        }

        /// <summary>
        /// Creates a new patch operation using the supplied target type name, target method name, target method parameter count, stub method name, and patch location.
        /// </summary>
        /// <param name="targetTypeFullName">The full name of target type (in Terraria) that contains the target method, e.g. "Terraria.Main".</param>
        /// <param name="targetMethodName">The name of the target method.</param>
        /// <param name="targetMethodParamCount">The number of parameters in the target method. Can be used to help discern between method overloads.</param>
        /// <param name="stubMethodName">The name of the stub method IN THIS CLASS that will be either prepended or appended to the target method. Must be a static method!</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        /// <param name="patchPriority">The priority of this patch, as used by Harmony to order multiple patches on the same method. Patches with higher numbers go first. Set to -1 to use default priority (typically = 400).</param>
        protected void CreateHPatchOperation(string targetTypeFullName, string targetMethodName, int targetMethodParamCount, string stubMethodName, HPatchLocation patchLocation, int patchPriority = -1)
        {
            Type targetType = null;
            if (!HHelpers.TryGetTerrariaType(targetTypeFullName, out targetType))
                throw new Exception("Invalid targetTypeFullName. Terraria does not contain a type named \"" + targetTypeFullName + "\".");

            // First look in normal methods
            MethodBase targetMethod = targetType.GetRuntimeMethods().Where(x =>
                    x.Name == targetMethodName &&
                    x.GetParameters().Count() == targetMethodParamCount).FirstOrDefault();
            if (targetMethod == null) // If nothing was found, look in constructors next
            {
                targetMethod = targetType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Where(x =>
                        x.Name == targetMethodName &&
                        x.GetParameters().Count() == targetMethodParamCount).FirstOrDefault();
            }
            if (targetMethod == null)
                throw new Exception("Invalid target method. The target type does not contain a method or constructor named \"" + stubMethodName + "\" with " + targetMethodParamCount + " parameters.");

            CreateHPatchOperation(targetMethod, stubMethodName, patchLocation, patchPriority);
        }

        /// <summary>
        /// Creates a new patch operation using the supplied target type name, target method name, target method parameter count, target method last parameter type, stub method name, and patch location.
        /// </summary>
        /// <param name="targetTypeFullName">The full name of target type (in Terraria) that contains the target method, e.g. "Terraria.Main".</param>
        /// <param name="targetMethodName">The name of the target method.</param>
        /// <param name="targetMethodParamCount">The number of parameters in the target method. Can be used to help discern between method overloads.</param>
        /// <param name="targetMethodLastParamType">The type of the target method's last parameter. Can be used to help discern between method overloads.</param>
        /// <param name="stubMethodName">The name of the stub method IN THIS CLASS that will be either prepended or appended to the target method. Must be a static method!</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        /// <param name="patchPriority">The priority of this patch, as used by Harmony to order multiple patches on the same method. Patches with higher numbers go first. Set to -1 to use default priority (typically = 400).</param>
        protected void CreateHPatchOperation(string targetTypeFullName, string targetMethodName, int targetMethodParamCount, Type targetMethodLastParamType, string stubMethodName, HPatchLocation patchLocation, int patchPriority = -1)
        {
            Type targetType = null;
            if (!HHelpers.TryGetTerrariaType(targetTypeFullName, out targetType))
                throw new Exception("Invalid targetTypeFullName. Terraria does not contain a type named \"" + targetTypeFullName + "\".");

            MethodInfo targetMethod = targetType.GetRuntimeMethods().Where(x =>
                    x.Name == targetMethodName &&
                    x.GetParameters().Count() == targetMethodParamCount &&
                    x.GetParameters().Count() > 0 &&
                    x.GetParameters()[x.GetParameters().Count() - 1].ParameterType == targetMethodLastParamType).FirstOrDefault();
            if (targetMethod == null)
                throw new Exception("Invalid target method. The target type does not contain a method named \"" + stubMethodName + "\" with " + targetMethodParamCount + " parameters and a final parameter of type \"" + targetMethodLastParamType.FullName + "\"");

            CreateHPatchOperation(targetMethod, stubMethodName, patchLocation, patchPriority);
        }

        #endregion


        #region Security Compliant Helpers

        /// <summary>
        /// Retrieves the byte[] that constitutes an embedded resouce in this HPlugin's PluginAssembly.
        /// This helper is particularly useful when the plugin Security Level is set to Level 3 or higher (which disallows use of System.IO).
        /// </summary>
        /// <param name="resourceName">The name of the embedded resource to retrieve.</param>
        /// <returns>The embedded resource's bytes.</returns>
        public byte[] GetPluginAssemblyResourceBytes(string resourceName)
        {
            using (var resStream = PluginAssembly.GetManifestResourceStream(resourceName))
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    resStream.CopyTo(memStream);
                    return memStream.ToArray();
                }
            }
        }

        #endregion


        #region Override Methods

        /// <summary>
        /// Called by the HPlugin applicator immediately after creating an instance of this HPlugin. Setup your plugin here.
        /// 1. Set the various fields of the Identity property to identify your plugin.
        /// 2. Set HasPersistentData to true or false, depending on the plugin's needs.
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Called by the HPlugin applicator after Initialize and after the plugin's on-disk savedata has been loaded (if applicable).
        /// At this point, the Configuration property has been populated and is ready to use.
        /// Perform one-time setup logic here, such as loading user preferences from the Configuration property.
        /// </summary>
        /// <param name="successfulConfigLoadFromDisk">True if the configuration was successfully loaded from the disk (or if there was no prior configuration and a new one was generated). False if the configuration failed to load and a blank configuration was substituted in.</param>
        public virtual void ConfigurationLoaded(bool successfulConfigLoadFromDisk) { }

        /// <summary>
        /// Called by the HPlugin applicator immediately before the plugin's PatchOperations are executed.
        /// If the plugin has not defined its PatchOperations by this point, it must do so now, or nothing will be patched.
        /// </summary>
        public virtual void PrePatch() { }

        #endregion
    }
}
