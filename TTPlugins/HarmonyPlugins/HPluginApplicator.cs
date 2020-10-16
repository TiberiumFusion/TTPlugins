using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using HarmonyLib;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// Handles the application of HPlugins as Harmony patches.
    /// </summary>
    public static class HPluginApplicator
    {
        #region Vars

        /// <summary>
        /// The executing Terraria assembly.
        /// </summary>
        internal static Assembly TerrariaAssembly { get; private set; }

        /// <summary>
        /// The proprietary ReLogic assembly, as loaded by Terraria.
        /// </summary>
        internal static Assembly ReLogicAssembly { get; private set; }

        /// <summary>
        /// A list of the XNA assemblies loaded by Terraria.
        /// </summary>
        internal static List<Assembly> XNAAssemblies { get; private set; } = new List<Assembly>();

        /// <summary>
        /// The Harmony instance which was created during patch application
        /// </summary>
        internal static Harmony HarmonyInstance { get; private set; }

        /// <summary>
        /// A list of all HPlugins that were successfully applied.
        /// </summary>
        internal static List<HSupervisedPlugin> AppliedHPlugins { get; private set; } = new List<HSupervisedPlugin>();

        /// <summary>
        /// Dictionary that maps each HPlugin to the HSupervisedPlugin object that manages it.
        /// </summary>
        private static Dictionary<HPlugin, HSupervisedPlugin> HPluginToSupervised = new Dictionary<HPlugin, HSupervisedPlugin>();

        /// <summary>
        /// The HPluginApplicatorConfiguration used in the last ApplyPatches() call.
        /// </summary>
        internal static HPluginApplicatorConfiguration LastConfiguation { get; private set; }

        /// <summary>
        /// The result report from the last time ApplyPatches() was invoked. 
        /// </summary>
        internal static HPluginApplicatorResult LastResult { get; private set; }

        /// <summary>
        /// List of namespaces which HPlugins are not allowed to patch.
        /// </summary>
        private static List<string> ProtectedNamespaces = new List<string>()
        {
            "System",
            "com.tiberiumfusion",
            "HarmonyLib"
        };

        /// <summary>
        /// The Harmony assembly that was embedded into Terraria by TTApplicator.
        /// </summary>
        internal static Assembly EmbeddedHarmonyAssembly { get; private set; }

        /// <summary>
        /// A list of all plugin assemblies that were loaded into the current AppDomain.
        /// </summary>
        internal static List<Assembly> LoadedPluginAssemblies { get; private set; } = new List<Assembly>();
        
        /// <summary>
        /// A list of the paths of all plugin assemblies that were extracted to the disk.
        /// </summary>
        internal static List<string> ExtractedPluginAssemblyPaths { get; private set; } = new List<string>();

        /// <summary>
        /// Flag for debug/release dichotomy of plugin operation.
        /// If true, plugin assembly PDBs will be loaded and some exceptions will be rethrown (for your debugger to catch).
        /// If false, PDBs will be skipped and only outstanding exceptions will be rethrown for the user to see.
        /// </summary>
        internal static bool PluginDebugMode { get; private set; } = false;

        /// <summary>
        /// The plugin Security Level used in the lastest (i.e. current) plugin application process.
        /// HPlugins can get this value via GetCurrentPluginSecurityLevel().
        /// </summary>
        internal static int SecurityLevel { get; private set; } = 1;

        /// <summary>
        /// List of assembly display names from the plugin assemblies config xml file's GACAssembliesToLoad element.
        /// </summary>
        internal static List<string> CommonAsmsInConfigGACList { get; private set; } = new List<string>();

        /// <summary>
        /// Directory where ourself (TTPlugins.dll), Harmony, and all plugin assemblies will be extracted to for loading (to avoid assembly context mismatch issues).
        /// </summary>
        internal static string RuntimeExtractFolder { get; private set; } = ".TTPlugins_RuntimeExtract";

        #endregion


        #region Assembly Resolver
        
        /// <summary>
        /// For helping the CLR load assemblies into the default context if calling code (namely plugin code) needs an assembly that hasn't been loaded yet.
        /// </summary>
        internal static void SetupDomainAssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }
        internal static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var domain = (AppDomain)sender;
            
            // First see if the asm has already been loaded (double-loading assemblies is an easy ticket to unexplainable bugs)
            foreach (var assembly in domain.GetAssemblies())
                if (assembly.FullName == args.Name)
                    return assembly;

            // If it's not loaded already, try going through the asms listed in the embedded config file's GACAssembliesToLoad element (i.e. the asms that were used at plugin compile time) and load any matches
            foreach (string asmDisplayName in CommonAsmsInConfigGACList)
            {
                if (asmDisplayName == args.Name)
                {
                    try
                    {
                        DLog("Loading common assembly for assembly resolver. Asm name: \"" + asmDisplayName + "\"");
                        Assembly asm = Assembly.Load(asmDisplayName);
                        DLog("success", false);
                        return asm;
                    }
                    catch (Exception e)
                    {
                        DLog("failed, Details: " + e);
                    }
                }
            }

            DLog("Unable to resolve assembly: " + args.Name + " for requester: " + args.RequestingAssembly.FullName);

            return null;
        }

        #endregion


        #region Patch Application

        /// <summary>
        /// Method which TTApplicator will patch Terraria into calling. This method will create a HPluginApplicatorConfiguration and call ApplyPatches.
        /// </summary>
        public static void EntryPointForTerraria(int entryCode)
        {
            // At this point, the MSIL patch has loaded TTPlugins from the embedded resources in order to call this static method.
            // We now need to load the rest of what TTApplicator has embedded before calling ApplyPatches

            DLogPrepare();
            DLog("Entered EntryPointForTerraria()");

            //System.Diagnostics.Debugger.Launch();

            try
            {
                // Find the Terraria assembly
                TerrariaAssembly = Assembly.GetCallingAssembly();

                // Find the proprietary ReLogic assembly
                FindLoadedReLogicAssembly();

                // Find XNA assemblies
                FindLoadedXNAAssemblies();

                // Extract plugins assmblies config doc
                DLog("Extracting configuration doc...");
                XDocument pluginAssembliesConfig = null;
                using (var resStream = TerrariaAssembly.GetManifestResourceStream("TTPlugins_EmbeddedPluginsConfig.xml"))
                {
                    pluginAssembliesConfig = XDocument.Load(resStream);
                }
                XElement configDocBase = pluginAssembliesConfig.Element("Base");
                DLog("done", false);

                // Add the gac assemblies specified in the config doc to a store list
                DLog("Storing common assembly names from plugin compile time (most likely sourced from the gac)...");
                XElement GACAssembliesToLoad = configDocBase.Element("GACAssembliesToLoad");
                foreach (XElement asmNameElement in GACAssembliesToLoad.Nodes())
                {
                    string fullAsmName = asmNameElement.Value;
                    DLog("Storing common asm \"" + fullAsmName + "\"...", 1);
                    CommonAsmsInConfigGACList.Add(fullAsmName);
                }
                DLog("Done storing common asm names");

                // Extract Harmony
                DLog("Extracting Harmony assembly to runtime extract dir...");
                EmbeddedHarmonyAssembly = null;
                string harmonyAsmDisplayName = null;
                try
                {
                    using (var resStream = TerrariaAssembly.GetManifestResourceStream("TTPlugins_0Harmony.dll"))
                    {
                        using (var memStream = new MemoryStream())
                        {
                            resStream.CopyTo(memStream);
                            byte[] asmBytes = memStream.ToArray();
                            //EmbeddedHarmonyAssembly = Assembly.Load(asmBytes); // Loading into the neither or loadfrom contexts causes some severe issues
                            // So unfortunately, we're forced to write the asm to disk so we can use Load() to load by display name into the default context
                            // Which means the extracted files are visible to the user and very easy to obtain while Terraria is running, but this cannot be avoided
                            string harmonyAsmPath = Path.Combine(RuntimeExtractFolder, "0Harmony.dll");
                            File.WriteAllBytes(harmonyAsmPath, asmBytes);
                            AssemblyName harmonyAsmName = AssemblyName.GetAssemblyName(harmonyAsmPath); // Get and store the Harmony asm's display name so we can load it with Load() later
                            harmonyAsmDisplayName = harmonyAsmName.FullName;
                            DLog("success", false);
                        }
                    }
                }
                catch (Exception e)
                {
                    DLog("failed, plugin application will be aborted. Details: " + e);
                    if (PluginDebugMode) throw e;
                    return;
                }

                // Get plugin mode config
                DLog("Reading PluginDebugMode from config...");
                PluginDebugMode = bool.Parse(configDocBase.Element("PluginDebugMode")?.Value ?? "false");
                DLog("done, PluginDebugMode is: " + PluginDebugMode, false);

                // Get security level indicator
                DLog("Reading PluginSecurityLevel from config...");
                SecurityLevel = int.Parse(configDocBase.Element("PluginSecurityLevel")?.Value ?? "1");
                DLog("done, PluginSecurityLevel is: " + SecurityLevel, false);

                // Extract the plugin assemblies specified in the config doc
                ExtractedPluginAssemblyPaths.Clear();
                List<string> pluginAsmsToLoadByDisplayNames = new List<string>();
                DLog("Extracting plugin assemblies...");
                XElement PluginAsmResourceNames = configDocBase.Element("PluginAsmResourceNames");
                foreach (XElement resNameElement in PluginAsmResourceNames.Nodes())
                {
                    // Find DLL and PDB resource names
                    string asmResourceName = resNameElement.Value;
                    string pdbResourceName = null;
                    if (PluginDebugMode)
                    {
                        XAttribute pdbNameAttr = resNameElement.Attribute("PDBName");
                        if (pdbNameAttr != null)
                            pdbResourceName = pdbNameAttr.Value;
                    }
                    
                    DLog("Processing plugin assembly \"" + asmResourceName + "\"" + ((pdbResourceName != null) ? " (PDB: " + pdbResourceName + ")..." : " (no PDB)..."), 1);

                    // Extract PDB if in debug mode
                    try
                    {
                        if (PluginDebugMode && pdbResourceName != null)
                        {
                            using (var resStream = TerrariaAssembly.GetManifestResourceStream(pdbResourceName))
                            {
                                string extractPDBPath = Path.Combine(RuntimeExtractFolder, pdbResourceName);
                                using (FileStream fileStream = File.Open(extractPDBPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
                                {
                                    DLog("Writing PDB to file \"" + extractPDBPath + "\"...", 2);
                                    resStream.CopyTo(fileStream);
                                }
                                Thread.Sleep(50); // Extra insurance for slow disks
                                DLog("done", false);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        DLog("Error while extracting PDB file to disk. Details: " + e, 2);
                        // Swallow. The pdb may not be available, but the plugin can still be loaded anyways.
                    }

                    // Extract the plugin assembly per the debug/release configuration
                    using (var resStream = TerrariaAssembly.GetManifestResourceStream(asmResourceName))
                    {
                        // Extract the plugin assembly to disk
                        string extractDLLPath = Path.Combine(RuntimeExtractFolder, asmResourceName);
                        using (FileStream fileStream = File.Open(extractDLLPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
                        {
                            DLog("Writing plugin assembly to file \"" + extractDLLPath + "\"...", 2);
                            resStream.CopyTo(fileStream);
                        }
                        Thread.Sleep(50); // Extra insurance for slow disks
                        ExtractedPluginAssemblyPaths.Add(extractDLLPath);
                        AssemblyName asmName = AssemblyName.GetAssemblyName(extractDLLPath); // Get and store the assembly's display name so we can load it with Load()
                        pluginAsmsToLoadByDisplayNames.Add(asmName.FullName);
                        DLog("done", false);
                    }
                }
                DLog("Extract plugin assemblies done");

                // Now load all 0Harmony and all extracted assemblies into the default context using Load()
                DLog("Loading extracted assemblies...");

                // Load Harmony first
                DLog("Loading extracted Harmony assembly...");
                EmbeddedHarmonyAssembly = Assembly.Load(harmonyAsmDisplayName);
                DLog("done", false);

                // Then plugin asms
                LoadedPluginAssemblies.Clear();
                foreach (string pluginAsmDisplayName in pluginAsmsToLoadByDisplayNames)
                {
                    DLog("Loading extracted plugin assembly\" " + pluginAsmDisplayName + "...");
                    Assembly pluginAsm = Assembly.Load(pluginAsmDisplayName);
                    LoadedPluginAssemblies.Add(pluginAsm);
                    DLog("done", false);
                }
                DLog("Load assemblies done");

                // Get temporary plugin files root dir from the config doc
                DLog("Reading PluginTemporaryFilesRootDir from config...");
                string pluginTemporaryFilesRootDir = configDocBase.Element("PluginTemporaryFilesRootDir").Value;
                DLog("done, PluginTemporaryFilesRootDir is: " + ((pluginTemporaryFilesRootDir != null) ? pluginTemporaryFilesRootDir.ToString() : "(null)"), false);

                // Get the hplugin full type name -> plugin relpath mapping from the config doc
                DLog("Creating plugin type to relpath map...");
                Dictionary<string, string> pluginTypeRelPathMap = new Dictionary<string, string>();
                XElement hpluginTypeNameRelPathMap = configDocBase.Element("HPluginTypeNameRelPathMap");
                foreach (XElement item in hpluginTypeNameRelPathMap.Nodes())
                {
                    string typeFullName = item.Attribute("TypeFullName").Value;
                    string relpath = item.Value;
                    pluginTypeRelPathMap[typeFullName] = relpath;
                }
                DLog("Type->relpath map done");

                // Create the plugin applicator config and hit the go button
                DLog("Calling ApplyPatches()");
                HPluginApplicatorConfiguration applicatorConfig = new HPluginApplicatorConfiguration();
                applicatorConfig.ExecutingTerrariaAssembly = TerrariaAssembly;
                applicatorConfig.PluginAssemblies = LoadedPluginAssemblies;
                applicatorConfig.PluginTemporaryFilesRootDirectory = pluginTemporaryFilesRootDir;
                applicatorConfig.PluginTypesRelativePaths = pluginTypeRelPathMap;
                ApplyPatches(applicatorConfig);
            }
            catch (Exception e)
            {
                DLog("Error while applying TTPlugins in EntryPointFromTerraria, details: " + e);
                if (PluginDebugMode) throw e;
            }

            DLogWriteToDisk();
        }
        
        /// <summary>
        /// Applies all HPlugins from the provided compiled assemblies.
        /// </summary>
        public static HPluginApplicatorResult ApplyPatches(HPluginApplicatorConfiguration configuration)
        {
            DLog("Entered ApplyPatches()");

            LastConfiguation = configuration;
            HPluginApplicatorResult result = new HPluginApplicatorResult();
            LastResult = result;


            // Fix weird missing assembly issues
            DLog("Creating assembly resolver...");
            SetupDomainAssemblyResolver();
            DLog("done", false);

            // Create a harmony instance
            DLog("Creating Harmony instance...");
            try
            {
                HarmonyInstance = new Harmony("com.tiberiumfusion.ttplugins.HarmonyPlugins.HPluginApplicator");
                DLog("done", false);
            }
            catch (Exception e)
            {
                DLog("Failed to create Harmony instance. Details: " + e);
                result.ConfigureAsFailure(HPluginApplicatorResultCodes.CreateHarmonyInstanceFailure, e);
                if (PluginDebugMode) throw e;
                return result;
            }


            // Apply framework patches before applying usercode patches
            DLog("Applying framework Harmony patches...");
            Type fwPatches = typeof(HFrameworkPatches);

            DLog("Applying FW_SaveAllPluginConfigs...", 1);
            HarmonyInstance.Patch(GetTerrariaMethod("Terraria.Main", "SaveSettings"), new HarmonyMethod(fwPatches.GetMethod("FW_SaveAllPluginConfigs")));
            DLog("done", false);

            DLog("Applying FW_RemoveRuntimeExtractDirOnQuite...", 1);
            HarmonyInstance.Patch(GetTerrariaMethod("Terraria.Main", "QuitGame"), new HarmonyMethod(fwPatches.GetMethod("FW_RemoveRuntimeExtractDirOnQuite")));
            DLog("done", false);

            DLog("Applying FW_InterceptTimeLoggerDrawException...", 1);
            HarmonyInstance.Patch(GetTerrariaMethod("Terraria.TimeLogger", "DrawException"), new HarmonyMethod(fwPatches.GetMethod("FW_InterceptTimeLoggerDrawException")));
            DLog("done", false);

            DLog("Applying FW_ShowPluginReport...", 1);
            HarmonyInstance.Patch(GetTerrariaMethod("Terraria.Chat.ChatCommandProcessor", "CreateOutgoingMessage"), null, new HarmonyMethod(fwPatches.GetMethod("FW_ShowPluginReport")));
            DLog("done", false);

            DLog("Framework patches done");


            // Find all HPlugins in the compiled assemblies and apply them
            AppliedHPlugins.Clear();
            try
            {
                // Map of each relpath to the plugin config that was loaded for it. Allows for reusing of already loaded configs for multiple types that share the same relpath (i.e. several classes in the same plugin assembly).
                Dictionary<string, XDocument> relpathToLoadedPluginConfig = new Dictionary<string, XDocument>();

                DLog("Processing HPlugins from all loaded plugin assemblies...");
                foreach (Assembly pluginAsm in configuration.PluginAssemblies)
                {
                    DLog("Processing plugin asm: " + pluginAsm.FullName + "(location: " + pluginAsm.Location + ")...", 1);

                    // Find all HPlugins in the usercode assembly
                    DLog("Finding plugin types in asm...", 2);
                    const string unknownRelPathString = "Unknown source file path.";
                    //List<Type> foundPluginTypes = pluginAsm.GetTypes().Where(t => t.IsClass && t.IsSubclassOf(typeof(HPlugin))).ToList();
                    List<Type> foundPluginTypes = pluginAsm.GetTypes().Where(t => t.IsClass && t.IsSubclassOf(typeof(HPlugin))).ToList();
                    DLog("found " + foundPluginTypes.Count + " types", false);
                    foreach (Type pluginType in foundPluginTypes)
                    {
                        DLog("Processing plugin type: " + pluginType.FullName + "...", 3);

                        // Create an instance of the plugin
                        DLog("Creating plugin instance...", 4);
                        HPlugin pluginInstance = Activator.CreateInstance(pluginType) as HPlugin;
                        if (pluginInstance == null)
                        {
                            DLog("Failed to create instance of plugin type: " + pluginType.FullName);
                            result.HPluginsThatFailedConstruction.Add(pluginType.FullName);
                            continue; // Skip over this plugin
                        }
                        DLog("done", false);

                        // Link it with its assembly of origin
                        // We use reflection for this, since exposing a method for setting the PluginAssembly could be easily exploited. This is kind of ugly, but any performance hits are extremely neglible.
                        DLog("Setting plugin.PluginAssembly...", 4);
                        MethodInfo pluginPropertySetter_PluginAssembly = typeof(HPlugin).GetRuntimeMethods().Where(x => x.Name == "set_PluginAssembly").FirstOrDefault();
                        pluginPropertySetter_PluginAssembly?.Invoke(pluginInstance, new object[] { pluginAsm });
                        DLog("done", false);

                        // Find out if it had a valid relative path at compile time that we can use
                        DLog("Finding source file relpath...", 4);
                        string sourceFileRelPath = null;
                        configuration.PluginTypesRelativePaths.TryGetValue(pluginType.FullName, out sourceFileRelPath);
                        if (sourceFileRelPath == null)
                            sourceFileRelPath = unknownRelPathString;
                        DLog("done, sourceFileRelPath is: " + sourceFileRelPath, false);

                        // Wrap it up
                        DLog("Wrapping HPlugin...", 4);
                        HSupervisedPlugin supervisedPlugin = new HSupervisedPlugin(pluginInstance, sourceFileRelPath);
                        HPluginToSupervised[pluginInstance] = supervisedPlugin; // And map it
                        DLog("done", false);

                        // Initialize() tells the plugin to set its informational fields, which need to be established first and foremost
                        DLog("Calling Initialize() on plugin...", 4);
                        try
                        {
                            supervisedPlugin.Plugin.Initialize();
                            DLog("success", false);
                        }
                        catch (Exception e)
                        {
                            DLog("Plugin of type \"" + pluginType.FullName + "\" from source \"" + supervisedPlugin.SourceFileRelativePath + "\" threw an error during Initialize(). Details: " + e);
                            result.HPluginsThatFailedInitialize[supervisedPlugin.SourceFileRelativePath] = e;
                            if (PluginDebugMode) throw e;
                            continue; // Skip over this plugin
                        }

                        // Start plugin with a default configuration doc
                        DLog("Creating blank plugin config doc base...", 4);
                        XDocument pluginConfigurationDoc = CreateBlankPluginConfigDoc();
                        DLog("done", false);

                        // If the plugin has persistent data and a valid relpath, load that now
                        bool successfulConfigLoad = false;
                        if (supervisedPlugin.Plugin.HasPersistentSavedata)
                        {
                            DLog("HPlugin set HasPersistentSavedata to true, loading runtime config from disk...", 4);
                            if (supervisedPlugin.SourceFileRelativePath != null)
                            {
                                // First check if the config for this relpath was already loaded
                                XDocument cachedConfigDoc = null;
                                if (relpathToLoadedPluginConfig.TryGetValue(supervisedPlugin.SourceFileRelativePath, out cachedConfigDoc) && supervisedPlugin.SourceFileRelativePath != unknownRelPathString)
                                {
                                    pluginConfigurationDoc = cachedConfigDoc;
                                }
                                else
                                {
                                    // Try to load the plugin configuration from the disk
                                    try
                                    {
                                        string configurationXMLFilePath = GetConfigurationXMLFilePathForPlugin(supervisedPlugin, configuration);
                                        DLog("Runtime config file should exist at \"" + configurationXMLFilePath + "\"", 5);
                                        if (File.Exists(configurationXMLFilePath)) // Load config if it exists
                                        {
                                            DLog("Loading runtime config file...", 5);
                                            pluginConfigurationDoc = XDocument.Load(configurationXMLFilePath);
                                            DLog("done", false);
                                        }
                                        else
                                        {
                                            DLog("Runtime config file does not exist", 5);
                                        }

                                        successfulConfigLoad = true;
                                    }
                                    catch (Exception e)
                                    {
                                        DLog("Unexpected error while trying to load runtime configuration for plugin of type \"" + pluginType.FullName + "\" from source \"" + supervisedPlugin.SourceFileRelativePath + "\". Details: " + e, 4);
                                        result.HPluginsWithFailedConfigurationLoads[supervisedPlugin.SourceFileRelativePath] = e;
                                        if (PluginDebugMode) throw e;
                                    }
                                }
                            }
                        }

                        // Hold onto the configuration xml doc
                        supervisedPlugin.LatestConfigurationXML = pluginConfigurationDoc;

                        // Setup the Configuration object from the xml doc
                        DLog("Creating plugin configuration...", 4);
                        HPluginConfiguration pluginConfiguration = new HPluginConfiguration(); // Create a new configuration object for the xml config
                        try
                        {
                            DLog("Loading element PluginConfig from xml...", 5);
                            XElement baseElement = pluginConfigurationDoc.Element("PluginConfig");
                            if (baseElement != null)
                            {
                                DLog("done", false);
                                DLog("Loading element Savedata from xml...", 6);
                                XElement savedataElement = baseElement.Element("Savedata");
                                if (savedataElement != null)
                                {
                                    DLog("done", false);
                                    pluginConfiguration.Savedata = savedataElement;
                                }
                                else
                                    DLog("element does not exist", false);
                            }
                            else
                                DLog("element does not exist", false);
                        }
                        catch (Exception e)
                        {
                            DLog("Unexpected error while parsing configuration for plugin of type \"" + pluginType.FullName + "\" from source \"" + supervisedPlugin.SourceFileRelativePath + "\" threw an error during Initialize(). Plugin will continue to run, but will have a blank savedata file with empty defaults. Details: " + e, 4);
                            result.HPluginsWithFailedConfigurationLoads[supervisedPlugin.SourceFileRelativePath] = e;
                            if (PluginDebugMode) throw e;
                            // In this case, the HPlugin will have a default HPluginConfiguration object with an empty savedata XElement
                        }
                        DLog("Plugin configration is ready", 4);

                        // Give the configuration to the HPlugin
                        // Like with setting the origin PluginAssembly, we use reflection for this for security purposes.
                        DLog("Setting plugin.Configuration...", 4);
                        MethodInfo pluginPropertySetter_Configuration = typeof(HPlugin).GetRuntimeMethods().Where(x => x.Name == "set_Configuration").FirstOrDefault();
                        pluginPropertySetter_Configuration?.Invoke(pluginInstance, new object[] { pluginConfiguration });
                        DLog("done", false);

                        // ConfigurationLoaded() lets the plugin know it can safely read Configuration now
                        DLog("Calling ConfigurationLoaded(" + successfulConfigLoad + ") on plugin...", 4);
                        try
                        {
                            supervisedPlugin.Plugin.ConfigurationLoaded(successfulConfigLoad);
                            DLog("success", false);
                        }
                        catch (Exception e)
                        {
                            DLog("Plugin of type \"" + pluginType.FullName + "\" from source \"" + supervisedPlugin.SourceFileRelativePath + "\" threw an error during ConfigurationLoaded(). Details: " + e, 4);
                            result.HPluginsThatFailedConfigurationLoaded[supervisedPlugin.SourceFileRelativePath] = e;
                            if (PluginDebugMode) throw e;
                            continue; // Skip over this plugin
                        }

                        // Now that the plugin is initialized and configured, it is finally time to patch it in with Harmony
                        // But first, we give the plugin one last chance to define its patch operations
                        DLog("Calling PrePatch() on plugin...", 4);
                        try
                        {
                            supervisedPlugin.Plugin.PrePatch();
                            DLog("success", false);
                        }
                        catch (Exception e)
                        {
                            DLog("Plugin of type \"" + pluginType.FullName + "\" from source \"" + supervisedPlugin.SourceFileRelativePath + "\" threw an error during PrePatch(). Details: " + e, 4);
                            result.HPluginsThatFailedPrePatch[supervisedPlugin.SourceFileRelativePath] = e;
                            if (PluginDebugMode) throw e;
                            continue; // Skip over this plugin
                        }

                        // Do all the patch operations defined by the plugin
                        DLog("Processing plugin's patch operations (total: " + supervisedPlugin.Plugin.PatchOperations.Count + ")...", 4);
                        foreach (HPatchOperation patchOp in supervisedPlugin.Plugin.PatchOperations)
                        {
                            string dpatchloc = "";
                            if (patchOp.PatchLocation == HPatchLocation.Prefix) dpatchloc = " (prefix)";
                            else if (patchOp.PatchLocation == HPatchLocation.Postfix) dpatchloc = " (postfix)";
                            string[] dmessage = new string[3];
                            dmessage[0] = "Processing patch op for plugin \"" + pluginType.FullName + "\" from \"" + supervisedPlugin.SourceFileRelativePath + "\"" + dpatchloc + "";
                            if (patchOp.TargetMethod == null)
                                dmessage[1] = " >> Target method: null";
                            else
                                dmessage[1] = " >> Target method: " + patchOp.TargetMethod.DeclaringType.FullName + "." + patchOp.TargetMethod.Name;
                            if (patchOp.StubMethod == null)
                                dmessage[2] = " >> Patch stub method: null";
                            else
                                dmessage[2] = " >> Patch stub method: " + patchOp.StubMethod.DeclaringType.FullName + "." + patchOp.StubMethod.Name;
                            DLog(dmessage, 5);

                            // First validate the patchOp

                            // Ensure both the target and patch stub MethodInfos exist
                            if (patchOp.TargetMethod == null || patchOp.StubMethod == null)
                            {
                                result.HPluginsThatTriedToPatchNullMethodInfos.Add(supervisedPlugin.SourceFileRelativePath);
                                continue; // Skip over this plugin
                            }

                            // Ensure the target method is not in a protected namespace
                            bool brokeRules = false;
                            foreach (string protectedNamespace in ProtectedNamespaces)
                            {
                                if (patchOp.TargetMethod.DeclaringType.Namespace.IndexOf(protectedNamespace) == 0)
                                {
                                    DLog("Plugin of type \"" + pluginType.FullName + "\" from source \"" + supervisedPlugin.SourceFileRelativePath + "\" attempted to patch a protected namespace: \"" + protectedNamespace + "\". Plugin will not be allowed to run.", 6);
                                    result.HPluginsThatBrokeRules[supervisedPlugin.SourceFileRelativePath] = "Tried to patch protected namespace: \"" + protectedNamespace + "\"";
                                    brokeRules = true;
                                    break;
                                }
                                if (brokeRules) break;
                            }
                            if (brokeRules)
                                continue; // Skip over this plugin


                            // At last, do the Harmony patch
                            try
                            {
                                DLog("Applying patch...", 6);
                                if (patchOp.PatchLocation == HPatchLocation.Prefix)
                                    HarmonyInstance.Patch(patchOp.TargetMethod, new HarmonyMethod(patchOp.StubMethod, patchOp.PatchPriority));
                                else if (patchOp.PatchLocation == HPatchLocation.Postfix)
                                    HarmonyInstance.Patch(patchOp.TargetMethod, null, new HarmonyMethod(patchOp.StubMethod, patchOp.PatchPriority));
                                else if (patchOp.PatchLocation == HPatchLocation.Transpiler)
                                    HarmonyInstance.Patch(patchOp.TargetMethod, null, null, new HarmonyMethod(patchOp.StubMethod, patchOp.PatchPriority));

                                if (!AppliedHPlugins.Contains(supervisedPlugin))
                                    AppliedHPlugins.Add(supervisedPlugin);
                                DLog("done", false);
                            }
                            catch (Exception e)
                            {
                                DLog("Error while applying patch operation. Details: " + e, 6);
                                result.HPluginsThatDidntPatch[supervisedPlugin.SourceFileRelativePath] = e;
                                // Carry on to the next plugin

                                // ...but alert the user first
                                string message = "Error while applying patch operation for HPlugin \"" + supervisedPlugin.SourceFileRelativePath + "\"" + dpatchloc + "."
                                    + "\n >> Target method: " + patchOp.TargetMethod.DeclaringType.FullName + "." + patchOp.TargetMethod.Name
                                    + "\n >> Patch stub method: " + patchOp.StubMethod.DeclaringType.FullName + "." + patchOp.StubMethod.Name
                                    + "\nDetails: " + e;

                                if (PluginDebugMode) throw e;
                                else System.Windows.Forms.MessageBox.Show(message);
                            }
                        }
                    }
                }
                DLog("Done processing HPlugins");
            }
            catch (Exception e)
            {
                DLog("An unexpected error occurred during the plugin application process. Details: " + e);
                result.ConfigureAsFailure(HPluginApplicatorResultCodes.GenericHPluginApplicationFailure, e);
                if (PluginDebugMode) throw e;
                return result;
            }

            // All done
            DLog("ApplyPatches() is done");
            return result;
        }

        #endregion


        #region Helpers

        private static string CurrentDLogText = null;
        private static string CurrentDLogPath = null;
        private static void DLogPrepare()
        {
            DateTime now = DateTime.Now;
            CurrentDLogText = "== TTPlugins HPluginApplication Log =="
                + "\nTTPlugins ver: " + Assembly.GetExecutingAssembly().GetName().Version.ToString()
                + "\nCurrent time: " + now.ToString("yyyy-MM-dd-HH-mm-ss-ffff")
                + "\n";

            CurrentDLogPath = "TTPluginsApplicationLog.txt";
        }
        [DebuggerStepThrough]
        private static void DLog(string[] messageLines, int indent = 0)
        {
            foreach (string line in messageLines)
                DLog(line, indent, true);
        }
        [DebuggerStepThrough]
        private static void DLog(string message, bool newline = true)
        {
            DLog(message, 0, newline);
        }
        [DebuggerStepThrough]
        private static void DLog(string message, int indent, bool newline = true)
        {
            DateTime now = DateTime.Now;
            if (newline)
            {
                string fileLine = "";
                for (int i = 0; i < indent; i++)
                    fileLine += "    ";
                fileLine += message;

                Debug.Write("\n[TTPlugins] (HPluginApplicator) " + message);
                if (CurrentDLogText != null)
                    CurrentDLogText += "\n" + now.ToString("[HH-mm-ss-ffff] ") + fileLine;
            }
            else
            {
                Debug.Write(message);
                if (CurrentDLogText != null)
                    CurrentDLogText += message;
            }
        }
        private static void DLogWriteToDisk()
        {
            if (CurrentDLogText == null || CurrentDLogPath == null)
                return;

            try
            {
                Debug.Write("\n[TTPlugins] (HPluginApplicator) Writing DLog to file: " + CurrentDLogPath + "...");
                File.WriteAllText(CurrentDLogPath, CurrentDLogText + "\n");
                Debug.WriteLine("success");
            }
            catch (Exception e)
            {
                Debug.WriteLine("error, details: " + e);
            }
        }

        /// <summary>
        /// Finds and returns a MethodInfo for a method in the executing Terraria assembly.
        /// </summary>
        /// <param name="typeFullName">The full name of the type to look in for the provided method.</param>
        /// <param name="methodName">The name of the method to look for.</param>
        /// <param name="parameterCount">The number of parameters that the target method has.</param>
        /// <param name="firstParamType">The type of the first parameter that the target method has.</param>
        /// <returns>The found MethodInfo, or null if nothing was found.</returns>
        private static MethodInfo GetTerrariaMethod(string typeFullName, string methodName, int parameterCount = -1, Type firstParamType = null)
        {
            if (TerrariaAssembly == null)
                return null;

            Type foundType = TerrariaAssembly.GetTypes().Where(t => t.FullName == typeFullName).FirstOrDefault();
            if (foundType == null)
                return null;

            List<MethodInfo> foundMethods = foundType.GetRuntimeMethods().Where(
                m => m.Name == methodName
                && (parameterCount == -1 || m.GetParameters().Length == parameterCount)
                && (firstParamType == null || (m.GetParameters().Length > 0 && m.GetParameters()[0].ParameterType == firstParamType))
            ).ToList();
            if (foundMethods.Count == 0)
                return null;
            else
                return foundMethods[0];
        }

        /// <summary>
        /// Looks for the proprietary ReLogic assembly in the current app domain.
        /// The ReLogic assembly is lazy loaded using an AssemblyResolve handler inside Terraria.
        /// </summary>
        internal static void FindLoadedReLogicAssembly()
        {
            if (ReLogicAssembly == null)
                ReLogicAssembly = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name == "ReLogic").FirstOrDefault();
        }

        /// <summary>
        /// Looks for all loaded XNA assemblies in the current app domain.
        /// </summary>
        internal static void FindLoadedXNAAssemblies()
        {
            XNAAssemblies.Clear();
            XNAAssemblies.AddRange(
                AppDomain.CurrentDomain.GetAssemblies().Where(x =>
                    x.FullName == "Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553" ||
                    x.FullName == "Microsoft.Xna.Framework.Game, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553" ||
                    x.FullName == "Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553" ||
                    x.FullName == "Microsoft.Xna.Framework.Xact, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553" ||
                    x.FullName == "Microsoft.Xna.Framework.Input.Touch, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553" ||
                    x.FullName == "Microsoft.Xna.Framework.Net, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553" ||
                    x.FullName == "Microsoft.Xna.Framework.GamerServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553" ||
                    x.FullName == "Microsoft.Xna.Framework.Storage, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553" ||
                    x.FullName == "Microsoft.Xna.Framework.Video, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"
                )
            );
        }

        /// <summary>
        /// Gets the current plugin Security Level.
        /// HPlugins can call this to learn what the current Security Level is and adjust themselves accordingly.
        /// </summary>
        /// <returns>The current plugin Security Level.</returns>
        public static int GetCurrentPluginSecurityLevel()
        {
            return SecurityLevel;
        }

        #endregion


        #region Plugin Persistent Savedata Handling

        /// <summary>
        /// Creates a new, blank XDocument that contains the necessary XML structure for a plugin's configuration.xml file.
        /// </summary>
        /// <returns>The completed XDocument.</returns>
        private static XDocument CreateBlankPluginConfigDoc()
        {
            XDocument pluginConfigurationDoc = new XDocument();

            XElement baseElement = new XElement("PluginConfig");
            baseElement.SetAttributeValue("RelPath", "Unknown");
            pluginConfigurationDoc.Add(baseElement);

            XElement savedataElement = new XElement("Savedata");
            savedataElement.Add(new XComment("This <Savedata> element contains the persistent savedata for this plugin."));
            savedataElement.Add(new XComment("If you are manually editing this savedata, do NOT modify the top-level <Savedata> element!"));
            baseElement.Add(savedataElement);

            return pluginConfigurationDoc;
        }
        
        /// <summary>
        /// Gets the file path where the specified HPlugin's configuration.xml resides.
        /// </summary>
        /// <returns>The path to configuration.xml</returns>
        private static string GetConfigurationXMLFilePathForPlugin(HSupervisedPlugin supervisedPlugin, HPluginApplicatorConfiguration applicatorConfiguration)
        {
            if (applicatorConfiguration == null)
                return null;
            
            string tempFolder = Path.Combine(applicatorConfiguration.PluginTemporaryFilesRootDirectory, supervisedPlugin.SourceFileRelativePath);
            Directory.CreateDirectory(tempFolder); // Ensure directory exists
            string configurationXMLFilePath = Path.Combine(tempFolder, applicatorConfiguration.PluginRuntimeConfigFileName);
            return configurationXMLFilePath;
        }

        /// <summary>
        /// Synchronously writes all HPlugins' Configuration to its temporary on-disk copy. Typically called by a Harmony patch that hooks in to Terraria.Main.SaveSettings().
        /// </summary>
        public static void WriteAllPluginConfigToDisk()
        {
            try
            {
                foreach (var supervisedPlugin in HPluginToSupervised.Values)
                {
                    try
                    {
                        if (supervisedPlugin.Plugin.HasPersistentSavedata)
                        {
                            string configFilePath = GetConfigurationXMLFilePathForPlugin(supervisedPlugin, LastConfiguation);
                            if (configFilePath != null)
                            {
                                XElement baseElement = supervisedPlugin.LatestConfigurationXML.Element("PluginConfig");
                                if (baseElement != null)
                                {
                                    // Remove old savedata
                                    XElement oldSavedata = baseElement.Element("Savedata");
                                    if (oldSavedata != null)
                                        oldSavedata.Remove();

                                    // Add new savedata and write to disk
                                    baseElement.Add(supervisedPlugin.Plugin.Configuration.Savedata);
                                    supervisedPlugin.LatestConfigurationXML.Save(configFilePath);
                                }
                            }
                        }
                    }
                    catch (Exception e) { }
                }
            }
            catch (Exception e) { }
        }

        /// <summary>
        /// Synchronously deletes all plugin configuration temporary disk copies.
        /// </summary>
        public static void DeleteAllPluginConfigDiskCopies()
        {
            try
            {
                foreach (var supervisedPlugin in HPluginToSupervised.Values)
                {
                    try
                    {
                        string configFilePath = GetConfigurationXMLFilePathForPlugin(supervisedPlugin, LastConfiguation);
                        if (configFilePath != null)
                        {
                            if (File.Exists(configFilePath))
                            {
                                File.Delete(configFilePath);
                            }
                        }
                    }
                    catch (Exception e) { }
                }
            }
            catch (Exception e) { }
        }

        #endregion
    }
}
