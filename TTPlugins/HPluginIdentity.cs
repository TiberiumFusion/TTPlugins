using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins
{
    /// <summary>
    /// Informational class which describes an HPlugin's identity.
    /// </summary>
    public class HPluginIdentity
    {
        /// <summary>
        /// The name of this plugin.
        /// </summary>
        public string PluginName { get; set; }

        /// <summary>
        /// A brief description of what this plugin does.
        /// </summary>
        public string PluginDescription { get; set; }

        /// <summary>
        /// The creator of this plugin.
        /// </summary>
        public string PluginAuthor { get; set; }

        /// <summary>
        /// The version of this plugin.
        /// </summary>
        public Version PluginVersion { get; set; }


        /// <summary>
        /// Creates a new HPluginIdentity with default values.
        /// </summary>
        public HPluginIdentity()
        {
            PluginName = "Unknown Plugin";
            PluginDescription = "Unknown Description";
            PluginAuthor = "Unknown Author";
            PluginVersion = new Version(0, 0, 0, 0);
        }
    }
}
