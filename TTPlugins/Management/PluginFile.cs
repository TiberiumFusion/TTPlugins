using com.tiberiumfusion.ttplugins.HarmonyPlugins;
using Mono.Cecil;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.Management
{
    /// <summary>
    /// A representation of a single plugin file in the user's Plugins folder.
    /// </summary>
    public class PluginFile
    {
        #region Properties

        /// <summary>
        /// Path to the plugin file on the disk.
        /// </summary>
        public string PathToFile { get; private set; }

        /// <summary>
        /// What kind of file the plugin is (source or compiled asm).
        /// </summary>
        public PluginFileType FileType { get; private set; }

        #endregion


        /// <summary>
        /// Creates a new PluginFile object for use in plugin file management.
        /// </summary>
        /// <param name="path">Path to the file on the disk.</param>
        /// <param name="type">What kind of file the plugin is.</param>
        public PluginFile(string path, PluginFileType type)
        {
            PathToFile = path;
            FileType = type;
        }

        /// <summary>
        /// Tests the plugin against all security levels so as to determine the maximum security level that will allow this plugin to function.
        /// </summary>
        /// <param name="terrariaPath">Path to Terraria.exe, which will be referenced by CodeDom during compilation.</param>
        /// <param name="terrariaDependencyAssemblies">List of Terraria.exe's embedded dependency assemblies, which will be temporarily written to disk and reference by CodeDom during compilation.</param>
        /// <returns>A SecurityLevelComplianceTestResult object containing the test results.</returns>
        public SecurityLevelComplianceTestResults TestSecurityLevelCompliance(string terrariaPath, List<byte[]> terrariaDependencyAssemblies)
        {
            SecurityLevelComplianceTestResults result = new SecurityLevelComplianceTestResults();

            try
            {
                byte[] asmBytesToTest = null;

                // If source file, compile it
                if (FileType == PluginFileType.CSSourceFile)
                {
                    HPluginCompilationConfiguration config = new HPluginCompilationConfiguration();
                    config.SingleAssemblyOutput = true;
                    config.SourceFiles.Add(PathToFile);
                    config.ReferencesOnDisk.Add(terrariaPath);
                    config.ReferencesInMemory.AddRange(terrariaDependencyAssemblies);
                    HPluginCompilationResult compileResult = HPluginAssemblyCompiler.Compile(config);
                    if (compileResult.CompiledAssemblies.Count == 0)
                    {
                        result.CompileFailure = true;
                        if (compileResult.CompileErrors.Count > 0)
                        {
                            result.GenericMessages.Add("Failed to compile source CS files. Error details are as follows.");
                            foreach (CompilerError error in compileResult.CompileErrors)
                                result.GenericMessages.Add(error.ToString());
                        }
                        else
                        {
                            result.GenericMessages.Add("Generic failure while compiling source CS files. No error details are available.");
                        }
                        return result;
                    }
                    else
                    {
                        // Turn the Assembly into a byte array
                        Assembly asmToTest = compileResult.CompiledAssemblies[0];
                        BinaryFormatter formatter = new BinaryFormatter();
                        using (MemoryStream memStream = new MemoryStream())
                        {
                            formatter.Serialize(memStream, asmToTest);
                            asmBytesToTest = memStream.ToArray();
                        }
                    }
                }
                // If already a compiled assembly, load its bytes
                else if (FileType == PluginFileType.CompiledAssemblyFile)
                {
                    asmBytesToTest = File.ReadAllBytes(PathToFile);
                }

                ///// Check security compliance with Cecil
                using (MemoryStream memStream = new MemoryStream(asmBytesToTest))
                {
                    AssemblyDefinition asmDef = AssemblyDefinition.ReadAssembly(memStream);

                    // Run tests
                    SecurityComplianceSingleTestResult level1TestResults = SecurityComplianceCecilTests.TestLevel1(asmDef);
                    SecurityComplianceSingleTestResult level2TestResults = SecurityComplianceCecilTests.TestLevel2(asmDef);
                    SecurityComplianceSingleTestResult level3TestResults = SecurityComplianceCecilTests.TestLevel3(asmDef);
                    SecurityComplianceSingleTestResult level4TestResults = SecurityComplianceCecilTests.TestLevel4(asmDef);
                    
                    // Gather results
                    result.PassLevel1 = level1TestResults.Passed;
                    result.PassLevel2 = level2TestResults.Passed;
                    result.PassLevel3 = level3TestResults.Passed;
                    result.PassLevel4 = level4TestResults.Passed;
                    result.MessagesLevel1.AddRange(level1TestResults.Messages);
                    result.MessagesLevel2.AddRange(level2TestResults.Messages);
                    result.MessagesLevel3.AddRange(level3TestResults.Messages);
                    result.MessagesLevel4.AddRange(level4TestResults.Messages);

                    // Make an aggregated generic messag for the user
                    string overallResult = "Final security level test results >> Security Level 1: " + (result.PassLevel1 ? "Compliant" : "Violated");
                    overallResult += "; Security Level 2: " + (result.PassLevel1 ? "Compliant" : "Violated");
                    overallResult += "; Security Level 3: " + (result.PassLevel1 ? "Compliant" : "Violated");
                    overallResult += "; Security Level 4: " + (result.PassLevel1 ? "Compliant" : "Violated") + ".";
                    result.GenericMessages.Add(overallResult);

                    // All done
                }
            }
            catch (Exception e)
            {
                result.GenericTestFailure = true;
                result.GenericMessages.Add("Unexpected error during security level testing: " + e.ToString());
            }

            return result;
        }
    }
}
