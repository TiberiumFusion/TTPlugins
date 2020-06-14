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
    public class SecurityLevelComplianceTestResults
    {
        /// <summary>
        /// Whether or not an unexpected exception occurred during the test.
        /// </summary>
        public bool GenericTestFailure = false;

        /// <summary>
        /// Whether or not the PluginFile being tested was a C# source file and it did not compile successfully.
        /// </summary>
        public bool CompileFailure = false;

        /// <summary>
        /// Messages from the testing process that are not specific to the individual security tests.
        /// </summary>
        public List<string> GenericMessages = new List<string>();

        /// <summary>
        /// Whether or not the compiled plugin is compliant with Level 1 security.
        /// </summary>
        public bool PassLevel1 = false;

        /// <summary>
        /// Whether or not the compiled plugin is compliant with Level 2 security.
        /// </summary>
        public bool PassLevel2 = false;

        /// <summary>
        /// Whether or not the compiled plugin is compliant with Level 3 security.
        /// </summary>
        public bool PassLevel3 = false;

        /// <summary>
        /// Whether or not the compiled plugin is compliant with Level 4 security.
        /// </summary>
        public bool PassLevel4 = false;
        
        /// <summary>
        /// Messages from the Level 1 testing procedure, which can be shown to the user in UI if applicable.
        /// </summary>
        public List<string> MessagesLevel1 = new List<string>();

        /// <summary>
        /// Messages from the Level 2 testing procedure, which can be shown to the user in UI if applicable.
        /// </summary>
        public List<string> MessagesLevel2 = new List<string>();

        /// <summary>
        /// Messages from the Level 3 testing procedure, which can be shown to the user in UI if applicable.
        /// </summary>
        public List<string> MessagesLevel3 = new List<string>();

        /// <summary>
        /// Messages from the Level 4 testing procedure, which can be shown to the user in UI if applicable.
        /// </summary>
        public List<string> MessagesLevel4 = new List<string>();
    }
}
