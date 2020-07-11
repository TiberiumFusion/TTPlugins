using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.tiberiumfusion.ttplugins.HarmonyPlugins;

namespace TEMPLATE_NAMESPACE
{
    /// <summary>
    /// This is the main class of your plugin.
    /// Don't forget to use the TTPlugins reference wiki: https://github.com/TiberiumFusion/TTPlugins/wiki
    /// </summary>
    public class TEMPLATE_CLASSNAME : HPlugin
    {
        #region Plugin Self-Management

        /// <summary>
        /// This is called ONCE by the HPlugin applicator immediately after creating an instance of this HPlugin. Setup your plugin here.
        /// 1. Set the various fields of the Identity property to identify your plugin.
        /// 2. Set HasPersistentData to true or false, depending on your plugin's needs.
        /// </summary>
        public override void Initialize()
        {
            // Establish this plugin's internal Identity within the TTPlugins environment. Every plugin should have a unique internal Identity.
            Identity.PluginName = "TEMPLATE_PLUGINIDNAME";
            Identity.PluginDescription = "TEMPLATE_PLUGINIDDESC";
            Identity.PluginAuthor = "TEMPLATE_PLUGINIDAUTHOR";
            Identity.PluginVersion = new Version("TEMPLATE_PLUGINIDVERSION");

            HasPersistentSavedata = false; // Set to true if your plugin uses the persistent savedata system.


            // TODO: Setup the rest of your plugin here (if necessary)
        }

        /// <summary>
        /// This is called ONCE by the HPlugin applicator some time after Initialize() and after the plugin's on-disk savedata has been loaded (if applicable).
        /// At this point, the Configuration property has been populated and is ready to use.
        /// Perform more one-time setup logic here, such as loading user preferences from the Configuration's Savedata property.
        /// </summary>
        /// <param name="successfulConfigLoadFromDisk">True if the configuration was successfully loaded from the disk (or if there was no prior configuration and a new one was generated). False if the configuration failed to load and a blank configuration was substituted in.</param>
        public override void ConfigurationLoaded(bool successfulConfigLoadFromDisk)
        {
            // TODO: Load persistent savedata (if applicable)
        }

        /// <summary>
        /// This is called ONCE by the HPlugin applicator (some time after ConfigurationLoaded()) immediately before the plugin's PatchOperations are executed.
        /// If your plugin has not defined its PatchOperations by this point, it must do so now, or nothing will be patched.
        /// </summary>
        public override void PrePatch()
        {
            // TODO: Define patch operations for your plugin using the various CreateHPatchOperation() methods.
        }

        #endregion


        #region Plugin Patch Methods

        // TODO: Define some static stub methods that will do things when dynamically patched into Terraria.

        #endregion
    }
}