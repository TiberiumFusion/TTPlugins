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
using Microsoft.Xna.Framework.Input;

namespace TTPluginsExamples.Simple
{
    /* This example plugin provides the player with a variable speed boost that can be changed ingame.
     * Additionally, the current speed boost intensity is saved between Terraria launches using persistent savedata.
     * 
     * This is the plugin code from the "Creating a Basic Plugin (Part 2: User Input and Persistent Savedata)" tutorial video. (with many added comments)
     * 
     * Key demonstrations:
     * - How to detect when the local player presses keys
     * - The basics of saving and loading persistent xml savedata using Configuration.Savedata
     */ 
     
    public class PersistentSavedataDemo : HPlugin
    {
    	private static PersistentSavedataDemo Singleton; // Static reference to the HPlugin instance so we can access Configuration.Savedata in static patch methods
    	private static float SpeedBoostAmount = 0f; // Current intensity of the speed boost effect

        public override void Initialize()
        {
            // Establish this plugin's internal Identity within the TTPlugins environment. Every plugin should have a unique internal Identity.
            Identity.PluginName = "PersistentSavedataDemo";
            Identity.PluginDescription = "Example plugin. Gives the player a speed boost that can be customized with ingame hotkeys.";
            Identity.PluginAuthor = "TiberiumFusion";
            Identity.PluginVersion = new Version("1.0.0.0");

            HasPersistentSavedata = true; // We are using persistent savedata to store and load the chosen speed boost intensity between Terraria launches

            Singleton = this; // Assign the singleton so we can access Configuration.Savedata in our static ChangeSpeedBoost() patch method
        }
        
        public override void ConfigurationLoaded(bool successfulConfigLoadFromDisk)
        {
            // This method will be called after the plugin system loads the persistent savedata for this plugin.
            // We will check for an element that will store our speed boost intensity and load its value, if available.

            XElement elementSpeedBoostAmount = Configuration.Savedata.Element("SpeedBoostAmount"); // Look for an element called SpeedBoostAmount
            if (elementSpeedBoostAmount != null) // If it exists...
            	float.TryParse(elementSpeedBoostAmount.Value, out SpeedBoostAmount); // ...try to parse its value
        }
        
        public override void PrePatch()
        {
            // Define our single patch operation.
            CreateHPatchOperation("Terraria.Player", "UpdateEquips", "PatchSpeedBoost", HPatchLocation.Prefix);
                // We will patch Terraria.Player.UpdateEquips into calling our custom PatchSpeedBoost method before the original method executes.
        }


        // Helper method that:
        // 1. Changes the speed boost amount
        // 2. Ensures the speed boost doesn't become negative
        // 3. Updates the persistent savedata
        private static void ChangeSpeedBoost(float amount)
    	{
        	SpeedBoostAmount += amount; // Change the speed boost intensity
        	
        	if (SpeedBoostAmount < 0f) // Ensure it doesn't go negative
        		SpeedBoostAmount = 0f;
        	
            // Check for the SpeedBoostAmount element in our persistent savedata
        	XElement elementSpeedBoostAmount = Singleton.Configuration.Savedata.Element("SpeedBoostAmount");
        	if (elementSpeedBoostAmount == null) // If it doesn't exist, we must create it
    		{
        		elementSpeedBoostAmount = new XElement("SpeedBoostAmount");
        		Singleton.Configuration.Savedata.Add(elementSpeedBoostAmount);
    		}
        	
            // Assign the current speed boost intensity to be the value of our SpeedBoostAmount element
        	elementSpeedBoostAmount.Value = SpeedBoostAmount.ToString();
    	}

        // The actual patch method which will be patched into Terraria
        public static void PatchSpeedBoost(Terraria.Player __instance)
    	{
            // The specially named __instance parameter is automatically filled in by Harmony to reference the Player instance that is calling this method.

            // First, we use the HHelpers.InputReading.IsKeyPressed() method to check if the local player has pressed one of our hotkeys.
            // If the comma is pressed (aka <), we will decrease the speed boost.
            // If the period is pressed (aka >), we will increase the speed boost.
        	if (HHelpers.InputReading.IsKeyPressed(Keys.OemComma))
        		ChangeSpeedBoost(-1f); // Change speed boost using helper method
        	if (HHelpers.InputReading.IsKeyPressed(Keys.OemPeriod))
        		ChangeSpeedBoost(1f); // Change speed boost using helper method
        	
            // Then, we apply the speed boost to the Terraria.Player
        	__instance.moveSpeed += SpeedBoostAmount; // The moveSpeed field directly controls how fast the player runs
    	}
    }
}