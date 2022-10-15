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

namespace com.tiberiumfusion.ttplugins.Management.SecurityCompliance
{
    /// <summary>
    /// Container of the security level tests.
    /// </summary>
    public static class CecilTests
    {
        #region Helpers

        /// <summary>
        /// Helper that checks if the provided <paramref name="deeperNamespace"/> is inside any namespace in the provided list of <paramref name="shallowerNamespaces"/>.
        /// </summary>
        /// <param name="deeperNamespace">The deeper namespace to test for existence in the shallower namespace(s).</param>
        /// <param name="shallowerNamespaces">The shallower namespace(s) that the deeper namespace may potentially be a part of.</param>
        /// <param name="firstFound">If return is true, this will be the first shallow namespace in <paramref name="shallowerNamespaces"/> found that contains <paramref name="deeperNamespace"/>. If return is false, this will be the first shallow namespace in <paramref name="ignoreNamespaces"/> found that contains <paramref name="deeperNamespace"/>.</param>
        /// <returns>True if any namespace in <paramref name="shallowerNamespaces"/> was found in <paramref name="deeperNamespace"/>. False otherwise.</returns>
        private static bool IsNamespacePartOfOtherNamespace(string deeperNamespace, List<string> shallowerNamespaces, out string firstFound)
        {
            foreach (string shallowNS in shallowerNamespaces)
            {
                if (deeperNamespace.IndexOf(shallowNS) == 0)
                {
                    firstFound = shallowNS;
                    return true;
                }
            }

            firstFound = null;
            return false;
        }

        /// <summary>
        /// Helper that checks if the specified object CLR name is present in the provided in the list of object CLR names.
        /// </summary>
        /// <remarks>
        /// This helper "canonicalizes" CLR name strings by removing potential "[]" on the end of type names that indicates arrays.
        /// </remarks>
        /// <returns>True if canonicalized <paramref name="clrName"/> is in canonicalized <paramref name="checkClrNames"/>. False otherwise.</returns>
        private static bool IsClrNameInList(string clrName, List<string> checkClrNames)
        {
            string canonClrName = clrName;
            if (clrName.Length > 2 && clrName.EndsWith("[]"))
                canonClrName = clrName.Substring(0, clrName.Length - 2);
            
            foreach (string checkClrName in checkClrNames)
            {
                string canonCheckClrName = checkClrName;
                if (checkClrName.Length > 2 && checkClrName.EndsWith("[]"))
                    canonCheckClrName = checkClrName.Substring(0, clrName.Length - 2);

                if (canonClrName == canonCheckClrName)
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets the generic arguments (if any) involved in the provided type's signature.
        /// </summary>
        /// <param name="typeRef">The <see cref="TypeReference"/> to explore.</param>
        /// <returns>A set of TypeReferences corresponding to a flattened view of <paramref name="typeRef"/>'s generic arguments (if any).</returns>
        private static HashSet<TypeReference> FindTypeGenericArguments(TypeReference typeRef)
        {
            HashSet<TypeReference> seen = new HashSet<TypeReference>();
            FindTypeGenericArgument_Inner(true, typeRef, null, seen);

            return seen;
        }
        private static void FindTypeGenericArgument_Inner(bool first, TypeReference rootTypeRef, TypeReference typeRef, HashSet<TypeReference> seen)
        {
            if (!seen.Contains(typeRef) && (first || typeRef != rootTypeRef))
            {
                if (!first)
                    seen.Add(typeRef);

                TypeReference checkTypeRef = typeRef;
                if (first)
                    checkTypeRef = rootTypeRef;

                if (checkTypeRef.IsGenericInstance)
                {
                    foreach (TypeReference genericArg in (checkTypeRef as GenericInstanceType).GenericArguments)
                        FindTypeGenericArgument_Inner(false, rootTypeRef, genericArg, seen);
                }
            }
        }

        #endregion


        #region Violation messages

        /// <summary>
        /// Creates an error message that describes a security level violation originating from an MSIL instruction.
        /// </summary>
        /// <param name="methodDef">The Method that contains the offending Instruction.</param>
        /// <param name="ins">The offending Instruction.</param>
        /// <param name="remarks">Additional info.</param>
        /// <returns>The created error message.</returns>
        private static string CreateInstructionViolationMessage(MethodDefinition methodDef, Instruction ins, string remarks)
        {
            string location = "";
            
            // Cecil 0.10+ breaking API change: Instruction.SequencePoint is removed
            SequencePoint seqPoint = null;
            try { seqPoint = methodDef.DebugInformation.GetSequencePoint(ins); } // In case the method is missing debug information or it is corrupt in some way
            catch (Exception e) { /* Swallow */ }

            if (seqPoint != null && seqPoint.Document != null)
                location = "\nLocation: " + seqPoint.Document.Url + " at line(s) " + seqPoint.StartLine + "-" + seqPoint.EndLine + ", position " + seqPoint.StartColumn;

            return "Violation in Type: " + methodDef.DeclaringType.FullName
                 + "\n    in Method: " + methodDef.Name
                 + "\n    at Instruction: " + ins.ToString()
                 + "\nRemarks: " + remarks
                 + location;
        }

        /// <summary>
        /// Creates an error message that describes a security level violation originating from an Attribute on a type.
        /// </summary>
        /// <param name="typeDef">The Type that contains the offending Attribute.</param>
        /// <param name="attribute">The offending Attribute.</param>
        /// <param name="remarks">Additional info.</param>
        /// <returns>The created error message.</returns>
        private static string CreateTypeAttributeViolationMessage(TypeDefinition typeDef, CustomAttribute attribute, CustomAttributeArgument? arg, string remarks)
        {
            string argInfo = "";
            if (arg != null)
                argInfo = "on Argument: " + ((CustomAttributeArgument)arg).Type.ToString() + " " + ((CustomAttributeArgument)arg).Value.ToString();
            return "Violation in Type: " + typeDef.FullName
                 + "\n    at Attribute: " + attribute.ToBetterString()
                 + argInfo
                 + "\nRemarks: " + remarks;
        }

        /// <summary>
        /// Creates an error message that describes a security level violation originating from an Attribute on a method.
        /// </summary>
        /// <param name="methodDef">The Method that contains the offending Attribute.</param>
        /// <param name="attribute">The offending Attribute.</param>
        /// <param name="remarks">Additional info.</param>
        /// <returns>The created error message.</returns>
        private static string CreateMethodAttributeViolationMessage(MethodDefinition methodDef, CustomAttribute attribute, CustomAttributeArgument? arg, string remarks)
        {
            string argInfo = "";
            if (arg != null)
                argInfo = "on Argument: " + ((CustomAttributeArgument)arg).Type.ToString() + " " + ((CustomAttributeArgument)arg).Value.ToString();
            return "Violation in Type: " + methodDef.DeclaringType.FullName
                 + "\n    in Method: " + methodDef.Name
                 + "\n    at Attribute: " + attribute.ToBetterString()
                 + argInfo
                 + "\nRemarks: " + remarks;
        }
        
        /// <summary>
        /// Creates an error message that describes a security level violation originating from a local variable in a method.
        /// </summary>
        /// <param name="methodDef">The Method that contains the offending local variable.</param>
        /// <param name="var">The offending local variable.</param>
        /// <param name="remarks">Additional info.</param>
        /// <returns>The created error message.</returns>
        private static string CreateMethodLocalViolationMessage(MethodDefinition methodDef, VariableDefinition var, string remarks)
        {
            return "Violation in Type: " + methodDef.DeclaringType.FullName
                 + "\n    in Method: " + methodDef.Name
                 + "\n    at local variable: #" + var.Index + " " + var.VariableType.ToString()
                 + "\nRemarks: " + remarks;
        }

        #endregion


        #region Micro tests

        /// <summary>
        /// Local helper class for type testing.
        /// </summary>
        private class TypeViolations
        {
            public string InRestrictedNamespace = null;
            public bool IsTypeRestricted = false;
            public bool IsAGenericArgument = false;

            public bool HasViolations { get { return (!string.IsNullOrWhiteSpace(InRestrictedNamespace) || IsTypeRestricted); } }
        }

        /// <summary>
        /// Determines if the provided type is restricted.
        /// </summary>
        /// <remarks>
        /// <para>If the type is directly restricted by type name or indirectly restricted by existence in a restricted namespace, and the type is not directly or indirectly whitelisted, the type will fail the test.</para>
        /// <para>If the type has generic arguments, the types of those arguments (and the types of recursive generic arguments, if any) will be tested as well as the base type without any specialization. All tested types must pass in order for the tested <paramref name="typeRef"/> to pass as a whole.</para>
        /// </remarks>
        /// <param name="typeRef">The type to be tested.</param>
        /// <param name="testConfig">The configuration for testing the provided type.</param>
        /// <param name="violations">A dictionary of the results. Only tested types with violations are present. Each violating type is keyed to a basic object describing the type of violation and if the offending type was part of generic type arguments.</param>
        /// <returns>True if any violations were false (fail). False otherwise (pass).</returns>
        private static bool IsTypeRestricted(TypeReference typeRef, LevelTestConfiguration testConfig, out Dictionary<TypeReference, TypeViolations> violations)
        {
            violations = new Dictionary<TypeReference, TypeViolations>();


            //
            // Determine types to test
            //

            Dictionary<TypeReference, bool> typesToTest = new Dictionary<TypeReference, bool>(); // type -> is generic arg

            // We test both the type itself
            typesToTest[typeRef] = false;

            // And any generic arguments it may have
            foreach (TypeReference genericArg in FindTypeGenericArguments(typeRef))
                typesToTest[genericArg] = true;


            //
            // Check each type against the restricted lists and potential whitelists
            //

            bool restrictedNamespacesEnabled = (testConfig.RestrictedNamespaces != null && testConfig.RestrictedNamespaces.Count > 0);
            bool restrictedTypesEnabled = (testConfig.RestrictedTypes != null && testConfig.RestrictedTypes.Count > 0);

            bool whitelistedNamespacesEnabled = (testConfig.WhitelistedNamespaces != null && testConfig.WhitelistedNamespaces.Count > 0);
            bool whitelistedTypesEnabled = (testConfig.WhitelistedTypes != null && testConfig.WhitelistedTypes.Count > 0);
            
            foreach (TypeReference testTypeRef in typesToTest.Keys)
            {
                // First check if the type is whitelisted (either directly or by being in a whitelisted namespace)
                if (whitelistedTypesEnabled && IsClrNameInList(testTypeRef.FullName, testConfig.WhitelistedTypes))
                    continue;
                if (whitelistedNamespacesEnabled && IsNamespacePartOfOtherNamespace(testTypeRef.Namespace, testConfig.WhitelistedNamespaces, out string dummy))
                    continue;
                
                // Types that are not whitelisted (again, either directly or indirectly) must not violate any direct type restriction or indirect namespace restriction
                TypeViolations typeViolations = new TypeViolations();
                typeViolations.IsAGenericArgument = typesToTest[testTypeRef];

                // Check the type itself
                if (restrictedTypesEnabled && IsClrNameInList(testTypeRef.FullName, testConfig.RestrictedTypes))
                    typeViolations.IsTypeRestricted = true;

                // Check the namespace of the type
                if (restrictedNamespacesEnabled && IsNamespacePartOfOtherNamespace(testTypeRef.Namespace, testConfig.RestrictedNamespaces, out string firstFound))
                    typeViolations.InRestrictedNamespace = firstFound;
                
                if (typeViolations.HasViolations)
                    violations[testTypeRef] = typeViolations;
            }

            return (violations.Count > 0);
        }

        #endregion


        #region Midi tests

        /// <summary>
        /// Checks the provided type's attributes for use of restricted types.
        /// </summary>
        /// <param name="typeDef">The <see cref="TypeDefinition"/> whose attributes will be checked.</param>
        /// <param name="testConfig">The configuration for testing the provided type.</param>
        /// <param name="messages">A list of messages to append new violation information to.</param>
        /// <returns>True if no violations were false (pass). False otherwise (fail).</returns>
        private static bool TestTypeAttributesForViolations(TypeDefinition typeDef, LevelTestConfiguration testConfig, List<string> messages)
        {
            bool pass = true;

            foreach (CustomAttribute attribute in typeDef.CustomAttributes)
            {
                // Check the type of the attribute itself
                if (IsTypeRestricted(attribute.AttributeType, testConfig, out Dictionary<TypeReference, TypeViolations> violations))
                {
                    pass = false;
                    foreach (KeyValuePair<TypeReference, TypeViolations> item in violations)
                    {
                        if (item.Value.InRestrictedNamespace != null)
                        {
                            messages.Add(CreateTypeAttributeViolationMessage(typeDef, attribute, null,
                                !item.Value.IsAGenericArgument
                                ? "Type attribute is of a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of attribute)"
                                : "Type attribute is of a type with a generic argument of type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of generic attribute of type \"" + item.Key.FullName + "\")"
                            ));
                        }
                        if (item.Value.IsTypeRestricted)
                        {
                            messages.Add(CreateTypeAttributeViolationMessage(typeDef, attribute, null,
                                !item.Value.IsAGenericArgument
                                ? "Type attribute is of a disallowed type \"" + item.Key.FullName + "\". (Type of attribute)"
                                : "Type attribute is of a type with a generic argument of disallowed type \"" + item.Key.FullName + "\". (Type of generic attribute argument type on \"" + attribute.AttributeType.FullName + "\")"
                            ));
                        }
                    }
                }
                
                // Check the arguments that were passed to the attribute's ctor
                for (int i = 0; i < attribute.ConstructorArguments.Count; i++)
                {
                    CustomAttributeArgument arg = attribute.ConstructorArguments[i];

                    if (IsTypeRestricted(arg.Type, testConfig, out Dictionary<TypeReference, TypeViolations> violations2))
                    {
                        pass = false;
                        foreach (KeyValuePair<TypeReference, TypeViolations> item in violations2)
                        {
                            if (item.Value.InRestrictedNamespace != null)
                            {
                                messages.Add(CreateTypeAttributeViolationMessage(typeDef, attribute, arg,
                                    !item.Value.IsAGenericArgument
                                    ? "Type attribute parameter is of a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of parameter " + i + " to attribute constructor)"
                                    : "Type attribute parameter is of a type with a generic argument of a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of generic attribute of type \"" + item.Key.FullName + "\" of parameter " + i + " of attribute constructor)"
                                ));
                            }
                            if (item.Value.IsTypeRestricted)
                            {
                                messages.Add(CreateTypeAttributeViolationMessage(typeDef, attribute, arg,
                                    !item.Value.IsAGenericArgument
                                    ? "Type attribute parameter is of disallowed type \"" + item.Key.FullName + "\". (Type of parameter " + i + " to attribute constructor)"
                                    : "Type attribute parameter is of a type with a generic argument of disallowed type \"" + item.Key.FullName + "\". (Type of generic attribute of parameter " + i + " of attribute constructor)"
                                ));
                            }
                        }
                    }
                }
            }

            return pass;
        }

        /// <summary>
        /// Checks the provided method's attributes for use of restricted types.
        /// </summary>
        /// <param name="methodDef">The <see cref="MethodDefinition"/> whose attributes will be checked.</param>
        /// <param name="testConfig">The configuration for testing the provided type.</param>
        /// <param name="messages">A list of messages to append new violation information to.</param>
        /// <returns>True if no violations were false (pass). False otherwise (fail).</returns>
        private static bool TestMethodAttributesForViolations(MethodDefinition methodDef, LevelTestConfiguration testConfig, List<string> messages)
        {
            bool pass = true;

            foreach (CustomAttribute attribute in methodDef.CustomAttributes)
            {
                // Check the type of the attribute itself
                if (IsTypeRestricted(attribute.AttributeType, testConfig, out Dictionary<TypeReference, TypeViolations> violations))
                {
                    pass = false;
                    foreach (KeyValuePair<TypeReference, TypeViolations> item in violations)
                    {
                        if (item.Value.InRestrictedNamespace != null)
                        {
                            messages.Add(CreateMethodAttributeViolationMessage(methodDef, attribute, null,
                                !item.Value.IsAGenericArgument
                                ? "Method attribute is of a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of attribute)"
                                : "Method attribute is of a type with a generic argument of type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of generic attribute of type \"" + item.Key.FullName + "\")"
                            ));
                        }
                        if (item.Value.IsTypeRestricted)
                        {
                            messages.Add(CreateMethodAttributeViolationMessage(methodDef, attribute, null,
                                !item.Value.IsAGenericArgument
                                ? "Method attribute is of disallowed type \"" + item.Key + "\". (Type of attribute)"
                                : "Method attribute is of a type with a generic argument of disallowed type \"" + item.Key.FullName + "\". (Type of generic attribute argument type on \"" + attribute.AttributeType.FullName + "\")"
                            ));
                        }
                    }
                }

                // Check the arguments that were passed to the attribute's ctor
                for (int i = 0; i < attribute.ConstructorArguments.Count; i++)
                {
                    CustomAttributeArgument arg = attribute.ConstructorArguments[i];

                    if (IsTypeRestricted(arg.Type, testConfig, out Dictionary<TypeReference, TypeViolations> violations2))
                    {
                        pass = false;
                        foreach (KeyValuePair<TypeReference, TypeViolations> item in violations2)
                        {
                            if (item.Value.InRestrictedNamespace != null)
                            {
                                messages.Add(CreateMethodAttributeViolationMessage(methodDef, attribute, arg,
                                    !item.Value.IsAGenericArgument
                                    ? "Method attribute parameter is of a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of parameter " + i + " to attribute constructor)"
                                    : "Methoda attribute parameter is of a type with a generic argument of type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of generic attribute of type \"" + item.Key.FullName + "\" of parameter " + i + " of attribute constructor)"
                                ));
                            }
                            if (item.Value.IsTypeRestricted)
                            {
                                messages.Add(CreateMethodAttributeViolationMessage(methodDef, attribute, arg,
                                    !item.Value.IsAGenericArgument
                                    ? "Method attribute parameter is of disallowed type \"" + item.Key.FullName + "\". (Type of parameter " + i + " to attribute constructor)"
                                    : "Method attribute parameter is of a type with a generic argument of disallowed type \"" + item.Key.FullName + "\". (Type of generic attribute of parameter " + i + " of attribute constructor)"
                                ));
                            }
                        }
                    }
                }
            }

            return pass;
        }

        /// <summary>
        /// Checks the provided type's methods for the use of restricted code.
        /// </summary>
        /// <param name="typeDef">The <see cref="TypeDefinition"/> whose methods will be checked.</param>
        /// <param name="testConfig">The configuration for testing the provided type.</param>
        /// <param name="messages">A list of messages to append new violation information to.</param>
        /// <returns>True if no violations were false (pass). False otherwise (fail).</returns>
        private static bool TestTypeMethodsForViolations(TypeDefinition typeDef, LevelTestConfiguration testConfig, List<string> messages)
        {
            bool pass = true;

            foreach (MethodDefinition methodDef in typeDef.Methods)
            {
                if (methodDef == null || methodDef.Body == null || methodDef.Body.Instructions == null)
                    continue;
                
                // Check method attributes for use of restricted types
                pass &= TestMethodAttributesForViolations(methodDef, testConfig, messages);
                
                // Check method locals for use of restricted types
                foreach (VariableDefinition var in methodDef.Body.Variables)
                {
                    if (IsTypeRestricted(var.VariableType, testConfig, out Dictionary<TypeReference, TypeViolations> violations))
                    {
                        pass = false;
                        foreach (KeyValuePair<TypeReference, TypeViolations> item in violations)
                        {
                            if (item.Value.InRestrictedNamespace != null)
                            {
                                messages.Add(CreateMethodLocalViolationMessage(methodDef, var,
                                    !item.Value.IsAGenericArgument
                                    ? "Local variable is of a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of local variable)"
                                    : "Local variable is of a type with a generic argument of type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of generic attribute of type \"" + item.Key.FullName + "\" of local variable)"
                                ));
                            }
                            if (item.Value.IsTypeRestricted)
                            {
                                messages.Add(CreateMethodLocalViolationMessage(methodDef, var,
                                    !item.Value.IsAGenericArgument
                                    ? "Local variable is of disallowed type \"" + item.Value.InRestrictedNamespace + "\". (Type of local variable)"
                                    : "Local variable is of a type with a generic argument of disallowed type \"" + item.Key.FullName + "\". (Type of generic attribute of type of local variable)"
                                ));
                            }
                        }
                    }
                }

                // Check method body for use of restricted types and methods
                foreach (Instruction ins in methodDef.Body.Instructions)
                {
                    if (ins.Operand != null)
                    {
                        // 3 kinds of operands can include types and methods

                        //
                        // Field access
                        //

                        FieldReference asFieldReference = ins.Operand as FieldReference;
                        if (asFieldReference != null)
                        {
                            // Check the declaring type of the field
                            // - e.g. ldc.i4.1, stsfld <restricted type> to write something like Terraria.Main.netMode = 1; where Terraria.Main is the restricted type, even though the netMode field (Int32) isn't of a restricted type)
                            if (IsTypeRestricted(asFieldReference.DeclaringType, testConfig, out Dictionary<TypeReference, TypeViolations> violations))
                            {
                                pass = false;
                                foreach (KeyValuePair<TypeReference, TypeViolations> item in violations)
                                {
                                    if (item.Value.InRestrictedNamespace != null)
                                    {
                                        messages.Add(CreateInstructionViolationMessage(methodDef, ins,
                                            !item.Value.IsAGenericArgument
                                                ? "Use of field declared on a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of declaring type of field)"
                                                : "Use of field declared on a type with a generic argument of a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of generic argument of declaring type of field)"
                                        ));
                                    }
                                    if (item.Value.IsTypeRestricted)
                                    {
                                        messages.Add(CreateInstructionViolationMessage(methodDef, ins,
                                            !item.Value.IsAGenericArgument
                                                ? "Use of field declared on disallowed type \"" + item.Key.FullName + "\". (Declaring type of field)"
                                                : "Use of field declared on a type with a generic argument of disallowed type \"" + item.Key.FullName + "\". (Type of generic argument of declaring type of field)"
                                        ));
                                    }
                                }
                            }
                            
                            // Check the type of the field itself
                            // - e.g. stfld <restricted type> to do something like: MyField = new Terraria.Main(); where Terraria.Main is the restricted type
                            if (IsTypeRestricted(asFieldReference.FieldType, testConfig, out Dictionary<TypeReference, TypeViolations> violations2))
                            {
                                pass = false;
                                foreach (KeyValuePair<TypeReference, TypeViolations> item in violations2)
                                {
                                    if (item.Value.InRestrictedNamespace != null)
                                    {
                                        messages.Add(CreateInstructionViolationMessage(methodDef, ins,
                                            !item.Value.IsAGenericArgument
                                                ? "Use of field of a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of field)"
                                                : "Use of field of a type with a generic argument of a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of generic argument of type of field)"
                                        ));
                                    }
                                    if (item.Value.IsTypeRestricted)
                                    {
                                        messages.Add(CreateInstructionViolationMessage(methodDef, ins,
                                            !item.Value.IsAGenericArgument
                                                ? "Use of field of disallowed type \"" + item.Key.FullName + "\". (Type of field)"
                                                : "Use of field of a type with a generic argument of disallowed type \"" + item.Key.FullName + "\". (Type of generic argument of type of field)"
                                        ));
                                    }
                                }
                            }
                        }

                        //
                        // Method call
                        //

                        MethodReference asMethodReference = ins.Operand as MethodReference;
                        if (asMethodReference != null)
                        {
                            // Check the type which declares the method
                            // - e.g. call <restricted type> to do something like Terraria.Main.DoUpdate(); where Terraria.Main is the restricted type
                            if (IsTypeRestricted(asMethodReference.DeclaringType, testConfig, out Dictionary<TypeReference, TypeViolations> violations))
                            {
                                pass = false;
                                foreach (KeyValuePair<TypeReference, TypeViolations> item in violations)
                                {
                                    if (item.Value.InRestrictedNamespace != null)
                                    {
                                        messages.Add(CreateInstructionViolationMessage(methodDef, ins,
                                            !item.Value.IsAGenericArgument
                                                ? "Use of method declared on a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of declaring type of method)"
                                                : "Use of method declared on a type with a generic argument of a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of generic argument of declaring type of method)"
                                        ));
                                    }
                                    if (item.Value.IsTypeRestricted)
                                    {
                                        messages.Add(CreateInstructionViolationMessage(methodDef, ins,
                                            !item.Value.IsAGenericArgument
                                                ? "Use of method declared on disallowed type \"" + item.Key.FullName + "\". (Declaring type of method)"
                                                : "Use of method declared on a type with a generic argument of disallowed type \"" + item.Key.FullName + "\". (Type of generic argument of declaring type of method)"
                                        ));
                                    }
                                }
                            }
                            
                            // Check the generic arguments to the method
                            if (asMethodReference.IsGenericInstance)
                            {
                                foreach (TypeReference genericTypeRef in (asMethodReference as GenericInstanceMethod).GenericArguments)
                                {
                                    if (IsTypeRestricted(asMethodReference.DeclaringType, testConfig, out Dictionary<TypeReference, TypeViolations> violations2))
                                    {
                                        pass = false;
                                        foreach (KeyValuePair<TypeReference, TypeViolations> item in violations2)
                                        {
                                            if (item.Value.InRestrictedNamespace != null)
                                            {
                                                messages.Add(CreateInstructionViolationMessage(methodDef, ins,
                                                    !item.Value.IsAGenericArgument
                                                        ? "Use of method with a generic argument of a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of generic argument of method)"
                                                        : "Use of method with a generic argument with a generic argument of a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of generic argument of generic argument of method)"
                                                ));
                                            }
                                            if (item.Value.IsTypeRestricted)
                                            {
                                                messages.Add(CreateInstructionViolationMessage(methodDef, ins,
                                                    !item.Value.IsAGenericArgument
                                                        ? "Use of method with a generic argument of disallowed type \"" + item.Key.FullName + "\". (Type of generic argument of method)"
                                                        : "Use of method with a generic argument with a generic argument of disallowed type \"" + item.Key.FullName + "\". (Type of generic argument of generic argument of method)"
                                                ));
                                            }
                                        }
                                    }
                                }

                                // No need to check for generic method parameters, since they cannot exist without the same generic arguments on the method
                            }

                            // Check the method name itself
                            // - e.g. call System.Reflection.Assembly::Load(); where Load() is the restricted method
                            if (testConfig.RestrictedMethods != null && testConfig.RestrictedMethods.Count > 0)
                            {
                                string[] methodSignatureParts = asMethodReference.FullName.Split(' ');
                                string justMethodName = methodSignatureParts.Last(); // We're only checking the method name here, not the return type or instance/tail/other prefix attributes
                                string justMethodNameWithoutParams = justMethodName.Substring(0, justMethodName.IndexOf('(')); // Strip off the parameters
                                if (IsClrNameInList(justMethodNameWithoutParams, testConfig.RestrictedMethods))
                                {
                                    pass = false;
                                    messages.Add(CreateInstructionViolationMessage(methodDef, ins,
                                        "Use of disallowed method \"" + justMethodNameWithoutParams + "\"."
                                    ));
                                }
                            }
                        }

                        //
                        // Type system
                        //

                        TypeReference asTypeReference = ins.Operand as TypeReference;
                        if (asTypeReference != null)
                        {
                            if (IsTypeRestricted(asTypeReference, testConfig, out Dictionary<TypeReference, TypeViolations> violations))
                            {
                                pass = false;
                                foreach (KeyValuePair<TypeReference, TypeViolations> item in violations)
                                {
                                    if (item.Value.InRestrictedNamespace != null)
                                    {
                                        messages.Add(CreateInstructionViolationMessage(methodDef, ins,
                                            !item.Value.IsAGenericArgument
                                                ? "Reference to a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of reference to type)"
                                                : "Reference to a type with a generic argument of a type in disallowed namespace \"" + item.Value.InRestrictedNamespace + "\". (Namespace of type of generic argument of reference to type)"
                                        ));
                                    }
                                    if (item.Value.IsTypeRestricted)
                                    {
                                        messages.Add(CreateInstructionViolationMessage(methodDef, ins,
                                            !item.Value.IsAGenericArgument
                                                ? "Reference to disallowed type \"" + item.Key.FullName + "\". (Reference to type)"
                                                : "Reference to a type with a generic argument of disallowed type \"" + item.Key.FullName + "\". (Type of generic argument of reference to type)"
                                        ));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return pass;
        }

        #endregion


        #region Macro tests

        /// <summary>
        /// Tests an assembly for use of restricted code.
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <param name="config">The test parameters.</param>
        /// <returns>The results of the test.</returns>
        private static LevelTestResult TestAssembly(AssemblyDefinition asmDef, LevelTestConfiguration config)
        {
            // What we look for:
            // 1. Use of types in restricted namespaces
            // 2. Use of restricted types
            // 3. Use of methods on restricted types
            // 4. Use of methods on types in restricted namespaces
            // 5. Use restricted methods

            // Where we look for it:
            // 1. On type attributes
            // 2. On method attributes
            // 3. In type methods

            bool pass = true;
            List<string> messages = new List<string>();

            foreach (ModuleDefinition modDef in asmDef.Modules)
            {
                foreach (TypeDefinition typeDef in modDef.Types)
                {
                    pass &= TestTypeAttributesForViolations(typeDef, config, messages);
                    pass &= TestTypeMethodsForViolations(typeDef, config, messages);
                }
            }

            return new LevelTestResult(pass, messages);
        }

        #endregion


        #region Test levels

        /// <summary>
        /// Checks for compliance with Security Level 1.
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <returns>True if compliant, false if violated something.</returns>
        public static LevelTestResult TestLevel1(AssemblyDefinition asmDef)
        {
            LevelTestConfiguration config = new LevelTestConfiguration();

            config.RestrictedNamespaces = new List<string>()
            {
                "com.tiberiumfusion",
                "HarmonyLib",
            };

            config.WhitelistedNamespaces = new List<string>()
            {
                "com.tiberiumfusion.ttplugins.HarmonyPlugins",
            };

            config.WhitelistedTypes = new List<string>()
            {
                "HarmonyLib.CodeInstruction",
                "HarmonyLib.CodeInstructionExtensions",
            };
            
            return TestAssembly(asmDef, config);
        }

        /// <summary>
        /// Checks for compliance with Security Level 2.
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <returns>True if compliant, false if violated something.</returns>
        public static LevelTestResult TestLevel2(AssemblyDefinition asmDef)
        {
            LevelTestConfiguration config = new LevelTestConfiguration();

            config.RestrictedNamespaces = new List<string>()
            {
                "Mono.Cecil",
                "System.CodeDom",
            };
            
            config.RestrictedTypes = new List<string>()
            {
                "System.AppDomain",
                "System.AppDomainSetup",
                "System.AppDomainManager",
                "System.AppDomainInitializer",
            };

            config.WhitelistedTypes = new List<string>()
            {
                "HarmonyLib.CodeInstruction",
                "HarmonyLib.CodeInstructionExtensions",
            };

            config.RestrictedMethods = new List<string>()
            {
                "System.Reflection.Assembly::Load",
                "System.Reflection.Assembly::LoadFile",
                "System.Reflection.Assembly::LoadFrom",
                "System.Reflection.Assembly::LoadWithPartialName",
                "System.Reflection.Assembly::LoadWithPartialName",
                "System.Reflection.Assembly::ReflectionOnlyLoad",
                "System.Reflection.Assembly::ReflectionOnlyLoadFrom",
                "System.Reflection.Assembly::UnsafeLoadFrom",
            };

            return TestAssembly(asmDef, config);
        }

        /// <summary>
        /// Checks for compliance with Security Level 3.
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <returns>True if compliant, false if violated something.</returns>
        public static LevelTestResult TestLevel3(AssemblyDefinition asmDef)
        {
            LevelTestConfiguration config = new LevelTestConfiguration();

            config.RestrictedNamespaces = new List<string>()
            {
                "System.IO",
                "System.Net",
                "System.Web",
            };
            
            config.WhitelistedTypes = new List<string>()
            {
                "System.IO.MemoryStream",
            };

            return TestAssembly(asmDef, config);
        }

        /// <summary>
        /// Checks for compliance with Security Level 4.
        /// </summary>
        /// <param name="asmDef">The assembly to test.</param>
        /// <returns>True if compliant, false if violated something.</returns>
        public static LevelTestResult TestLevel4(AssemblyDefinition asmDef)
        {
            LevelTestConfiguration config = new LevelTestConfiguration();

            config.RestrictedNamespaces = new List<string>()
            {
                "System.Reflection",
            };
            
            return TestAssembly(asmDef, config);
        }

        #endregion


        /// <summary>
        /// Tests PluginFile(s) against all security levels so as to determine the maximum security level that will allow a plugin to function.
        /// </summary>
        /// <param name="testConfig">The parameters to be used in this security level compliance test.</param>
        /// <returns>A SecurityLevelComplianceTestResult object containing the test results.</returns>
        public static MultipleTestsResults TestPluginCompliance(PluginTestConfiguration testConfig)
        {
            if (testConfig.TerrariaEnvironment == TerrariaEnvironment.Unspecified)
                throw new InvalidOperationException("Property TerrariaEnvironment of parameter testConfig cannot be TerrariaEnvironment.Unspecified.");

            MultipleTestsResults allResults = new MultipleTestsResults();
            
            bool firstCompile = true;
            foreach (PluginFile pluginFile in testConfig.PluginFilesToTest)
            {
                PluginTestResult singleResult = new PluginTestResult(pluginFile);
                allResults.IndividualResults[pluginFile] = singleResult;

                // Skip plugins which are in source code form when TTPlugins is running inside Terraria
                // The TTPlugins security model dictates that no plugin compilation shall occur after TTApplicator has patched Terraria with TTPlugins and validated the plugins against the user's chosen security level
                if (testConfig.TerrariaEnvironment == TerrariaEnvironment.Online && pluginFile.FileType == PluginFileType.CSSourceFile)
                {
                    allResults.Skipped.Add(pluginFile);
                    continue;
                }

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
                        if (testConfig.AdditionalCompileDependencies != null)
                            compileConfig.ReferencesOnDisk.AddRange(testConfig.AdditionalCompileDependencies); // This is how 0Harmony.dll gets referenced
                        compileConfig.CompilerArguments = ""; // Don't use /optimize since this is a test compile

                        if (firstCompile) // Write the Terraria dependencies to disk on the first compile...
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
        private static void RunCecilTests(PluginTestConfiguration testConfig, AssemblyDefinition asmDef, PluginTestResult singleResult)
        {
            if (testConfig.RunLevel1Test)
            {
                LevelTestResult level1TestResults = TestLevel1(asmDef);
                singleResult.TestedLevel1 = true;
                singleResult.PassLevel1 = level1TestResults.Passed;
                singleResult.MessagesLevel1.AddRange(level1TestResults.Messages);
            }
            if (testConfig.RunLevel2Test)
            {
                LevelTestResult level2TestResults = TestLevel2(asmDef);
                singleResult.TestedLevel2 = true;
                singleResult.PassLevel2 = level2TestResults.Passed;
                singleResult.MessagesLevel2.AddRange(level2TestResults.Messages);
            }
            if (testConfig.RunLevel3Test)
            {
                LevelTestResult level3TestResults = TestLevel3(asmDef);
                singleResult.TestedLevel3 = true;
                singleResult.PassLevel3 = level3TestResults.Passed;
                singleResult.MessagesLevel3.AddRange(level3TestResults.Messages);
            }
            if (testConfig.RunLevel4Test)
            {
                LevelTestResult level4TestResults = TestLevel4(asmDef);
                singleResult.TestedLevel4 = true;
                singleResult.PassLevel4 = level4TestResults.Passed;
                singleResult.MessagesLevel4.AddRange(level4TestResults.Messages);
            }
        }
    }
}
