using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.Management
{
    /// <summary>
    /// The kind of file type that a plugin is.
    /// </summary>
    public enum PluginFileType
    {
        /// <summary>
        /// For dummy values.
        /// </summary>
        None,
        
        /// <summary>
        /// A raw C# source file containing type(s) derived from HPlugin, which will be compiled on-demand when needed.
        /// </summary>
        CSSourceFile,

        /// <summary>
        /// An already compiled .NET assembly containing types derived from HPlugin.
        /// </summary>
        CompiledAssemblyFile
    }
}
