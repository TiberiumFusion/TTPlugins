using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// Container of both an HPlugin and data related to the HPlugin that is hidden from the HPlugin itself
    /// </summary>
    internal class HSupervisedPlugin
    {
        /// <summary>
        /// The associated HPlugin.
        /// </summary>
        internal HPlugin Plugin { get; set; }

        /// <summary>
        /// Relative path of the source file used to compile the plugin, which is used as a unique identity for plugin configuration and savedata.
        /// </summary>
        internal string SourceFileRelativePath { get; set; }

        /// <summary>
        /// Creates a new HSupervisedPlugin wrapped around the provided HPlugin.
        /// </summary>
        /// <param name="plugin">The HPlugin to wrap.</param>
        /// <param name="sourceFileRelativePath">The relative path of the plugin's source file.</param>
        internal HSupervisedPlugin(HPlugin plugin, string sourceFileRelativePath)
        {
            Plugin = plugin;
            SourceFileRelativePath = sourceFileRelativePath;
        }

        /// <summary>
        /// Returns the path to the source file that was used to compile the supervised HPlugin.
        /// </summary>
        /// <returns>The path to the source file that was used to compile the supervised HPlugin.</returns>
        internal string GetPluginSourceFilePath()
        {
            string sourcePath = "";
            try { sourcePath = Plugin.GetSourceFilePath(); }
            catch (Exception e) { sourcePath = "Unknown plugin source file path."; };
            return sourcePath;
        }
    }
}
