using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// Provides configuration data for the HPluginApplicator.
    /// Configuration data will be provided by TTApplicator.
    /// </summary>
    public sealed class HPluginApplicatorConfiguration
    {
        /// <summary>
        /// The executing Terraria assembly which will be patched using Harmony.
        /// </summary>
        public Assembly ExecutingTerrariaAssembly { get; set; }

        /// <summary>
        /// List of all loaded usercode assemblies that contain the HPlugins to be applied.
        /// </summary>
        public List<Assembly> PluginAssemblies { get; set; } = new List<Assembly>();

        /// <summary>
        /// Dictionary that maps the full name of each HPlugin type to the relative path of its source file (which is its configuration and savedata identity).
        /// </summary>
        public Dictionary<string, string> PluginTypesRelativePaths { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Path to the root folder to use for temporary plugin files (i.e. temporary configuration & savedata copies).
        /// </summary>
        public string PluginTemporaryFilesRootDirectory { get; set; }
        
        /// <summary>
        /// The name of the file containing a plugin's runtime configuration file, within its temporary files directory.
        /// </summary>
        public string PluginRuntimeConfigFileName { get; set; } = "RuntimeConfiguration.xml";
    }
}
