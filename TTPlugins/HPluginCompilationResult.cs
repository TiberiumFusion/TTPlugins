using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins
{
    /// <summary>
    /// A bundle of data produced by HPluginAssemblyCompiler.Compile()
    /// </summary>
    public class HPluginCompilationResult
    {
        /// <summary>
        /// The compiled usercode assemblies.
        /// </summary>
        public List<Assembly> CompiledAssemblies { get; set; } = new List<Assembly>();

        /// <summary>
        /// List of any compiler errors.
        /// </summary>
        public List<CompilerError> CompileErrors { get; set; } = new List<CompilerError>();

        /// <summary>
        /// If true, a generic exception was thrown during compilation.
        /// </summary>
        public bool GenericCompilationFailure { get; set; } = false;
    }
}
