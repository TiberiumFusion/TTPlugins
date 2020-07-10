using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        /// Simple logging helper.
        /// </summary>
        private static void DLog(string message)
        {
            Debug.WriteLine("[TTPlugins] (HFrameworkPatches) Writing all plugin configuration to disk...");
        }

        /// <summary>
        /// Prefixed onto Terraria.Main.SaveSettings().
        /// Writes all persistent plugin savedata back to disk.
        /// </summary>
        public static void FW_SaveAllPluginConfigs()
        {
            DLog("Entered FW_SaveAllPluginConfigs()");

            try
            {
                DLog("Writing all plugin configuration to disk...");
                HPluginApplicator.WriteAllPluginConfigToDisk();
                DLog("Finished writing plugin configurations.");
            }
            catch (Exception e)
            {
                DLog("Generic error while writing plugin configurations. Details: " + e);
            }
        }

        /// <summary>
        /// Prefixed onto Terraria.Main.QuitGame().
        /// Sets up hidden cmd task to kill the runtime extract folder after a short delay (after which Terraria should be closed and the files no longer locked).
        /// </summary>
        public static void FW_RemoveRuntimeExtractDirOnQuite()
        {
            DLog("Entered FW_RemoveRuntimeExtractDirOnQuite()");

            try
            {
                if (HPluginApplicator.TerrariaAssembly != null)
                {
                    string fullRuntimeExtractDirPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(HPluginApplicator.TerrariaAssembly.Location)), HPluginApplicator.RuntimeExtractFolder);
                    DLog("Starting cmd delete task for runtime extract dir: " + fullRuntimeExtractDirPath + "...");

                    // Credit: https://stackoverflow.com/questions/41922322/using-winform-c-sharp-delete-the-folder-the-exe-exists-in
                    // This is kind of ugly, but it is the only simple, reliable way I can think of to do this
                    ProcessStartInfo procInfo = new ProcessStartInfo("cmd.exe",
                        String.Format("/k {0} & {1} & {2}",
                            "timeout /T 5 /NOBREAK >NUL",
                            "rmdir /s /q \"" + fullRuntimeExtractDirPath + "\"",
                            "exit")
                        );

                    procInfo.UseShellExecute = false;
                    procInfo.CreateNoWindow = true;
                    procInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    Process.Start(procInfo);
                    DLog("Runtime extract dir delete task created.");
                }
            }
            catch (Exception e)
            {
                DLog("Error while creating cmd delete task for runtime extract dir. Details: " + e);
            }
        }

        /// <summary>
        /// Prefixed onto Terraria.TimeLogger.DrawException().
        /// Writes the intercepted exception to Debug.
        /// </summary>
        /// <param name="e">The intercepted exception.</param>
        public static void FW_InterceptTimeLoggerDrawException(Exception e)
        {
            Debug.WriteLine("Exception intercepted from Terraria.TimeLogger.DrawException(): " + e);
        }
    }
}
