using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.Management
{
    /// <summary>
    /// A configuration object which contains the paramters for SecurityComplianceCecilTests.TestPluginCompliance()
    /// </summary>
    public class SecurityLevelComplianceTestConfiguration
    {
        /// <summary>
        /// List of PluginFiles to test. If any of these PluginFiles are source files, they will be compiled first.
        /// </summary>
        public List<PluginFile> PluginFilesToTest { get; set; }

        /// <summary>
        /// Path to the root IO.PluginsUserFilesFolder directory that contains that SourceFiles.
        /// </summary>
        public string UserFilesRootDirectory { get; set; }

        /// <summary>
        /// Path to Terraria.exe, which will be referenced by CodeDom during compilation.
        /// </summary>
        public string TerrariaPath { get; set; }

        /// <summary>
        /// List of Terraria.exe's embedded dependency assemblies, which will be temporarily written to disk and referenced by CodeDom during compilation.
        /// </summary>
        public List<byte[]> TerrariaDependencyAssemblies { get; set; }

        /// <summary>
        /// Whether or not to perform the Security Level 1 test.
        /// </summary>
        public bool RunLevel1Test = true;

        /// <summary>
        /// Whether or not to perform the Security Level 2 test.
        /// </summary>
        public bool RunLevel2Test = true;

        /// <summary>
        /// Whether or not to perform the Security Level 3 test.
        /// </summary>
        public bool RunLevel3Test = true;

        /// <summary>
        /// Whether or not to perform the Security Level 4 test.
        /// </summary>
        public bool RunLevel4Test = true;
    }
}
