using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.tiberiumfusion.ttplugins.HarmonyPlugins;

namespace TTPluginsExamples.Simple.PrecompiledPluginDemo
{
    /* This example plugin will increase player defense by 200.
     * The Visual Studio project structure around this class demonstrates how to precompile a plugin.
     * 
     * Don't forget:
     * - Add a project reference for TTPlugins.dll
     * - If your plugin code statically references any Terraria types, you must add a project reference for Terraria.exe
     *   - AND you must recompile your plugin every time Terraria updates!
     *   
     * This example is tiny to help understanding, but there is often very little reason to precompile small plugins.
     * Typically, you should only precompile plugins that require images, sounds, and other assets that must be embedded as assembly resources.
     */

    public class SimpleDefenseBoostPlugin : HPlugin
    {
        public override void Initialize()
		{
			// Establish this plugin's internal Identity within the TTPlugins environment. Every plugin should have a unique internal Identity.
			Identity.PluginName = "SimpleDefenseBoostPlugin";
			Identity.PluginDescription = "Example plugin. Increases player defense by 200.";
			Identity.PluginAuthor = "TiberiumFusion";
			Identity.PluginVersion = new Version("1.0.0.0");

            HasPersistentSavedata = false; // This plugin doesn't use persistent savedata.
		}
        
        public override void ConfigurationLoaded(bool successfulConfigLoadFromDisk)
		{
            // This plugin does not use persistent savedata, so we will ignore our Configuration property.
        }
        
        public override void PrePatch()
		{
            // Define our single patch operation.
            CreateHPatchOperation("Terraria.Player", "ResetEffects", "PostfixPatch", HPatchLocation.Postfix);
                // We will patch Terraria.Player.ResetEffects into calling our custom PostfixPatch method after the original method finishes.
		}


        // Our stub method to patch into Terraria
        public static void PostfixPatch(Terraria.Player __instance)
        {
            // The specially named __instance parameter is automatically filled in by Harmony to reference the Player instance that is calling this method.

            __instance.statDefense += 200; // Add 200 defense to the Terraria.Player
        }
    }
}