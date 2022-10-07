using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.Management.SecurityCompliance
{
    /// <summary>
    /// Holder of the test results for multiple plugin files.
    /// </summary>
    public class MultipleTestsResults
    {
        /// <summary>
        /// Whether or not the any of the PluginFile(s) being tested was a C# source file and failed to compile.
        /// </summary>
        public bool AnyCompileFailure = false;

        /// <summary>
        /// List of per-PluginFile test results. Only tested plugins will be present in this dictionary. <see cref="Skipped"/> plugins will not have an entry.
        /// </summary>
        public Dictionary<PluginFile, PluginTestResult> IndividualResults = new Dictionary<PluginFile, PluginTestResult>();

        /// <summary>
        /// A list of <see cref="PluginFile"/>s which were skipped.
        /// </summary>
        /// <remarks>
        /// For security purposes, plugins are skipped when they are in source code form and TTPlugins is running inside Terraria. This is the only scenario in which plugins are skipped.
        /// </remarks>
        public List<PluginFile> Skipped = new List<PluginFile>();
    }
}
