using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// Provider of the compiled assemblies that contain the usercode HPlugins.
    /// </summary>
    public static class HPluginAssemblyCompiler
    {
        /// <summary>
        /// Name of the temporary folder which will be created on disk if necessary during the assembly compilation (such as for referencing in-memory assemblies with CodeDom)
        /// </summary>
        public static string TemporaryFilesPath { get; set; } = ".TTPluginsTemp_Compile";

        /// <summary>
        /// Returns a list of assemblies compiled from the provided configuration.
        /// </summary>
        /// <param name="configuration">The configuration to use when compiling.</param>
        /// <returns>The compiled assemblies.</returns>
        public static HPluginCompilationResult Compile(HPluginCompilationConfiguration configuration)
        {
            HPluginCompilationResult result = new HPluginCompilationResult();

            // List of paths to dlls that were written to disk from memory and should be deleted when compilation is over
            List<string> temporaryDiskReferenceFiles = new List<string>();

            try
            {
                // Compiler configuration
                CompilerParameters compilerParams = new CompilerParameters();
                compilerParams.GenerateInMemory = true;
                compilerParams.GenerateExecutable = false;
                compilerParams.CompilerOptions = "/optimize";
                compilerParams.TreatWarningsAsErrors = false;
                // References on disk
                foreach (string filePath in configuration.ReferencesOnDisk)
                    compilerParams.ReferencedAssemblies.Add(filePath);
                // References in memory
                int refAsmNum = 0;
                foreach (byte[] asmBytes in configuration.ReferencesInMemory)
                {
                    Directory.CreateDirectory(TemporaryFilesPath);
                    string asmFullPath = Path.Combine(Directory.GetCurrentDirectory(), TemporaryFilesPath, "RefAsm" + refAsmNum + ".dll");
                    File.WriteAllBytes(asmFullPath, asmBytes);
                    compilerParams.ReferencedAssemblies.Add(asmFullPath);
                    temporaryDiskReferenceFiles.Add(asmFullPath);
                    refAsmNum++;
                }

                CSharpCodeProvider csProvider = new CSharpCodeProvider();

                if (configuration.SingleAssemblyOutput)
                {
                    compilerParams.OutputAssembly = "AllCompiledTTPlugins";
                    CompileOnce(configuration, compilerParams, csProvider, result);
                }
                else
                {
                    foreach (string sourceFile in configuration.SourceFiles)
                    {
                        compilerParams.OutputAssembly = Path.GetFileNameWithoutExtension(sourceFile);
                        CompileOnce(configuration, compilerParams, csProvider, result);
                    }
                }
            }
            catch (Exception e)
            {
                result.GenericCompilationFailure = true;
            }

            // Clear temporary reference assembly files
            foreach (string tempFile in temporaryDiskReferenceFiles)
            {
                try { File.Delete(tempFile); }
                catch (Exception e) { } // Swallow it
            }
            try
            {
                if (Directory.Exists(TemporaryFilesPath))
                    Directory.Delete(TemporaryFilesPath);
            }
            catch (Exception e) { } // Swallow it
            
            return result;
        }
        
        private static void CompileOnce(HPluginCompilationConfiguration configuration, CompilerParameters compilerParams, CSharpCodeProvider csProvider, HPluginCompilationResult result)
        {
            CompilerResults compileResult = csProvider.CompileAssemblyFromFile(compilerParams, configuration.SourceFiles.ToArray());

            if (compileResult.Errors.HasErrors)
            {
                foreach (CompilerError error in compileResult.Errors)
                    result.CompileErrors.Add(error);
            }
            else
                result.CompiledAssemblies.Add(compileResult.CompiledAssembly);
        }
    }
}
