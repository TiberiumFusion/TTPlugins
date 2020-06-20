using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// Informational class which describes an HPlugin's identity.
    /// </summary>
    public class HPluginIdentity
    {
        /// <summary>
        /// The name of this plugin.
        /// </summary>
        public string PluginName
        {
            get { return _PluginName; }
            set
            {
                _PluginName = value;
                HasModifiedName = true;
                HasModifiedIDInfo = true;
            }
        }
        private string _PluginName = null;

        /// <summary>
        /// A brief description of what this plugin does.
        /// </summary>
        public string PluginDescription
        {
            get { return _PluginDescription; }
            set
            {
                _PluginDescription = value;
                HasModifiedIDInfo = true;
            }
        }
        private string _PluginDescription = null;

        /// <summary>
        /// The creator of this plugin.
        /// </summary>
        public string PluginAuthor
        {
            get { return _PluginAuthor; }
            set
            {
                _PluginAuthor = value;
                HasModifiedIDInfo = true;
            }
        }
        private string _PluginAuthor = null;

        /// <summary>
        /// The version of this plugin.
        /// </summary>
        public Version PluginVersion
        {
            get { return _PluginVersion; }
            set
            {
                _PluginVersion = value;
                HasModifiedIDInfo = true;
            }
        }
        private Version _PluginVersion = null;

        /// <summary>
        /// True if the PluginName plugin has been modified (and thus is likely not an empty default value).
        /// </summary>
        public bool HasModifiedName { get; private set; }

        /// <summary>
        /// True if any of the plugin identification properties in this object have been modified in some way (and thus are likely not empty default values).
        /// </summary>
        public bool HasModifiedIDInfo { get; private set; }


        /// <summary>
        /// Creates a new HPluginIdentity with default values.
        /// </summary>
        public HPluginIdentity()
        {
            PluginName = "Unknown Plugin";
            PluginDescription = "Unknown Description";
            PluginAuthor = "Unknown Author";
            PluginVersion = new Version(0, 0, 0, 0);

            HasModifiedName = false;
            HasModifiedIDInfo = false;
        }
    }
}
