using com.tiberiumfusion.ttplugins.HarmonyPlugins;
using Mono.Cecil;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using com.tiberiumfusion.ttplugins.Management.SecurityCompliance;

namespace com.tiberiumfusion.ttplugins.Management
{
    /// <summary>
    /// A representation of a single plugin file in the user's Plugins folder.
    /// </summary>
    public class PluginFile
    {
        #region Properties

        /// <summary>
        /// Path to the plugin file on the disk.
        /// </summary>
        public string PathToFile { get; private set; }

        /// <summary>
        /// What kind of file the plugin is (source or compiled asm).
        /// </summary>
        public PluginFileType FileType { get; private set; }

        #endregion


        /// <summary>
        /// Creates a new PluginFile object for use in plugin file management.
        /// </summary>
        /// <param name="path">Path to the file on the disk.</param>
        /// <param name="type">What kind of file the plugin is.</param>
        public PluginFile(string path, PluginFileType type)
        {
            PathToFile = path;
            FileType = type;
        }

        /// <summary>
        /// Updates this PluginFile when the contents of the file on the disk change.
        /// </summary>
        public void UpdateFromFileChange()
        {
            // Nothing here for now
        }

        /// <summary>
        /// Update this PluginFile's PathToFile and FileType to correspond to a new path.
        /// </summary>
        /// <param name="newPath">The new path to use.</param>
        public void UpdateFilePath(string newPath)
        {
            PathToFile = newPath;

            string newExt = Path.GetExtension(Path.GetFullPath(newPath)).ToLowerInvariant();
            if (newExt == ".cs")
                FileType = PluginFileType.CSSourceFile;
            else if (newExt == ".dll")
                FileType = PluginFileType.CompiledAssemblyFile;
        }

        /// <summary>
        /// Returns the relative path of this plugin file on the disk (relative to IO.PluginsUserFilesFolder).
        /// </summary>
        /// <returns>The relative path, or null if the file is not actually relative to IO.PluginsUserFilesFolder.</returns>
        public string GetRelativePath()
        {
            return IO.GetRelativeUserFilesPathFor(PathToFile);
        }

        /// <summary>
        /// Returns the path to use for reading and writing temporary, on-disk files relating to this PluginFile.
        /// </summary>
        /// <returns>The path to the directory to use. The directory will NOT be created if it does not exist.</returns>
        public string GetTemporaryFilesPath()
        {
            return IO.GetTemporaryFilePathFor(this);
        }


        /// <summary>
        /// Tests this PluginFile against all security levels so as to determine the maximum security level that will allow this plugin to function.
        /// </summary>
        /// <remarks>
        /// This method should only be used by management tools (i.e. TT2 and TTApp).
        /// </remarks>
        /// <param name="terrariaPath">Path to Terraria.exe, which will be referenced by CodeDom during compilation.</param>
        /// <param name="terrariaDependencyAssemblies">List of Terraria.exe's embedded dependency assemblies, which will be temporarily written to disk and reference by CodeDom during compilation.</param>
        /// <param name="additionalCompileDependencies">Optional list of on-disk paths to additional assemblies that may be required for plugin compilation.</param>
        /// <returns>A SecurityLevelComplianceTestResult object containing the test results.</returns>
        public MultipleTestsResults TestAllSecurityLevelCompliance(string terrariaPath, List<byte[]> terrariaDependencyAssemblies, List<string> additionalCompileDependencies = null)
        {
            PluginTestConfiguration config = new PluginTestConfiguration();
            config.PluginFilesToTest = new List<PluginFile>() { this };
            config.TerrariaEnvironment = TerrariaEnvironment.Offline;
            config.TerrariaPath = terrariaPath;
            config.TerrariaDependencyAssemblies = terrariaDependencyAssemblies;
            config.AdditionalCompileDependencies = additionalCompileDependencies;
            return CecilTests.TestPluginCompliance(config);
        }
    }
}
