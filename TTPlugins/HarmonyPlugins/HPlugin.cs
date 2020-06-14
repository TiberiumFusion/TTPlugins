using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// The base class which provides the means to create a Harmony-based plugin.
    /// </summary>
    public class HPlugin : MarshalByRefObject
    {
        #region Properties

        /// <summary>
        /// An informational object which describes the identity of this plugin.
        /// </summary>
        public HPluginIdentity Identity { get; protected set; }

        /// <summary>
        /// The list of patch operations that constitute this HPLugin.
        /// </summary>
        public List<HPatchOperation> PatchOperations { get; protected set; }

        /// <summary>
        /// Contains the plugin's pesistent savedata, which includes user preferences from savedata (if any).
        /// </summary>
        public HPluginConfiguration Configuration { get; set; }

        /// <summary>
        /// Whether or not this plugin needs to write persistent savedata to disk (such as for user preferences).
        /// </summary>
        public bool HasPersistentData { get; protected set; }

        #endregion


        #region Ctor

        /// <summary>
        /// Creates a new HPlugin with default values.
        /// </summary>
        public HPlugin()
        {
            Identity = new HPluginIdentity();
            PatchOperations = new List<HPatchOperation>();
            Configuration = new HPluginConfiguration();
            HasPersistentData = false;
        }

        #endregion
        

        #region Convenience Methods

        /// <summary>
        /// Creates a new patch operation using the supplied target method, stub method, and patch location.
        /// </summary>
        /// <param name="targetMethod">The target method that will be patched.</param>
        /// <param name="stubMethod">The stub method that will be either prepended or appended to the target method.</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        protected void CreateHPatchOperation(MethodInfo targetMethod, MethodInfo stubMethod, HPatchLocation patchLocation)
        {
            PatchOperations.Add(new HPatchOperation(targetMethod, stubMethod, patchLocation));
        }

        /// <summary>
        /// Creates a new patch operation using the supplied target type & method name, stub method, and patch location.
        /// </summary>
        /// <param name="targetType">The target type that contains the target method.</param>
        /// <param name="targetMethodName">The name of the target method.</param>
        /// <param name="stubMethod">The stub method that will be either prepended or appended to the target method.</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        protected void CreateHPatchOperation(Type targetType, string targetMethodName, MethodInfo stubMethod, HPatchLocation patchLocation)
        {
            PatchOperations.Add(new HPatchOperation(targetType, targetMethodName, stubMethod, patchLocation));
        }

        /// <summary>
        /// Writes the current Configuration object and associated savedata to disk in an asynchronous task.
        /// </summary>
        protected void SaveConfigurationToDisk()
        {
            HPluginApplicator.WriteConfigurationForHPatch(this);
        }

        #endregion


        #region Override Methods

        /// <summary>
        /// Called by the HPatch applicator immediately after creating an instance of this HPatch. Setup your plugin here.
        /// 1. Set the various fields of the Identity property to identify your plugin.
        /// 2. Set HasPersistentData to true or false, depending on the plugin's needs.
        /// 3. Define HPatchOperations, using the CreateHPatchOperation method.
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Called by the HPatch applicator after Initialize and after the plugin's on-disk savedata has been loaded (if applicable).
        /// At this point, the Configuration property has been populated and is ready to use,.
        /// Perform one-time setup logic here, such as loading user preferences from the Configuration property.
        /// </summary>
        public virtual void Configure() { }

        #endregion
    }
}
