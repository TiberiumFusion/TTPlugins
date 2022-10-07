using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace com.tiberiumfusion.ttplugins
{
    internal static class Extensions
    {
        /// <summary>
        /// Gets the *real* bytes that constitute an Assembly using a rather ugly hack.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to turn into a byte[].</param>
        /// <returns>A byte[] containing the assembly's bytes, or null if the operation failed.</returns>
        public static byte[] ToByteArray(this Assembly assembly)
        {
            try
            {
                MethodInfo asmGetRawBytes = assembly.GetType().GetMethod("GetRawBytes", BindingFlags.Instance | BindingFlags.NonPublic);
                object bytesObject = asmGetRawBytes.Invoke(assembly, null);
                return (byte[])bytesObject;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Actually useful version of ToString() for Cecil's CustomAttribute type. The ToString() provided by Cecil simply dumps the type of Mono.Cecil.CustomAttribute instead of showing anything about the instanced CustomAttribute itself.
        /// </summary>
        /// <param name="attribute">The <see cref="Mono.Cecil.CustomAttribute"/> to stringify.</param>
        /// <returns>A string that resembles the way the attribute would have been typed in its source code.</returns>
        public static string ToBetterString(this CustomAttribute attribute)
        {
            if (attribute.HasConstructorArguments)
            {
                ModuleDefinition module = attribute.AttributeType.Module;

                string[] args = new string[attribute.ConstructorArguments.Count];
                for (int i = 0; i < attribute.ConstructorArguments.Count; i++)
                {
                    CustomAttributeArgument arg = attribute.ConstructorArguments[i];
                    if (arg.Type == module.TypeSystem.Boolean)
                        args[i] = (bool)arg.Value ? "true" : "false";
                    else if (arg.Type == module.TypeSystem.Byte)
                        args[i] = arg.Value.ToString();
                    else if (arg.Type == module.TypeSystem.SByte)
                        args[i] = arg.Value.ToString();
                    else if (arg.Type == module.TypeSystem.Char)
                        args[i] = "'" + arg.Value + "'";
                    else if (arg.Type == module.TypeSystem.Int16 || arg.Type == module.TypeSystem.Int32)
                        args[i] = arg.Value.ToString();
                    else if (arg.Type == module.TypeSystem.Int64)
                        args[i] = arg.Value.ToString();
                    else if (arg.Type == module.TypeSystem.UInt16 || arg.Type == module.TypeSystem.UInt32)
                        args[i] = arg.Value.ToString();
                    else if (arg.Type == module.TypeSystem.UInt64)
                        args[i] = arg.Value.ToString();
                    else if (arg.Type == module.TypeSystem.Single)
                        args[i] = arg.Value.ToString() + "f";
                    else if (arg.Type == module.TypeSystem.Double)
                        args[i] = ((double)arg.Value).ToString(".0###############");
                    else if (arg.Type == module.TypeSystem.String)
                        args[i] = "\"" + arg.Value + "\"";
                    else
                        args[i] = arg.Value.ToString();
                }

                return "["
                + attribute.AttributeType.ToString()
                + "("
                + string.Join(", ", args)
                + ")]";
            }
            else
            {
                return "["
                + attribute.AttributeType.ToString()
                + "]";
            }
        }
    }
}
