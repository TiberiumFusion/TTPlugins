using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        /// The Harmony instance which was created during patch application
        /// </summary>
        internal static HarmonyLib.Harmony HarmonyInstance { get; private set; }

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
        private static HPluginApplicatorConfiguration LastConfiguation;

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
        /// A list of the bytes of all plugin assemblies that were extracted in memory.
        /// </summary>
        internal static List<byte[]> ExtractedPluginAssemblyBytes { get; private set; } = new List<byte[]>();

        /// <summary>
        /// A list of the paths of all plugin assemblies that were extracted to the disk.
        /// </summary>
        internal static List<string> ExtractedPluginAssemblyPaths { get; private set; } = new List<string>();

        #endregion


        #region Assembly Resolver

        // Fixes bizarre problems with dynamically loaded assemblies missing all their types and fields because the CLR couldnt "find" the correct assembly
        internal static void SetupDomainAssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }
        // See: https://stackoverflow.com/questions/2658275/assembly-gettypes-reflectiontypeloadexception
        // The bizarre way this works:
        // 1. The CLR doesnt know where an Assembly is, so it gives us its full name.
        // 2. We find the loaded assembly that has that full name and give it to the CLR.
        // 3. The CLR is baffled by how well we solved this incredibly complex problem and things work again.
        internal static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var domain = (AppDomain)sender;

            foreach (var assembly in domain.GetAssemblies())
                if (assembly.FullName == args.Name)
                    return assembly;

            return null;
        }

        #endregion


        #region Patch Application

        /// <summary>
        /// Method which TTApplicator will patch Terraria into calling. This method will create a HPluginApplicatorConfiguration and call ApplyPatches.
        /// </summary>
        public static void EntryPointForTerraria()
        {
            // At this point, the MSIL patch has loaded TTPlugins from the embedded resources in order to call this static method.
            // We now need to load the rest of what TTApplicator has embedded before calling ApplyPatches

            //System.Diagnostics.Debugger.Launch();

            try
            {
                TerrariaAssembly = Assembly.GetCallingAssembly();

                // Extract Harmony
                EmbeddedHarmonyAssembly = null;
                using (var resStream = TerrariaAssembly.GetManifestResourceStream("TTPlugins_0Harmony.dll"))
                {
                    using (var memStream = new MemoryStream())
                    {
                        resStream.CopyTo(memStream);
                        byte[] asmBytes = memStream.ToArray();
                        EmbeddedHarmonyAssembly = Assembly.Load(asmBytes);
                    }
                }

                // Extract plugins assmblies config doc
                XDocument pluginAssembliesConfig = null;
                using (var resStream = TerrariaAssembly.GetManifestResourceStream("TTPlugins_EmbeddedPluginsConfig.xml"))
                {
                    pluginAssembliesConfig = XDocument.Load(resStream);
                }

                // Load the gac assemblies specified in the config doc into the appdomain
                XElement configDocBase = pluginAssembliesConfig.Element("Base");
                XElement GACAssembliesToLoad = configDocBase.Element("GACAssembliesToLoad");
                foreach (XElement asmNameElement in GACAssembliesToLoad.Nodes())
                {
                    string fullAsmName = asmNameElement.Value;
                    try { Assembly.Load(fullAsmName); }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Failed to load common assembly: " + fullAsmName);
                    }
                }

                // Get plugin mode config
                bool pluginDebugMode = bool.Parse(configDocBase.Element("PluginDebugMode")?.Value ?? "false");

                // Extract and load the plugin assemblies specified in the config doc into the appdomain
                LoadedPluginAssemblies.Clear();
                ExtractedPluginAssemblyBytes.Clear();
                ExtractedPluginAssemblyPaths.Clear();
                string pluginExtractDir = null;
                if (pluginDebugMode)
                {
                    string terrariaFolderPath = Path.GetDirectoryName(TerrariaAssembly.Location);
                    pluginExtractDir = Path.Combine(terrariaFolderPath, ".TTPlugins_RuntimeExtract");
                    if (Directory.Exists(pluginExtractDir))
                    {
                        try
                        {
                            DirectoryInfo dirInfo = new DirectoryInfo(pluginExtractDir);
                            dirInfo.Delete(true);
                        }
                        catch (Exception e) { } // Hopefully the dir was locked and not the files inside
                    }
                    Directory.CreateDirectory(pluginExtractDir);
                }

                XElement PluginAsmResourceNames = configDocBase.Element("PluginAsmResourceNames");
                foreach (XElement resNameElement in PluginAsmResourceNames.Nodes())
                {
                    // Find DLL and PDB resource names
                    string asmResourceName = resNameElement.Value;
                    string pdbResourceName = null;
                    if (pluginDebugMode)
                    {
                        XAttribute pdbNameAttr = resNameElement.Attribute("PDBName");
                        if (pdbNameAttr != null)
                            pdbResourceName = pdbNameAttr.Value;
                    }

                    // Extract PDB if in debug mode
                    try
                    {
                        if (pluginDebugMode && pdbResourceName != null)
                        {
                            using (var resStream = TerrariaAssembly.GetManifestResourceStream(pdbResourceName))
                            {
                                string extractPDBPath = Path.Combine(pluginExtractDir, pdbResourceName);
                                using (FileStream fileStream = File.Open(extractPDBPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
                                {
                                    resStream.CopyTo(fileStream);
                                }
                            }
                        }
                    }
                    catch (Exception e) { } // Swallow. The pdb may not be available, but the plugin can still be loaded anyways.

                    // Extract the plugin assembly per the debug/release configuration
                    using (var resStream = TerrariaAssembly.GetManifestResourceStream(asmResourceName))
                    {
                        // In debug mode, extract to the disk in order for VS to pick up the PDB when the debugger is attached
                        if (pluginDebugMode)
                        {
                            string extractDLLPath = Path.Combine(pluginExtractDir, asmResourceName);
                            using (FileStream fileStream = File.Open(extractDLLPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
                            {
                                resStream.CopyTo(fileStream);
                            }
                            ExtractedPluginAssemblyPaths.Add(extractDLLPath);
                            Assembly pluginAsm = Assembly.LoadFile(extractDLLPath); // Because this asm will be loaded from the disk, it will be locked by this process and cannot be cleaned up on Terraria shutdown by internal code.
                            LoadedPluginAssemblies.Add(pluginAsm);
                        }
                        // In (normal) release mode, skip the disk and extract in memory
                        else
                        {
                            using (var memStream = new MemoryStream())
                            {
                                resStream.CopyTo(memStream);
                                byte[] asmBytes = memStream.ToArray();
                                ExtractedPluginAssemblyBytes.Add(asmBytes);
                                Assembly pluginAsm = Assembly.Load(asmBytes);
                                LoadedPluginAssemblies.Add(pluginAsm);
                            }
                        }
                    }
                }

                // Get temporary plugin files root dir from the config doc
                string pluginTemporaryFilesRootDir = configDocBase.Element("PluginTemporaryFilesRootDir").Value;

                // Get the hplugin full type name -> plugin relpath mapping from the config doc
                Dictionary<string, string> pluginTypeRelPathMap = new Dictionary<string, string>();
                XElement hpluginTypeNameRelPathMap = configDocBase.Element("HPluginTypeNameRelPathMap");
                foreach (XElement item in hpluginTypeNameRelPathMap.Nodes())
                {
                    string typeFullName = item.Attribute("TypeFullName").Value;
                    string relpath = item.Value;
                    pluginTypeRelPathMap[typeFullName] = relpath;
                }

                // Create the plugin applicator config and hit the go button
                HPluginApplicatorConfiguration applicatorConfig = new HPluginApplicatorConfiguration();
                applicatorConfig.ExecutingTerrariaAssembly = TerrariaAssembly;
                applicatorConfig.PluginAssemblies = LoadedPluginAssemblies;
                applicatorConfig.PluginTemporaryFilesRootDirectory = pluginTemporaryFilesRootDir;
                applicatorConfig.PluginTypesRelativePaths = pluginTypeRelPathMap;
                ApplyPatches(applicatorConfig);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error while applying TTPlugins in EntryPointFromTerraria, details: " + e);
            }
        }

        /// <summary>
        /// Applies all HPlugins from the provided compiled assemblies.
        /// </summary>
        public static HPluginApplicatorResult ApplyPatches(HPluginApplicatorConfiguration configuration)
        {
            LastConfiguation = configuration;
            HPluginApplicatorResult result = new HPluginApplicatorResult();


            // Fix weird missing assembly issues
            SetupDomainAssemblyResolver();
            

            // Create a harmony instance
            try
            {
                HarmonyInstance = new Harmony("com.tiberiumfusion.ttplugins.HarmonyPlugins.HPluginApplicator");
            }
            catch (Exception e)
            {
                result.ConfigureAsFailure(HPluginApplicatorResultCodes.CreateHarmonyInstanceFailure, e);
                return result;
            }


            // Apply framework patches before applying usercode patches
            Type fwPatches = typeof(HFrameworkPatches);
            HarmonyInstance.Patch(GetTerrariaMethod("Terraria.Main", "SaveSettings"), new HarmonyMethod(fwPatches.GetMethod("FW_SaveAllPluginConfigs")));


            // Find all HPlugins in the compiled assemblies and apply them
            AppliedHPlugins.Clear();
            try
            {
                // Map of each relpath to the plugin config that was loaded for it. Allows for reusing of already loaded configs for multiple types that share the same relpath (i.e. several classes in the same plugin assembly).
                Dictionary<string, XDocument> relpathToLoadedPluginConfig = new Dictionary<string, XDocument>();

                foreach (Assembly pluginAsm in configuration.PluginAssemblies)
                {
                    // Find all HPlugins in the usercode assembly
                    const string unknownRelPathString = "Unknown source file path.";
                    List<Type> foundPluginTypes = pluginAsm.GetTypes().Where(t => t.IsClass && t.IsSubclassOf(typeof(HPlugin))).ToList();
                    foreach (Type pluginType in foundPluginTypes)
                    {
                        // Create an instance of the plugin
                        HPlugin pluginInstance = Activator.CreateInstance(pluginType) as HPlugin;

                        // Find out if it had a valid relative path at compile time that we can use
                        string sourceFileRelPath = null;
                        configuration.PluginTypesRelativePaths.TryGetValue(pluginType.FullName, out sourceFileRelPath);
                        if (sourceFileRelPath == null)
                            sourceFileRelPath = unknownRelPathString;
                        
                        // Wrap it up
                        HSupervisedPlugin supervisedPlugin = new HSupervisedPlugin(pluginInstance, sourceFileRelPath);
                        HPluginToSupervised[pluginInstance] = supervisedPlugin; // And map it

                        // Initialize() tells the plugin to set its informational fields, which need to be established first and foremost
                        try
                        {
                            supervisedPlugin.Plugin.Initialize();
                        }
                        catch (Exception e)
                        {
                            result.HPluginsThatFailedInitialize[supervisedPlugin.SourceFileRelativePath] = e;
                            continue; // Skip over this plugin
                        }

                        // Start plugin with a default configuration doc
                        XDocument pluginConfigurationDoc = CreateBlankPluginSavedataDoc();

                        // If the plugin has persistent data and a valid relpath, load that now
                        bool successfulConfigLoad = false;
                        if (supervisedPlugin.Plugin.HasPersistentData)
                        {
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
                                        if (File.Exists(configurationXMLFilePath)) // Load config if it exists
                                            pluginConfigurationDoc = XDocument.Load(configurationXMLFilePath);

                                        successfulConfigLoad = true;
                                    }
                                    catch (Exception e)
                                    {
                                        result.HPluginsWithFailedConfigurationLoads[supervisedPlugin.SourceFileRelativePath] = e;
                                    }
                                }
                            }
                        }

                        // Hold onto the configuration xml doc
                        supervisedPlugin.LatestConfigurationXML = pluginConfigurationDoc;

                        // Setup the Configuration object from the xml doc
                        HPluginConfiguration pluginConfiguration = new HPluginConfiguration(); // Create a new configuration object for the xml config
                        try
                        {
                            XElement baseElement = pluginConfigurationDoc.Element("PluginConfig");
                            if (baseElement != null)
                            {
                                XElement savedataElement = baseElement.Element("Savedata");
                                if (savedataElement != null)
                                    pluginConfiguration.Savedata = savedataElement;
                            }
                        }
                        catch (Exception e)
                        {
                            result.HPluginsWithFailedConfigurationLoads[supervisedPlugin.SourceFileRelativePath] = e;
                            // In this case, the HPlugin will have a default HPluginConfiguration object with an empty savedata XElement
                        }

                        // Give the configuration to the HPlugin
                        supervisedPlugin.Plugin.ReceiveConfiguration(pluginConfiguration);

                        // ConfigurationLoaded() lets the plugin know it can safely read Configuration now
                        try
                        {
                            supervisedPlugin.Plugin.ConfigurationLoaded(successfulConfigLoad);
                        }
                        catch (Exception e)
                        {
                            result.HPluginsThatThrewExceptions[supervisedPlugin.SourceFileRelativePath] = e;
                            continue; // Skip over this plugin
                        }

                        // Now that the plugin is initialized and configured, it is finally time to patch it in with Harmony
                        // But first, we give the plugin one last chance to define its patch operations
                        try
                        {
                            supervisedPlugin.Plugin.PrePatch();
                        }
                        catch (Exception e)
                        {
                            result.HPluginsThatThrewExceptions[supervisedPlugin.SourceFileRelativePath] = e;
                            continue; // Skip over this plugin
                        }

                        // Do all the patch operations defined by the plugin
                        foreach (HPatchOperation patchOp in supervisedPlugin.Plugin.PatchOperations)
                        {
                            // First validate the patchOp

                            // Ensure both the target and patch stub MethodInfos exist
                            if (patchOp.TargetMethod == null || patchOp.StubMethod == null)
                            {
                                result.HPluginsWithNullMethodInfos.Add(supervisedPlugin.SourceFileRelativePath);
                                continue; // Skip over this plugin
                            }

                            // Ensure the target method is not in a protected namespace
                            bool brokeRules = false;
                            foreach (string protectedNamespace in ProtectedNamespaces)
                            {
                                if (patchOp.TargetMethod.DeclaringType.Namespace.IndexOf(protectedNamespace) == 0)
                                {
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
                                if (patchOp.PatchLocation == HPatchLocation.Prefix)
                                    HarmonyInstance.Patch(patchOp.TargetMethod, new HarmonyMethod(patchOp.StubMethod));
                                else if (patchOp.PatchLocation == HPatchLocation.Postfix)
                                    HarmonyInstance.Patch(patchOp.TargetMethod, null, new HarmonyMethod(patchOp.StubMethod));

                                AppliedHPlugins.Add(supervisedPlugin);
                            }
                            catch (Exception e)
                            {
                                result.HPluginsThatDidntPatch[supervisedPlugin.SourceFileRelativePath] = e;
                                // Carry on to the next plugin
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                result.ConfigureAsFailure(HPluginApplicatorResultCodes.GenericHPluginApplicationFailure, e);
                return result;
            }

            // All done
            return result;
        }

        #endregion


        #region Helpers

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

        #endregion


        #region Plugin Persistent Savedata Handling

        /// <summary>
        /// Creates a new, blank XDocument that contains the necessary XML structure for a plugin's configuration.xml file.
        /// </summary>
        /// <returns>The completed XDocument.</returns>
        private static XDocument CreateBlankPluginSavedataDoc()
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
