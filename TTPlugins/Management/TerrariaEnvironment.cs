using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.Management
{
    /// <summary>
    /// Specifies how Terraria related to the assembly context in which TTPlugins is running.
    /// </summary>
    public enum TerrariaEnvironment
    {
        /// <summary>
        /// For situations where online vs offline has no difference, or when the status of the Terraria reference assemblies is indeterminate.
        /// </summary>
        Unspecified,

        /// <summary>
        /// Indicates that the assembly load context in which TTPlugins is running is <strong>not</strong> a running Terraria process.
        /// </summary>
        Offline,

        /// <summary>
        /// Indicates that the assembly load context in which TTPlugins is running is a running Terraria process.TTPlugins.
        /// </summary>
        Online,
    }
}
