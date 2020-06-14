using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.Management
{
    /// <summary>
    /// IO-related aspects of plugin management.
    /// </summary>
    public static class IO
    {
        #region Properties

        /// <summary>
        /// Absolute path to the folder containing the user's plugins, whether in .cs or .dll form.
        /// </summary>
        public static string PluginsUserFilesFolder { get; private set; } // Should be %APPDATA%/Terraria Tweaker 2/Plugins

        /// <summary>
        /// Absolute path to the top-level working folder for plugin data, where things like temporary savedata are stored.
        /// </summary>
        public static string PluginsDataFolder { get; private set; } // Should be %APPDATA%/Terraria Tweaker 2/ttplugins

        /// <summary>
        /// Absolute path to the folder where plugin savedata is temporarily written to during a tweak launch, before being written into the Tweak List by TTApplicator on patch lifecycle end.
        /// </summary>
        public static string PluginsTempSavedataFolder { get { return Path.Combine(PluginsDataFolder, "TempSavedata"); } }

        /// <summary>
        /// List of all PluginFiles that were found in the PluginsUserFilesFolder.
        /// </summary>
        public static List<PluginFile> FoundUserPluginFiles { get; private set; } = new List<PluginFile>();

        #endregion


        /// <summary>
        /// Sets up the bulk of the plugin management system and ensures all the necessary paths exist.
        /// </summary>
        /// <param name="tt2SavedataDirectory">Absolute path where Terraria Tweaker 2 stores its savedata.</param>
        public static void Initialize(string tt2SavedataDirectory)
        {
            // Find root folder
            PluginsUserFilesFolder = Path.Combine(tt2SavedataDirectory, "Plugins"); // tt2SavedataPath should be %APPDATA%/Terraria Tweaker 2
            PluginsDataFolder = Path.Combine(tt2SavedataDirectory, "ttplugins");

            // Create folders
            Directory.CreateDirectory(PluginsUserFilesFolder);
            Directory.CreateDirectory(PluginsDataFolder);
            Directory.CreateDirectory(PluginsTempSavedataFolder);
        }

        /// <summary>
        /// Rescans all files in the PluginsUserFilesFolder folder for .cs source files and .dll compiled assemblies.
        /// </summary>
        public static void Rescan()
        {
            FoundUserPluginFiles.Clear();

            // Find CS source plugin files
            string[] filesA = Directory.GetFiles(PluginsUserFilesFolder, "*.cs", SearchOption.AllDirectories);
            foreach (string file in filesA)
                FoundUserPluginFiles.Add(new PluginFile(file, PluginFileType.CSSourceFile));

            // Find compiled assembly plugin files
            string[] filesB = Directory.GetFiles(PluginsUserFilesFolder, "*.dll", SearchOption.AllDirectories);
            foreach (string file in filesB)
            {
                // Check if the DLL is a valid .NET assembly
                bool valid = false;
                try
                {
                    AssemblyName asmName = AssemblyName.GetAssemblyName(file);
                    valid = true;
                }
                catch (Exception e) { }

                if (valid)
                    FoundUserPluginFiles.Add(new PluginFile(file, PluginFileType.CompiledAssemblyFile));
            }
        }
        

        /// <summary>
        /// Tests all found PluginFiles against all security levels so as to determine the maximum security level that will allow each plugin to function.
        /// </summary>
        /// <param name="terrariaPath">Path to Terraria.exe, which will be referenced by CodeDom during compilation.</param>
        /// <param name="terrariaDependencyAssemblies">List of Terraria.exe's embedded dependency assemblies, which will be temporarily written to disk and reference by CodeDom during compilation.</param>
        /// <returns>A SecurityLevelComplianceTestResult object containing the test results.</returns>
        public static SecurityLevelComplianceTestsResults TestAllSecurityLevelComplianceForAllPlugins(string terrariaPath, List<byte[]> terrariaDependencyAssemblies)
        {
            SecurityLevelComplianceTestConfiguration config = new SecurityLevelComplianceTestConfiguration();
            config.PluginFilesToTest = new List<PluginFile>();
            config.PluginFilesToTest.AddRange(FoundUserPluginFiles);
            config.TerrariaPath = terrariaPath;
            config.TerrariaDependencyAssemblies = terrariaDependencyAssemblies;
            return SecurityComplianceCecilTests.TestPluginCompliance(config);
        }
    }
}
