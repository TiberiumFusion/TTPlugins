using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins
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
        /// If true, all source files will be compiled into a single output assembly.
        /// If false, each source file will be compiled into its own assembly.
        /// </summary>
        public bool SingleAssemblyOutput { get; set; } = false;
    }
}
