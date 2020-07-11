using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// Container of both an HPlugin and data related to the HPlugin that is hidden from the HPlugin itself.
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
        /// The latest iteration of the runtime configuration in its xml state.
        /// </summary>
        internal XDocument LatestConfigurationXML { get; set; }

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
    }
}
