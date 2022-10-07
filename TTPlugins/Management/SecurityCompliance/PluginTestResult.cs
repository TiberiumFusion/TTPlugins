using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.Management.SecurityCompliance
{
    /// <summary>
    /// Container of secury compliance test results for a single PluginFile.
    /// </summary>
    public class PluginTestResult
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
        /// Messages from the testing process that are not related to any specific level of the security tests.
        /// </summary>
        public List<string> GenericMessages = new List<string>();

        /// <summary>
        /// Whether or not compliance with Level 1 security was tested.
        /// </summary>
        public bool TestedLevel1 = false;

        /// <summary>
        /// Whether or not compliance with Level 2 security was tested.
        /// </summary>
        public bool TestedLevel2 = false;

        /// <summary>
        /// Whether or not compliance with Level 3 security was tested.
        /// </summary>
        public bool TestedLevel3 = false;

        /// <summary>
        /// Whether or not compliance with Level 4 security was tested.
        /// </summary>
        public bool TestedLevel4 = false;

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

        /// <summary>
        /// The PluginFile associated with these test results.
        /// </summary>
        public PluginFile TestedPluginFile { get; private set; }


        /// <summary>
        /// Creates a new SecurityLevelComplianceSingleTestResult for the specified PluginFile.
        /// </summary>
        /// <param name="pluginFileToTest">The PluginFile to associate with these test results.</param>
        public PluginTestResult(PluginFile pluginFileToTest)
        {
            TestedPluginFile = pluginFileToTest;
        }
    }
}
