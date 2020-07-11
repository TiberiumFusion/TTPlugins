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
    public class HPluginApplicatorResult
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
        /// List of HPlugins (by their full type name) that failed to construct in the Activator.
        /// </summary>
        public List<string> HPluginsThatFailedConstruction = new List<string>();

        /// <summary>
        /// List of HPlugins (by their relative source file path) that threw exceptions during their Initialize() (and thus don't have a valid Identity to use).
        /// </summary>
        public Dictionary<string, Exception> HPluginsThatFailedInitialize = new Dictionary<string, Exception>();

        /// <summary>
        /// List of HPlugins (by their relative source file path) that could not have their on-disk configuration loaded and the corresponding exception.
        /// </summary>
        public Dictionary<string, Exception> HPluginsWithFailedConfigurationLoads = new Dictionary<string, Exception>();

        /// <summary>
        /// List of HPlugins (by their relative source file path) that threw exceptions in their ConfigurationLoaded().
        /// </summary>
        public Dictionary<string, Exception> HPluginsThatFailedConfigurationLoaded = new Dictionary<string, Exception>();

        /// <summary>
        /// List of HPlugins (by their relative source file path) that threw exceptions in their PrePatch().
        /// </summary>
        public Dictionary<string, Exception> HPluginsThatFailedPrePatch = new Dictionary<string, Exception>();

        /// <summary>
        /// List of HPlugins (by their relative source file path) that tried to do things they shouldn't do.
        /// </summary>
        public Dictionary<string, string> HPluginsThatBrokeRules = new Dictionary<string, string>();

        /// <summary>
        /// List of HPlugins (by their relative source file path) that tried to patch null MethodInfos.
        /// </summary>
        public List<string> HPluginsThatTriedToPatchNullMethodInfos = new List<string>();

        /// <summary>
        /// List of HPlugins (by their relative source file path) that threw exceptions while Harmony.Patch was trying to patch them.
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
    /// All possible result codes from plugin application.
    /// </summary>
    public enum HPluginApplicatorResultCodes
    {
        /// <summary>
        /// No outstanding errors occurred.
        /// </summary>
        Success = 0,

        /// <summary>
        /// An unexpected error occurred.
        /// </summary>
        GenericFailure = 1000,

        /// <summary>
        /// Could not instantiate Harmony.
        /// </summary>
        CreateHarmonyInstanceFailure = 1001,

        /// <summary>
        /// An unexpected error occurred, specifically during plugin application (i.e. after preparation).
        /// </summary>
        GenericHPluginApplicationFailure = 2000,
    }
}
