using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// The configuration used to compile usercode HPlugins into assemblies.
    /// </summary>
    public class HPluginCompilationConfiguration
    {
        /// <summary>
        /// List of paths to all CS source files to use.
        /// </summary>
        public List<string> SourceFiles { get; set; } = new List<string>();

        /// <summary>
        /// Path to the root IO.PluginsUserFilesFolder directory that contains that SourceFiles.
        /// </summary>
        public string UserFilesRootDirectory { get; set; }

        /// <summary>
        /// If true, all source files will be compiled into a single output assembly.
        /// If false, each source file will be compiled into its own assembly.
        /// </summary>
        public bool SingleAssemblyOutput { get; set; } = false;

        /// <summary>
        /// List of paths to all references needed for plugin compilation (i.e. Terraria.exe and its extracted dependencies).
        /// Must be file paths on the disk, since CodeDom cannot use in-memory assemblies.
        /// </summary>
        public List<string> ReferencesOnDisk { get; set; } = new List<string>();

        /// <summary>
        /// List of all references that are in memory.
        /// HPluginAssemblyCompiler.Compile() will write temporary disk copies of these files to a temporary folder for CodeDom to reference.
        /// </summary>
        public List<byte[]> ReferencesInMemory { get; set; } = new List<byte[]>();
        
        /// <summary>
        /// If true, the ReferencesInMemory that were written to temporary disk copies will be deleted once the compile operation is complete.
        /// </summary>
        public bool ClearTemporaryFilesWhenDone { get; set; } = true;

        /// <summary>
        /// If true, ReferencesInMemory won't be written to temporary disk copies, and the contents of the folder will be re-used instead.
        /// </summary>
        public bool ReuseTemporaryFiles { get; set; } = false;
    }
}
