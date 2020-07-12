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

namespace TTPluginsExamples.Simple
{
    // This example plugin will increase player speed by 10.

    public class SinglePatchDemo : HPlugin
    {
        public override void Initialize()
        {
            // Establish this plugin's internal Identity within the TTPlugins environment. Every plugin should have a unique internal Identity.
            Identity.PluginName = "SinglePatchDemo";
            Identity.PluginDescription = "Example plugin. Increases player speed by 10.";
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
            CreateHPatchOperation("Terraria.Player", "UpdateEquips", "PrefixPatch", HPatchLocation.Prefix);
                // We will patch Terraria.Player.UpdateEquips into calling our custom PrefixPatch method before the original method executes.
        }


        // Our stub method to patch into Terraria
        public static void PrefixPatch(Terraria.Player __instance)
        {
            // The specially named __instance parameter is automatically filled in by Harmony to reference the Player instance that is calling this method.

            __instance.moveSpeed += 10.0f; // Add 10 speed to the Terraria.Player
        }
    }
}