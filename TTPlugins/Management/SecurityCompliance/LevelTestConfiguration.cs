using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.tiberiumfusion.ttplugins.Management;
using Mono.Cecil;

namespace com.tiberiumfusion.ttplugins.Management.SecurityCompliance
{
    /// <summary>
    /// A configuration object which contains the parameters for testing a single security level.
    /// </summary>
    public class LevelTestConfiguration
    {
        /// <summary>
        /// A list of namespaces which code in the <see cref="SubjectAssembly"/> is forbidden from using. This includes types, subnamespaces, types of subnamespaces, and recursions thereof.
        /// </summary>
        public List<string> RestrictedNamespaces { get; set; } = new List<string>();

        /// <summary>
        /// A list of types (by their full name w/o assembly name) which code in the <see cref="SubjectAssembly"/> is forbidden from using.
        /// </summary>
        public List<string> RestrictedTypes { get; set; } = new List<string>();

        /// <summary>
        /// A list of methods (by their CLR name) which code in the <see cref="SubjectAssembly"/> is forbidden from using.
        /// </summary>
        public List<string> RestrictedMethods { get; set; } = new List<string>();

        /// <summary>
        /// A list of namespaces which code in the <see cref="SubjectAssembly"/> is allowed to use, including types, subnamespaces, types of subnamespaces, and recursions thereof.
        /// </summary>
        /// <remarks>
        /// The types, subnamespaces, types of subnamespaces, and recursions thereof derived from the items in this list are considered exempt from restriction, even if those explicit and implicit items appear in <see cref="RestrictedNamespaces"/> or <see cref="RestrictedTypes"/>.
        /// </remarks>
        public List<string> WhitelistedNamespaces { get; set; } = new List<string>();

        /// <summary>
        /// A list of types which code in the <see cref="SubjectAssembly"/> is allowed to use.
        /// </summary>
        /// <remarks>
        /// The types of the items in this list are considered exempt from restriction, even if those items appear in <see cref="RestrictedTypes"/> or belong to namespaces appearing in <see cref="RestrictedNamespaces"/>.
        /// </remarks>
        public List<string> WhitelistedTypes { get; set; } = new List<string>();
    }
}
