using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// The location at which to apply a stub patch method.
    /// </summary>
    public enum HPatchLocation
    {
        /// <summary>
        /// Indicates that a stub method should be dynamically appended to a target method as a postfix method.
        /// </summary>
        Postfix = 0,

        /// <summary>
        /// Indicates that a stub method should be dynamically prepended to a target method as a prefix method.
        /// </summary>
        Prefix = 1,

        /// <summary>
        /// Indicates that a stub method should be interpreted as a Harmony transpiler patch.
        /// </summary>
        Transpiler = 2
    }
}
