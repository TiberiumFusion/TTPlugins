using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins
{
    internal static class Extensions
    {
        /// <summary>
        /// Gets the *real* bytes that constitute an Assembly using a rather ugly hack.
        /// </summary>
        /// <param name="assembly">The assembly to turn into a byte[].</param>
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
    }
}
