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
    public class HPlugin
    {
        #region Properties

        /// <summary>
        /// An informational object which describes the identity of this plugin.
        /// This property is used to identify plugin savedata and must be unique.
        /// </summary>
        public HPluginIdentity Identity { get; protected set; }

        /// <summary>
        /// The list of patch operations that constitute this HPlugin's functionality.
        /// </summary>
        public List<HPatchOperation> PatchOperations { get; protected set; }

        /// <summary>
        /// Contains the plugin's pesistent savedata, which includes user preferences from savedata (if any).
        /// </summary>
        public HPluginConfiguration Configuration { get; protected set; }

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
            Configuration = null;
            HasPersistentData = false;
        }

        #endregion


        #region Instrinsic Methods

        /// <summary>
        /// Called by the HPlugin applicator when it has loaded this plugins configuration (as per the ID provided by this plugin's Identity property).
        /// The current Configuration object (initially empty defaults) will be replaced with the provided configuration.
        /// </summary>
        /// <param name="config">The new HPluginConfiguration.</param>
        internal void ReceiveConfiguration(HPluginConfiguration config)
        {
            Configuration = config;
        }

        /// <summary>
        /// Writes the current Configuration object and associated savedata to disk in an asynchronous task.
        /// </summary>
        protected void SaveConfigurationToDisk()
        {
            HPluginApplicator.WriteConfigurationForHPlugin(this);
        }

        #endregion


        #region Convenient HPatchOperation creators

        /// <summary>
        /// Creates a new patch operation using the supplied target method, stub method, and patch location.
        /// </summary>
        /// <param name="targetMethod">The target method that will be patched.</param>
        /// <param name="stubMethod">The stub method that will be either prepended or appended to the target method. Must be a static method!</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        protected void CreateHPatchOperation(MethodInfo targetMethod, MethodInfo stubMethod, HPatchLocation patchLocation)
        {
            PatchOperations.Add(new HPatchOperation(targetMethod, stubMethod, patchLocation));
        }

        /// <summary>
        /// Creates a new patch operation using the supplied target type and method name, stub method, and patch location.
        /// </summary>
        /// <param name="targetType">The target type that contains the target method.</param>
        /// <param name="targetMethodName">The name of the target method.</param>
        /// <param name="stubMethod">The stub method that will be either prepended or appended to the target method. Must be a static method!</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        protected void CreateHPatchOperation(Type targetType, string targetMethodName, MethodInfo stubMethod, HPatchLocation patchLocation)
        {
            PatchOperations.Add(new HPatchOperation(targetType, targetMethodName, stubMethod, patchLocation));
        }

        /// <summary>
        /// Creates a new patch operation using the supplied target method, stub method name from this class, and patch location.
        /// </summary>
        /// <param name="targetMethod">The target method that will be patched.</param>
        /// <param name="stubMethodName">The name of the stub method IN THIS CLASS that will be either prepended or appended to the target method. Must be a static method!</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        /// <returns>True if the stubMethodName provided is a valid method and thus the HPatchOperation was created successfully; false if otherwise.</returns>
        protected bool CreateHPatchOperation(MethodInfo targetMethod, string stubMethodName, HPatchLocation patchLocation)
        {
            MethodInfo stubMethod = this.GetType().GetRuntimeMethods().Where(x => x.Name == stubMethodName).FirstOrDefault();
            if (stubMethod == null)
                return false;
            if (!stubMethod.Attributes.HasFlag(MethodAttributes.Static))
                return false;

            CreateHPatchOperation(targetMethod, stubMethod, patchLocation);
            return true;
        }

        /// <summary>
        /// Creates a new patch operation using the supplied target type and method name, stub method, and patch location.
        /// </summary>
        /// <param name="targetType">The target type that contains the target method.</param>
        /// <param name="targetMethodName">The name of the target method.</param>
        /// <param name="stubMethodName">The name of the stub method IN THIS CLASS that will be either prepended or appended to the target method. Must be a static method!</param>
        /// <param name="patchLocation">Whether the stub method will be prepended as a prefix or appended as a postfix to the target method.</param>
        /// <returns>True if the stubMethodName provided is a valid method and thus the HPatchOperation was created successfully; false if otherwise.</returns>
        protected bool CreateHPatchOperation(Type targetType, string targetMethodName, string stubMethodName, HPatchLocation patchLocation)
        {
            MethodInfo stubMethod = this.GetType().GetRuntimeMethods().Where(x => x.Name == stubMethodName).FirstOrDefault();
            if (stubMethod == null)
                return false;
            if (!stubMethod.Attributes.HasFlag(MethodAttributes.Static))
                return false;

            CreateHPatchOperation(targetType, targetMethodName, stubMethod, patchLocation);
            return true;
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
