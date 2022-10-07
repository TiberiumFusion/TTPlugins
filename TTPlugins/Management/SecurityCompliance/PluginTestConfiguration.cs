using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.Management.SecurityCompliance
{
    /// <summary>
    /// A configuration object which contains the parameters for CecilTests.TestPluginCompliance()
    /// </summary>
    public class PluginTestConfiguration
    {
        /// <summary>
        /// List of PluginFiles to test. If any of these PluginFiles are source files, they will be compiled first.
        /// </summary>
        public List<PluginFile> PluginFilesToTest { get; set; }

        /// <summary>
        /// Specifies how Terraria relates to the assembly load context in which TTPlugins is currently running. This determines the necessity of setting certain other parameters of this configuration.
        /// </summary>
        /// <remarks>
        /// <para>When <see cref="TerrariaEnvironment.Offline"/> is specified, <see cref="UserFilesRootDirectory"/> and <see cref="TerrariaPath"/> <strong>must</strong> be specified.</para>
        /// <para>When <see cref="TerrariaEnvironment.Online"/> is specified, automatic compilation of plugins in source code during the security tests is disabled (those plugins, if any, are ignored) and <see cref="UserFilesRootDirectory"/> and <see cref="TerrariaPath"/> are ignored and can be left null.</para>
        /// <para>When <see cref="TerrariaEnvironment.Unspecified"/> is specified, <strong>an exception is raised in <see cref="CecilTests.TestPluginCompliance"/></strong>.</para>
        /// <para>Terraria Tweaker 2 and TTApplicator always uses <see cref="TerrariaEnvironment.Offline"/>. The Terraria entry point in TTPlugins itself uses <see cref="TerrariaEnvironment.Online"/></para>
        /// </remarks>
        public TerrariaEnvironment TerrariaEnvironment { get; set; }

        /// <summary>
        /// Path to the root IO.PluginsUserFilesFolder directory that contains that SourceFiles.
        /// </summary>
        public string UserFilesRootDirectory { get; set; }

        /// <summary>
        /// Path to Terraria.exe, which will be referenced by CodeDom during compilation.
        /// </summary>
        public string TerrariaPath { get; set; }

        /// <summary>
        /// List of Terraria.exe's embedded dependency assemblies, which will be temporarily written to disk and referenced by CodeDom during on-demand compilation of plugins in source code form.
        /// </summary>
        public List<byte[]> TerrariaDependencyAssemblies { get; set; }
        
        /// <summary>
        /// Optional list of on-paths to additional assemblies which plugin compilation may depend upon.
        /// </summary>
        /// <remarks>
        /// TT2 and TTApplicator use this to include 0Harmony.dll as a reference.
        /// </remarks>
        public List<string> AdditionalCompileDependencies { get; set; }

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
