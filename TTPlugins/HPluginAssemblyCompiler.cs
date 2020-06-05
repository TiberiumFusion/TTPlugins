using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins
{
    /// <summary>
    /// Provider of the compiled assemblies that contain the usercode HPlugins.
    /// </summary>
    public static class HPluginAssemblyCompiler
    {
        /// <summary>
        /// Returns a list of assemblies compiled from the provided configuration.
        /// </summary>
        /// <param name="configuration">The configuration to use when compiling.</param>
        /// <returns>The compiled assemblies.</returns>
        public static HPluginCompilationResult Compile(HPluginCompilationConfiguration configuration)
        {
            HPluginCompilationResult results = new HPluginCompilationResult();

            try
            {
                // Compiler configuration
                CompilerParameters compilerParams = new CompilerParameters();
                compilerParams.GenerateInMemory = true;
                compilerParams.GenerateExecutable = false;
                compilerParams.CompilerOptions = "/optimize";
                compilerParams.TreatWarningsAsErrors = false;

                CSharpCodeProvider csProvider = new CSharpCodeProvider();

                if (configuration.SingleAssemblyOutput)
                {
                    compilerParams.OutputAssembly = "AllCompiledTTPlugins";
                    CompileOnce(configuration, compilerParams, csProvider, results);
                }
                else
                {
                    foreach (string sourceFile in configuration.SourceFiles)
                    {
                        compilerParams.OutputAssembly = Path.GetFileNameWithoutExtension(sourceFile);
                        CompileOnce(configuration, compilerParams, csProvider, results);
                    }
                }
            }
            catch (Exception e)
            {
                results.GenericCompilationFailure = true;
            }

            return results;
        }
        
        private static void CompileOnce(HPluginCompilationConfiguration configuration, CompilerParameters compilerParams, CSharpCodeProvider csProvider, HPluginCompilationResult results)
        {
            CompilerResults result = csProvider.CompileAssemblyFromFile(compilerParams, configuration.SourceFiles.ToArray());

            if (result.Errors.HasErrors)
            {
                foreach (CompilerError error in result.Errors)
                    results.CompileErrors.Add(error);
            }
            else
                results.CompiledAssemblies.Add(result.CompiledAssembly);
        }
    }
}
