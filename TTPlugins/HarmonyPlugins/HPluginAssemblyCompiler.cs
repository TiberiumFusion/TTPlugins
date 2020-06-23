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
        /// Compiles and returns a list of assemblies using the provided configuration.
        /// </summary>
        /// <param name="configuration">The configuration to use when compiling.</param>
        /// <returns>The compiled assemblies.</returns>
        public static HPluginCompilationResult Compile(HPluginCompilationConfiguration configuration)
        {
            HPluginCompilationResult result = new HPluginCompilationResult();
            
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
                if (configuration.ReuseTemporaryFiles)
                {
                    if (Directory.Exists(TemporaryFilesPath))
                    {
                        foreach (string refAsmPath in Directory.GetFiles(TemporaryFilesPath))
                            compilerParams.ReferencedAssemblies.Add(refAsmPath);
                    }
                }
                else
                {
                    int refAsmNum = 0;
                    foreach (byte[] asmBytes in configuration.ReferencesInMemory)
                    {
                        Directory.CreateDirectory(TemporaryFilesPath);
                        string asmFullPath = Path.Combine(Directory.GetCurrentDirectory(), TemporaryFilesPath, "RefAsm" + refAsmNum + ".dll");
                        File.WriteAllBytes(asmFullPath, asmBytes);
                        compilerParams.ReferencedAssemblies.Add(asmFullPath);
                        refAsmNum++;
                    }
                }

                CSharpCodeProvider csProvider = new CSharpCodeProvider();

                if (configuration.SingleAssemblyOutput)
                {
                    compilerParams.OutputAssembly = "AllCompiledTTPlugins";
                    CompileOnce(configuration.SourceFiles, configuration, compilerParams, csProvider, result);
                }
                else
                {
                    foreach (string sourceFile in configuration.SourceFiles)
                    {
                        compilerParams.OutputAssembly = Path.GetFileNameWithoutExtension(sourceFile);
                        CompileOnce(new List<string>() { sourceFile }, configuration, compilerParams, csProvider, result);
                    }
                }
            }
            catch (Exception e)
            {
                result.GenericCompilationFailure = true;
            }

            // Clear temporary reference assembly files if applicable
            if (configuration.ClearTemporaryFilesWhenDone)
                ClearTemporaryCompileFiles();
            
            return result;
        }
        
        private static void CompileOnce(List<string> sourceFiles, HPluginCompilationConfiguration configuration, CompilerParameters compilerParams, CSharpCodeProvider csProvider, HPluginCompilationResult result)
        {
            CompilerResults compileResult = csProvider.CompileAssemblyFromFile(compilerParams, sourceFiles.ToArray());

            if (compileResult.Errors.HasErrors)
            {
                foreach (CompilerError error in compileResult.Errors)
                    result.CompileErrors.Add(error);
            }
            else
            {
                // Get the compiled assembly
                Assembly asm = compileResult.CompiledAssembly;
                result.CompiledAssemblies.Add(asm);

                // Then go through all compiled HPlugin types and deduce the relative path of each one so that it can be associated with its savedata
                foreach (Type type in asm.GetTypes().Where(t => t.IsClass && t.IsSubclassOf(typeof(HPlugin))).ToList())
                {
                    string relPath = "";
                    try
                    {
                        HPlugin dummyInstance = Activator.CreateInstance(type) as HPlugin;
                        string sourcePath = dummyInstance.GetSourceFilePath();
                        string standardizedSourcePath = Path.GetFullPath(sourcePath);
                        string standardizedRootDir = Path.GetFullPath(configuration.UserFilesRootDirectory);
                        int spot = standardizedSourcePath.IndexOf(standardizedRootDir);
                        if (spot >= 0)
                            relPath = (standardizedSourcePath.Substring(0, spot) + standardizedSourcePath.Substring(spot + standardizedRootDir.Length)).TrimStart('\\', '/');
                    }
                    catch (Exception e) { } // Just swallow it. The plugin probably broke some protocol and thus will not have savedata.
                    result.CompiledTypesSourceFileRelativePaths[type.FullName] = relPath;
                }
            }
        }
        
        /// <summary>
        /// Deletes all files inside the TemporaryFilesPath directory, then removes the directory.
        /// </summary>
        /// <returns>True if no errors occured, false if otherwise.</returns>
        public static bool ClearTemporaryCompileFiles()
        {
            try
            {
                if (Directory.Exists(TemporaryFilesPath))
                {
                    DirectoryInfo topDirInfo = new DirectoryInfo(TemporaryFilesPath);
                    topDirInfo.Delete(true);
                }
                
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
