using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// Contains configuration data for an HPlugin, such as user preferences.
    /// </summary>
    public class HPluginConfiguration
    {
        /// <summary>
        /// An XML element which may contain user preferences or other persistent plugin savedata.
        /// This object will automatically be saved to disk when Terraria closes.
        /// Make changes to this object to update your plugin's savedata.
        /// This will be an empty element if your plugin claims it does not need persistent savedata (HPlugin.HasPersistentData = false).
        /// </summary>
        public XElement Savedata { get; set; }

        /// <summary>
        /// Creates a new HPluginConfiguration with a blank savedata element
        /// </summary>
        public HPluginConfiguration()
        {
            Savedata = new XElement("Savedata");
        }

        /// <summary>
        /// Creates a new HPluginConfiguration with the provided savedata element
        /// </summary>
        /// <param name="savedata">The XML savedata element to use.</param>
        public HPluginConfiguration(XElement savedata)
        {
            Savedata = savedata;
        }
    }
}
