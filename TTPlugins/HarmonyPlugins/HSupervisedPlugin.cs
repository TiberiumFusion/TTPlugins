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
        /// Unique name of the supervised HPlugin that is used in determining things like savedata paths.
        /// </summary>
        internal string SavedataIdentity { get; set; }

        /// <summary>
        /// Creates a new HSupervisedPlugin wrapped around the provided HPlugin.
        /// </summary>
        /// <param name="plugin">The HPlugin to wrap.</param>
        internal HSupervisedPlugin(HPlugin plugin)
        {
            Plugin = plugin;

            SavedataIdentity = plugin.GetType().FullName;
        }
    }
}
