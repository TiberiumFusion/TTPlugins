using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// Holder of various Harmony patches which provide some framework for HPlugins.
    /// </summary>
    public static class HFrameworkPatches
    {
        /// <summary>
        /// Prefixed onto Terraria.Main.SaveSettings().
        /// Writes all persistent plugin savedata back to disk.
        /// </summary>
        public static void FW_SaveAllPluginConfigs()
        {
            try
            {
                Debug.WriteLine("[TTPlugins] Writing all plugin configuration to disk...");
                HPluginApplicator.WriteAllPluginConfigToDisk();
                Debug.WriteLine("[TTPlugins] Finished writing plugin configurations.");
            }
            catch (Exception e)
            {
                Debug.WriteLine("[TTPlugins] Generic error while writing plugin configurations. Details: " + e);
            }
        }
    }
}
