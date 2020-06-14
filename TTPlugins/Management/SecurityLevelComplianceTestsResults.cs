using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.Management
{
    /// <summary>
    /// A simple holder of result data that is returned by PluginFile.TestSecurityLevelCompliance()
    /// </summary>
    public class SecurityLevelComplianceTestsResults
    {
        /// <summary>
        /// Whether or not the any of the PluginFile(s) being tested was a C# source file and failed to compile.
        /// </summary>
        public bool AnyCompileFailure = false;

        /// <summary>
        /// List of per-PluginFile test results.
        /// </summary>
        public Dictionary<PluginFile, SecurityLevelComplianceSingleTestResult> IndividualResults = new Dictionary<PluginFile, SecurityLevelComplianceSingleTestResult>();
    }
}
