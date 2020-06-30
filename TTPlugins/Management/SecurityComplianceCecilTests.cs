using com.tiberiumfusion.ttplugins.HarmonyPlugins;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.Management
{
    /// <summary>
    /// Container of the security level tests.
    /// </summary>
    public static class SecurityComplianceCecilTests
    {
        #region Helpers

        /// <summary>
        /// Helper that checks if the provided deeper namespace is inside any namespace in the provided list of shallower namespaces.
        /// </summary>
        /// <param name="deeperNamespace">The deeper namespace to test for existence in the shallower namespace(s).</param>
        /// <param name="shallowerNamespaces">The shallower namespace(s) that the deeper namespace may potentially be a part of.</param>
        /// <param name="ignoreNamespaces">A list of shallower namespace(s) which will be skipped for testing, even if they are part of the shallowerNamespaces list.</param>
        /// <returns>If the deeperNamespace is part of any of the shallowerNamespaces, will return the shallower namespace it was found in. Otherwise, null will be returned.</returns>
        private static string NamespaceIsPartOfOtherNamespace(string deeperNamespace, List<string> shallowerNamespaces, List<string> ignoreNamespaces = null)
        {
            if (ignoreNamespaces != null)
            {
                foreach (string shallowIgnoreNS in ignoreNamespaces)
                {
                    if (deeperNamespace.IndexOf(shallowIgnoreNS) == 0)
                        return null;
                }
            }

            foreach (string shallowNS in shallowerNamespaces)
            {
                if (deeperNamespace.IndexOf(shallowNS) == 0)
                {
                    return shallowNS;
                }
            }
            return null;
        }

        /// <summary>
        /// Creates an error message that describes a security level violation.
        /// </summary>
        /// <param name="typeDef">The Type that contains the offending Instruction.</param>
        /// <param name="methodDef">The Method that contains the offending Instruction.</param>
        /// <param name="ins">The offending Instruction.</param>
        /// <param name="remarks">Additional info.</param>
        /// <returns>The created error message.</returns>
        private static string CreateInstructionViolationMessage(TypeDefinition typeDef, MethodDefinition methodDef, Instruction ins, string remarks)
        {
            string location = "";
            if (ins.SequencePoint != null && ins.SequencePoint.Document != null)
                location = "\nLocation: " + ins.SequencePoint.Document.Url + " at line(s) " + ins.SequencePoint.StartLine + "-" + ins.SequencePoint.EndLine + ", position " + ins.SequencePoint.StartColumn;

            return "Violation in Type: " + typeDef.FullName
                 + "\n    in Method: " + methodDef.Name
                 + "\n    at Instruction: " + ins.ToString()
                 + "\nRemarks: " + remarks
                 + location;
        }

        /// <summary>
        /// Tests an assembly for usage of any fields, methods, or types in restricted namespace(s).
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <param name="restrictedNamespaces">A list of restricted namespaces to check for usage of.</param>
        /// <param name="pass">Reference to the test's ultimate pass flag.</param>
        /// <param name="messages">Reference to test's output messages list.</param>
        private static void TestForRestrictedNamespaces(AssemblyDefinition asmDef, List<string> restrictedNamespaces, List<string> whitelistedNamespaces, ref bool pass, List<string> messages)
        {
            foreach (ModuleDefinition modDef in asmDef.Modules)
            {
                foreach (TypeDefinition typeDef in modDef.Types)
                {
                    // Check at the MSIL level for usage of restricted types
                    foreach (MethodDefinition methodDef in typeDef.Methods)
                    {
                        if (methodDef == null || methodDef.Body == null || methodDef.Body.Instructions == null)
                            continue;

                        foreach (Instruction ins in methodDef.Body.Instructions)
                        {
                            if (ins.Operand != null)
                            {
                                ///// 3 kinds of operands that can include namespaces

                                FieldReference asFieldReference = ins.Operand as FieldReference;
                                if (asFieldReference != null)
                                {
                                    // Check for reference to any field in a restricted namespace
                                    string violatedNamespace = NamespaceIsPartOfOtherNamespace(asFieldReference.DeclaringType.Namespace, restrictedNamespaces, whitelistedNamespaces);
                                    if (violatedNamespace != null)
                                    {
                                        pass = false;
                                        messages.Add(CreateInstructionViolationMessage(typeDef, methodDef, ins, "Use of protected namespace \"" + violatedNamespace + "\" is disallowed."));
                                    }
                                }

                                MethodReference asMethodReference = ins.Operand as MethodReference;
                                if (asMethodReference != null)
                                {
                                    // Check for reference to any method in a restricted namespace
                                    string violatedNamespace = NamespaceIsPartOfOtherNamespace(asMethodReference.DeclaringType.Namespace, restrictedNamespaces, whitelistedNamespaces);
                                    if (violatedNamespace != null)
                                    {
                                        pass = false;
                                        messages.Add(CreateInstructionViolationMessage(typeDef, methodDef, ins, "Use of protected namespace \"" + violatedNamespace + "\" is disallowed."));
                                    }
                                }

                                TypeReference asTypeReference = ins.Operand as TypeReference;
                                if (asTypeReference != null)
                                {
                                    // Check for reference to any type in a restricted namespace
                                    string violatedNamespace = NamespaceIsPartOfOtherNamespace(asTypeReference.Namespace, restrictedNamespaces, whitelistedNamespaces);
                                    if (violatedNamespace != null)
                                    {
                                        pass = false;
                                        messages.Add(CreateInstructionViolationMessage(typeDef, methodDef, ins, "Use of protected namespace \"" + violatedNamespace + "\" is disallowed."));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tests an assembly for usage of any restricted type(s).
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <param name="restrictedTypes">A list of restricted types to check for usage of.</param>
        /// <param name="pass">Reference to the test's ultimate pass flag.</param>
        /// <param name="messages">Reference to test's output messages list.</param>
        private static void TestForRestrictedTypes(AssemblyDefinition asmDef, List<string> restrictedTypes, ref bool pass, List<string> messages)
        {
            foreach (ModuleDefinition modDef in asmDef.Modules)
            {
                foreach (TypeDefinition typeDef in modDef.Types)
                {
                    // Check at the MSIL level for usage of restricted types
                    foreach (MethodDefinition methodDef in typeDef.Methods)
                    {
                        if (methodDef == null || methodDef.Body == null || methodDef.Body.Instructions == null)
                            continue;

                        foreach (Instruction ins in methodDef.Body.Instructions)
                        {
                            if (ins.Operand != null)
                            {
                                ///// 3 kinds of operands that can include type names

                                FieldReference asFieldReference = ins.Operand as FieldReference;
                                if (asFieldReference != null)
                                {
                                    // Check for direct use of the restricted type (e.g. newobj <restricted type> to do something like: new Terraria.Main(); where Terraria.Main is the restricted type)
                                    if (restrictedTypes.Contains(asFieldReference.FieldType.FullName))
                                    {
                                        pass = false;
                                        messages.Add(CreateInstructionViolationMessage(typeDef, methodDef, ins, "Use of protected type \"" + asFieldReference.FieldType.FullName + "\" is disallowed."));
                                    }
                                    // Check for use of a field declared in a restricted type (e.g. ldc.i4.1 -> stsfld <restricted type> to write something like Terraria.Main.netMode = 1; where Terraria.Main is the restricted type, even though the netMode field (Int32) isn't of a restricted type)
                                    if (restrictedTypes.Contains(asFieldReference.DeclaringType.FullName))
                                    {
                                        pass = false;
                                        messages.Add(CreateInstructionViolationMessage(typeDef, methodDef, ins, "Use of protected type \"" + asFieldReference.DeclaringType.FullName + "\" is disallowed."));
                                    }
                                }

                                MethodReference asMethodReference = ins.Operand as MethodReference;
                                if (asMethodReference != null)
                                {
                                    // Check for use of a method declared in a restricted type (e.g. call <restricted type> to do something like Terraria.Main.DoUpdate(); where Terraria.Main is the restricted type)
                                    if (restrictedTypes.Contains(asMethodReference.DeclaringType.FullName))
                                    {
                                        pass = false;
                                        messages.Add(CreateInstructionViolationMessage(typeDef, methodDef, ins, "Use of protected type \"" + asMethodReference.DeclaringType.FullName + "\" is disallowed."));
                                    }
                                }

                                TypeReference asTypeReference = ins.Operand as TypeReference;
                                if (asTypeReference != null)
                                {
                                    // Check for direct use of a restricted type
                                    if (restrictedTypes.Contains(asTypeReference.FullName))
                                    {
                                        pass = false;
                                        messages.Add(CreateInstructionViolationMessage(typeDef, methodDef, ins, "Use of protected type \"" + asTypeReference.FullName + "\" is disallowed."));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        #endregion

        #region Tests

        /// <summary>
        /// Checks for compliance with the Security Level 1.
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <returns>True if compliant, false if violated something.</returns>
        public static SecurityComplianceSingleCecilTestResult TestLevel1(AssemblyDefinition asmDef)
        {
            List<string> restrictedNamespaces = new List<string>()
            {
                "com.tiberiumfusion",
                "HarmonyLib",
            };
            List<string> whitelistedNamespaces = new List<string>()
            {
                "com.tiberiumfusion.ttplugins.HarmonyPlugins",
            };

            bool pass = true;
            List<string> messages = new List<string>();

            TestForRestrictedNamespaces(asmDef, restrictedNamespaces, whitelistedNamespaces, ref pass, messages);
            
            return new SecurityComplianceSingleCecilTestResult(pass, messages);
        }

        /// <summary>
        /// Checks for compliance with the Security Level 2.
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <returns>True if compliant, false if violated something.</returns>
        public static SecurityComplianceSingleCecilTestResult TestLevel2(AssemblyDefinition asmDef)
        {
            List<string> restrictedNamespaces = new List<string>()
            {
                "Mono.Cecil",
                "System.CodeDom",
            };

            List<string> restrictedTypes = new List<string>()
            {
                "System.AppDomain",
                "System.AppDomainSetup",
                "System.AppDomainManager",
                "System.AppDomainInitializer",
                "System.Reflection.Assembly",
            };

            bool pass = true;
            List<string> messages = new List<string>();

            TestForRestrictedNamespaces(asmDef, restrictedNamespaces, null, ref pass, messages);
            TestForRestrictedTypes(asmDef, restrictedTypes, ref pass, messages);

            return new SecurityComplianceSingleCecilTestResult(pass, messages);
        }

        /// <summary>
        /// Checks for compliance with the Security Level 3.
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <returns>True if compliant, false if violated something.</returns>
        public static SecurityComplianceSingleCecilTestResult TestLevel3(AssemblyDefinition asmDef)
        {
            List<string> restrictedNamespaces = new List<string>()
            {
                "System.IO",
                "System.Net",
                "System.Web",
            };

            bool pass = true;
            List<string> messages = new List<string>();

            TestForRestrictedNamespaces(asmDef, restrictedNamespaces, null, ref pass, messages);

            return new SecurityComplianceSingleCecilTestResult(pass, messages);
        }

        /// <summary>
        /// Checks for compliance with the Security Level 4.
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <returns>True if compliant, false if violated something.</returns>
        public static SecurityComplianceSingleCecilTestResult TestLevel4(AssemblyDefinition asmDef)
        {
            List<string> restrictedNamespaces = new List<string>()
            {
                "System.Reflection",
            };

            bool pass = true;
            List<string> messages = new List<string>();

            TestForRestrictedNamespaces(asmDef, restrictedNamespaces, null, ref pass, messages);

            return new SecurityComplianceSingleCecilTestResult(pass, messages);
        }

        #endregion

        /// <summary>
        /// Tests PluginFile(s) against all security levels so as to determine the maximum security level that will allow a plugin to function.
        /// </summary>
        /// <param name="testConfig">The parameters to be used in this security level compliance test.</param>
        /// <returns>A SecurityLevelComplianceTestResult object containing the test results.</returns>
        public static SecurityLevelComplianceTestsResults TestPluginCompliance(SecurityLevelComplianceTestConfiguration testConfig)
        {
            SecurityLevelComplianceTestsResults allResults = new SecurityLevelComplianceTestsResults();
            
            bool firstCompile = true;
            foreach (PluginFile pluginFile in testConfig.PluginFilesToTest)
            {
                SecurityLevelComplianceSingleTestResult singleResult = new SecurityLevelComplianceSingleTestResult(pluginFile);
                allResults.IndividualResults[pluginFile] = singleResult;

                try
                {
                    byte[] asmBytesToTest = null;

                    // If source file, compile it
                    HPluginCompilationResult compileResult = null;
                    if (pluginFile.FileType == PluginFileType.CSSourceFile)
                    {
                        HPluginCompilationConfiguration compileConfig = new HPluginCompilationConfiguration();
                        compileConfig.SingleAssemblyOutput = true;
                        compileConfig.SourceFiles.Add(pluginFile.PathToFile);
                        compileConfig.UserFilesRootDirectory = testConfig.UserFilesRootDirectory;
                        compileConfig.SingleAssemblyOutput = true;
                        compileConfig.DeleteOutputFilesFromDiskWhenDone = false; // Keep the output files so we can load the PDBs for the cecil-based security tests
                        compileConfig.ReferencesOnDisk.Add(testConfig.TerrariaPath);
                        compileConfig.ReferencesInMemory.AddRange(testConfig.TerrariaDependencyAssemblies);
                        if (firstCompile && testConfig.PluginFilesToTest.Count > 1) // Write the Terraria dependencies to disk on the first compile...
                        {
                            compileConfig.ClearTemporaryFilesWhenDone = false;
                            compileConfig.ReuseTemporaryFiles = false;
                        }
                        else // ...and reuse them on subsequent compiles. They will be finally deleted with HPluginAssemblyCompiler.ClearTemporaryCompileFiles() at the end of these tests.
                        {
                            compileConfig.ClearTemporaryFilesWhenDone = false;
                            compileConfig.ReuseTemporaryFiles = true;
                        }
                        compileResult = HPluginAssemblyCompiler.Compile(compileConfig);
                        if (compileResult.CompiledAssemblies.Count == 0)
                        {
                            // Clean up
                            HPluginAssemblyCompiler.ClearTemporaryCompileFiles();
                            HPluginAssemblyCompiler.TryRemoveDirectory(compileResult.OutputDirectory);

                            // Note down the compile failure
                            singleResult.CompileFailure = true;
                            if (compileResult.CompileErrors.Count > 0)
                            {
                                singleResult.GenericMessages.Add("Failed to compile source CS files. Error details are as follows.");
                                foreach (CompilerError error in compileResult.CompileErrors)
                                    singleResult.GenericMessages.Add(error.ToString());
                            }
                            else
                            {
                                singleResult.GenericMessages.Add("Generic failure while compiling source CS files. No error details are available.");
                            }
                            allResults.AnyCompileFailure = true;
                        }
                        else
                        {
                            // Turn the Assembly into a byte array
                            Assembly asmToTest = compileResult.CompiledAssemblies[0];
                            asmBytesToTest = asmToTest.ToByteArray();
                        }
                    }
                    // If already a compiled assembly, load its bytes
                    else if (pluginFile.FileType == PluginFileType.CompiledAssemblyFile)
                    {
                        if (File.Exists(pluginFile.PathToFile))
                        {
                            try { asmBytesToTest = File.ReadAllBytes(pluginFile.PathToFile); }
                            catch (Exception e)
                            {
                                singleResult.GenericTestFailure = true;
                                singleResult.GenericMessages.Add("Could not load assembly \"" + pluginFile.PathToFile + "\" from disk. Details: " + e);
                            }
                        }
                    }

                    if (asmBytesToTest == null) // Can't test assemblies that failed to load
                        continue;

                    ///// Check security compliance with Cecil
                    using (MemoryStream memStream = new MemoryStream(asmBytesToTest))
                    {
                        // Try to load the compiled assembly's associated pdb file (if one exists)
                        string pdbPath = null;
                        if (pluginFile.FileType == PluginFileType.CSSourceFile)
                        {
                            // Since we used SingleAssemblyOutput and we're only testing one plugin at a time, there should be only one pdb file in the compiler's output.
                            pdbPath = compileResult.OutputFilesOnDisk.Where(x => Path.GetExtension(x) == ".pdb").FirstOrDefault();
                        }
                        else if (pluginFile.FileType == PluginFileType.CompiledAssemblyFile)
                        {
                            string dllPath = pluginFile.PathToFile;
                            string checkPdbPath = "";
                            int spot = dllPath.LastIndexOf(".dll");
                            if (spot > -1)
                                checkPdbPath = dllPath.Substring(0, spot) + ".pdb";
                            if (File.Exists(checkPdbPath))
                                pdbPath = checkPdbPath;
                        }

                        AssemblyDefinition asmDef = null;
                        if (pdbPath != null)
                        {
                            // Run tests with the pdb to get helpful info like line numbers
                            using (FileStream pdbStream = new FileStream(pdbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                            {
                                ReaderParameters readerParameters = new ReaderParameters();
                                readerParameters.ReadSymbols = true;
                                readerParameters.SymbolStream = pdbStream;
                                asmDef = AssemblyDefinition.ReadAssembly(memStream, readerParameters);

                                RunCecilTests(testConfig, asmDef, singleResult);
                            }
                        }
                        else
                        {
                            // Run tests without a pdb and just have generic error messages
                            asmDef = AssemblyDefinition.ReadAssembly(memStream);

                            RunCecilTests(testConfig, asmDef, singleResult);
                        }

                        // Make an aggregated generic messag for the user
                        string overallResult = "Final security level test results >> ";
                        overallResult += "Level 1: " + (singleResult.TestedLevel1 ? (singleResult.PassLevel1 ? "Compliant" : "Violated") : "Untested");
                        overallResult += "; Level 2: " + (singleResult.TestedLevel2 ? (singleResult.PassLevel2 ? "Compliant" : "Violated") : "Untested");
                        overallResult += "; Level 3: " + (singleResult.TestedLevel3 ? (singleResult.PassLevel3 ? "Compliant" : "Violated") : "Untested");
                        overallResult += "; Level 4: " + (singleResult.TestedLevel4 ? (singleResult.PassLevel4 ? "Compliant" : "Violated") : "Untested");
                        overallResult += ".";
                        singleResult.GenericMessages.Add(overallResult);

                        // All done
                    }

                    // Clear the on-disk output files that were generated during the assembly compile
                    if (compileResult != null && Directory.Exists(compileResult.OutputDirectory))
                    {
                        DirectoryInfo topDirInfo = new DirectoryInfo(compileResult.OutputDirectory);
                        topDirInfo.Delete(true);
                    }
                }
                catch (Exception e)
                {
                    singleResult.GenericTestFailure = true;
                    singleResult.GenericMessages.Add("Unexpected error during security level testing: " + e.ToString());
                }
                
                firstCompile = false;
            }

            // Delete the temporary disk copies of the Terraria dependencies
            HPluginAssemblyCompiler.ClearTemporaryCompileFiles();

            return allResults;
        }

        /// <summary>
        /// Excised section from TestPluginCompliance. Tests the specified AssemblyDefinition against all chosen security tests.
        /// </summary>
        /// <param name="testConfig">Mirror.</param>
        /// <param name="asmDef">Mirror.</param>
        /// <param name="singleResult">Mirror.</param>
        private static void RunCecilTests(SecurityLevelComplianceTestConfiguration testConfig, AssemblyDefinition asmDef, SecurityLevelComplianceSingleTestResult singleResult)
        {
            // Run the tests
            if (testConfig.RunLevel1Test)
            {
                SecurityComplianceSingleCecilTestResult level1TestResults = SecurityComplianceCecilTests.TestLevel1(asmDef);
                singleResult.TestedLevel1 = true;
                singleResult.PassLevel1 = level1TestResults.Passed;
                singleResult.MessagesLevel1.AddRange(level1TestResults.Messages);
            }
            if (testConfig.RunLevel2Test)
            {
                SecurityComplianceSingleCecilTestResult level2TestResults = SecurityComplianceCecilTests.TestLevel2(asmDef);
                singleResult.TestedLevel2 = true;
                singleResult.PassLevel2 = level2TestResults.Passed;
                singleResult.MessagesLevel2.AddRange(level2TestResults.Messages);
            }
            if (testConfig.RunLevel3Test)
            {
                SecurityComplianceSingleCecilTestResult level3TestResults = SecurityComplianceCecilTests.TestLevel3(asmDef);
                singleResult.TestedLevel3 = true;
                singleResult.PassLevel3 = level3TestResults.Passed;
                singleResult.MessagesLevel3.AddRange(level3TestResults.Messages);
            }
            if (testConfig.RunLevel4Test)
            {
                SecurityComplianceSingleCecilTestResult level4TestResults = SecurityComplianceCecilTests.TestLevel4(asmDef);
                singleResult.TestedLevel4 = true;
                singleResult.PassLevel4 = level4TestResults.Passed;
                singleResult.MessagesLevel4.AddRange(level4TestResults.Messages);
            }
        }
    }
}
