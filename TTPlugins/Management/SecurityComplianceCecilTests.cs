using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Helper that checks if the provided single namespace is inside any namespace in the provided list of namespaces.
        /// </summary>
        /// <param name="deeperNamespace">The deeper namespace to test for existence in the shallower namespace(s).</param>
        /// <param name="shallowerNamespaces">The shallower namespace(s) that the deeper namespace may potentially be a part of.</param>
        /// <returns>If the deeperNamespace is part of any of the shallowerNamespaces, will return the shallower namespace it was found in. Otherwise, null will be returned.</returns>
        private static string NamespaceIsPartOfOtherNamespace(string deeperNamespace, List<string> shallowerNamespaces)
        {
            foreach (string shallowNS in shallowerNamespaces)
            {
                if (deeperNamespace.IndexOf(shallowNS) == 0)
                    return shallowNS;
            }
            return null;
        }

        /// <summary>
        /// Helper that creates an error message that describes a security level violation.
        /// </summary>
        /// <param name="typeDef">The Type that contains the offending Instruction.</param>
        /// <param name="methodDef">The Method that contains the offendeing Instruction.</param>
        /// <param name="ins">The offending Instruction.</param>
        /// <param name="remarks">Additional info.</param>
        /// <returns>The created error message.</returns>
        private static string CreateInstructionViolationMessage(TypeDefinition typeDef, MethodDefinition methodDef, Instruction ins, string remarks)
        {
            return "Violation in Type: \"" + typeDef.FullName + "\" in Method: \"" + methodDef.Name + "\" at Instruction: " + ins.ToString() + "; Remarks: " + remarks;
        }

        /// <summary>
        /// Tests an assembly for usage of any fields, methods, or types in restricted namespace(s).
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <param name="restrictedNamespaces">A list of restricted namespaces to check for usage of.</param>
        /// <param name="pass">Reference to the test's ultimate pass flag.</param>
        /// <param name="messages">Reference to test's output messages list.</param>
        private static void TestForRestrictedNamespaces(AssemblyDefinition asmDef, List<string> restrictedNamespaces, ref bool pass, List<string> messages)
        {
            foreach (ModuleDefinition modDef in asmDef.Modules)
            {
                foreach (TypeDefinition typeDef in modDef.Types)
                {
                    // Check at the MSIL level for usage of restricted types
                    foreach (MethodDefinition methodDef in typeDef.Methods)
                    {
                        foreach (Instruction ins in methodDef.Body.Instructions)
                        {
                            if (ins.Operand != null)
                            {
                                ///// 3 kinds of operands that can include namespaces

                                FieldReference asFieldReference = ins.Operand as FieldReference;
                                if (asFieldReference != null)
                                {
                                    // Check for reference to any field in a restricted namespace
                                    string violatedNamespace = NamespaceIsPartOfOtherNamespace(asFieldReference.DeclaringType.Namespace, restrictedNamespaces);
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
                                    string violatedNamespace = NamespaceIsPartOfOtherNamespace(asMethodReference.DeclaringType.Namespace, restrictedNamespaces);
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
                                    string violatedNamespace = NamespaceIsPartOfOtherNamespace(asTypeReference.Namespace, restrictedNamespaces);
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
                                    // Check for use of a nethod declared in a restricted type (e.g. call <restricted type> to do something like Terraria.Main.DoUpdate(); where Terraria.Main is the restricted type)
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
        public static SecurityComplianceSingleTestResult TestLevel1(AssemblyDefinition asmDef)
        {
            List<string> restrictedNamespaces = new List<string>()
            {
                "com.tiberiumfusion",
                "HarmonyLib",
            };

            bool pass = true;
            List<string> messages = new List<string>();

            TestForRestrictedNamespaces(asmDef, restrictedNamespaces, ref pass, messages);
            
            return new SecurityComplianceSingleTestResult(pass, messages);
        }

        /// <summary>
        /// Checks for compliance with the Security Level 2.
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <returns>True if compliant, false if violated something.</returns>
        public static SecurityComplianceSingleTestResult TestLevel2(AssemblyDefinition asmDef)
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

            TestForRestrictedNamespaces(asmDef, restrictedNamespaces, ref pass, messages);
            TestForRestrictedTypes(asmDef, restrictedTypes, ref pass, messages);

            return new SecurityComplianceSingleTestResult(pass, messages);
        }

        /// <summary>
        /// Checks for compliance with the Security Level 3.
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <returns>True if compliant, false if violated something.</returns>
        public static SecurityComplianceSingleTestResult TestLevel3(AssemblyDefinition asmDef)
        {
            List<string> restrictedNamespaces = new List<string>()
            {
                "System.IO",
                "System.Net",
                "System.Web",
            };

            bool pass = true;
            List<string> messages = new List<string>();

            TestForRestrictedNamespaces(asmDef, restrictedNamespaces, ref pass, messages);

            return new SecurityComplianceSingleTestResult(pass, messages);
        }

        /// <summary>
        /// Checks for compliance with the Security Level 4.
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <returns>True if compliant, false if violated something.</returns>
        public static SecurityComplianceSingleTestResult TestLevel4(AssemblyDefinition asmDef)
        {
            List<string> restrictedNamespaces = new List<string>()
            {
                "System.Reflection",
            };

            bool pass = true;
            List<string> messages = new List<string>();

            TestForRestrictedNamespaces(asmDef, restrictedNamespaces, ref pass, messages);

            return new SecurityComplianceSingleTestResult(pass, messages);
        }

        #endregion
    }
}
