using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.Management.SecurityCompliance
{
    /// <summary>
    /// Holder of the test results of a security compliance test for a single level.
    /// </summary>
    public class LevelTestResult
    {
        /// <summary>
        /// Whether or not the assembly passed the test.
        /// </summary>
        public bool Passed { get; private set; }

        /// <summary>
        /// A list of messages from the test, such as details on specific security violations.
        /// </summary>
        public List<string> Messages { get; private set; } = new List<string>();

        /// <summary>
        /// Creates a new SecurityComplianceTestResult object with the provided data.
        /// </summary>
        /// <param name="passed">The value to assign to the Passed property.</param>
        /// <param name="messages">The value to assign to the Messages property.</param>
        public LevelTestResult(bool passed, List<string> messages)
        {
            Passed = passed;
            Messages = messages;
        }
    }
}
