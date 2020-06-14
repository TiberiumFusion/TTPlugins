using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// A data bundle of returned information from the HPluginApplicator
    /// </summary>
    public class HPluginApplicatorResult : MarshalByRefObject
    {
        #region Properties

        /// <summary>
        /// Result code that identifies the type of result.
        /// </summary>
        public HPluginApplicatorResultCodes ResultCode = HPluginApplicatorResultCodes.Success;

        /// <summary>
        /// Thrown exception (if applicable)
        /// </summary>
        public Exception ThrownException = null;

        /// <summary>
        /// Error message (if applicable). Used for non-exception errors.
        /// </summary>
        public string ErrorMessage = null;

        /// <summary>
        /// List of HPlugins (by their unique SavedataIdentity name) that could not have their on-disk configuration loaded and the corresponding exception
        /// </summary>
        public Dictionary<string, Exception> HPluginsWithFailedConfigurationLoads = new Dictionary<string, Exception>();

        /// <summary>
        /// List of HPlugins (by their unique SavedataIdentity name) that threw exceptions while executing their override methods
        /// </summary>
        public Dictionary<string, Exception> HPluginsThatThrewExceptions = new Dictionary<string, Exception>();

        /// <summary>
        /// List of HPlugins (by their unique SavedataIdentity name) that tried to do things they shouldn't do
        /// </summary>
        public Dictionary<string, string> HPluginsThatBrokeRules = new Dictionary<string, string>();

        /// <summary>
        /// List of HPlugins (by their unique SavedataIdentity name) that tried to patch null MethodInfos
        /// </summary>
        public List<string> HPluginsWithNullMethodInfos = new List<string>();

        /// <summary>
        /// List of HPlugins (by their unique SavedataIdentity name) that threw exceptions while Harmony.Patch was trying to patch them
        /// </summary>
        public Dictionary<string, Exception> HPluginsThatDidntPatch = new Dictionary<string, Exception>();

        #endregion


        /// <summary>
        /// Sets the properties to indicate failure from a caught Exception
        /// </summary>
        /// <param name="resultCode">The result code.</param>
        /// <param name="error">The thrown Exception.</param>
        public void ConfigureAsFailure(HPluginApplicatorResultCodes resultCode = HPluginApplicatorResultCodes.GenericFailure, Exception error = null)
        {
            ResultCode = resultCode;
            ThrownException = error;
        }

        /// <summary>
        /// Sets the properties to indicate failure from a non-exception error.
        /// </summary>
        /// <param name="resultCode">The result code.</param>
        /// <param name="error">The error message.</param>
        public void ConfigureAsFailure(HPluginApplicatorResultCodes resultCode = HPluginApplicatorResultCodes.GenericFailure, string error = null)
        {
            ResultCode = resultCode;
            ErrorMessage = error;
        }
    }

    /// <summary>
    /// All result codes.
    /// </summary>
    public enum HPluginApplicatorResultCodes
    {
        Success = 0,
        GenericFailure = 1000,
        DependencyAssemblyLoadFailure = 1001,
        CreateHarmonyInstanceFailure = 1002,
        UsercodeAssemblyLoadError = 1003,
        GenericHPluginApplicationFailure = 2000,
    }
}
