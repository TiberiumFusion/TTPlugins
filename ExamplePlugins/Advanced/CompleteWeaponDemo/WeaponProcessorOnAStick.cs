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
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace TTPluginsExamples.Advanced.CompleteWeaponDemo
{
    /* This example plugin adds a new weapon & accompanying projectile to Terraria.
     * 
     * Key demonstrations:
     * - How to embed assets in the compiled plugin assembly, then load & use them in Terraria
     * - The minimum patches necessary to create a new weapon
     * - The minimum patches necessary to create a new projectile
     * - Use of helpers in HHelpers to improve plugin security compliance and avoid violating Security Level 3 and 4
     */
    public class WeaponProcessorOnAStick : HPlugin
    {
        #region Vars

        ///// Generic
        private static bool PluginLoadedSuccessfully = true; // Whether or not we have successfully loaded and initialized everything our plugin needs
            // If something fails to load, we will set this to false to indicate to the rest of our code that our plugin cannot run properly
            

        ///// Specific to the weapon's Item
        private static int WeaponItemID = 10010; // The ID that will be associated with the weapon's Item

        private static string WeaponItemNameText = "Processor on a Stick"; // The plain text of our weapon's Item's name, which will go into the WeaponItemName LocalizedText object
        private static string WeaponItemTooltipText = "A small processor glued onto a stick. Somehow, it's full of magic."; // The tooltip for our weapon's Item
        private static Terraria.Localization.LocalizedText WeaponItemName; // The LocalizedText for the name of our weapon's Item
        private static Terraria.UI.ItemTooltip WeaponItemTooltip; // The ItemTooltip for our weapon's Item

        private static byte[] ItemGraphicBytes; // The byte[] of the embedded weapon graphic resource
        private static Texture2D ItemGraphic; // The weapon graphic
        private static ReLogic.Content.Asset<Texture2D> ItemGraphicAsset; // A relogic asset that contains our weapon's graphic
        

        ///// Specific to the weapon's custom projectile
        private static int ProjectileID = 3001;

        private static byte[] ProjectileGraphicBytes; // The byte[] of the embedded projectile graphic resource
        private static Texture2D ProjectileGraphic; // The projectile graphic
        private static ReLogic.Content.Asset<Texture2D> ProjectileGraphicAsset; // A relogic asset that contains our custom projectile's graphic

        #endregion


        #region Plugin Self-Management

        public override void Initialize()
        {
            // Establish this plugin's internal Identity within the TTPlugins environment. Every plugin should have a unique internal Identity.
            Identity.PluginName = "WeaponProcessorOnAStick";
            Identity.PluginDescription = "Example plugin. Adds a new weapon called \"Processor on a Stick\" to the game.";
            Identity.PluginAuthor = "TiberiumFusion";
            Identity.PluginVersion = new Version("1.0.0.0");

            HasPersistentSavedata = false; // This plugin doesn't use persistent savedata.
        }

        public override void ConfigurationLoaded(bool successfulConfigLoadFromDisk)
        {
            // This plugin does not use persistent savedata, so we will ignore the Configuration property.
            
            // Load the bytes of the embedded resources in our assembly.
            ItemGraphicBytes = GetPluginAssemblyResourceBytes("TTPluginsExamples.Advanced.CompleteWeaponDemo.Assets.ItemGraphic.png");
            ProjectileGraphicBytes = GetPluginAssemblyResourceBytes("TTPluginsExamples.Advanced.CompleteWeaponDemo.Assets.WeaponProjectileFrames.png");

            // We have to wait until Terraria.Main initializes before we can create any Texture2D objects (because we need a GraphicsDevice).
        }

        public override void PrePatch()
        {
            // This single HPlugin contains all the patch code and assets which constitute both the new weapon and its custom projectile.
            // You could split the weapon and projectile into their own, seperate HPlugins, if that fits your project's organization better.


            ///// Patch checklist for making a basic weapon:
            
            // 1. Patch Terraria.Main.LoadContent to load our weapon's assets
            // 2. Expand a bunch of design code arrays to be as large as our weapon's ID
            // 3. Create a ReLogic asset for our item graphic
            CreateHPatchOperation("Terraria.Main", "LoadContent", "LoadContent_LoadWeaponAssets", HPatchLocation.Postfix);
                // We'll do 1, 2, and 3 in this one patch stub method, since the host method (LoadContent) only runs once
            
            // 4. Patch Terraria.Item.SetDefaults to give our weapon's Item some defaults
            CreateHPatchOperation("Terraria.Item", "SetDefaults", 3, "SetDefaults_SetItemDefaults", HPatchLocation.Prefix);
                // SetDefaults is overloaded, so we need to make sure we patch the right one (so we use the CreateHPatchOperation() overload that lets us specify the parameter count of the target method).
            
            // 5. Patch Terraria.Item.RebuildTooltip to give our item some tooltip text.
            CreateHPatchOperation("Terraria.Item", "RebuildTooltip", "RebuildTooltip_SetItemTooltip", HPatchLocation.Prefix);

            // 6. Patch Terraria.Lang.GetTooltip to retrieve our item's tooltip object.
            CreateHPatchOperation("Terraria.Lang", "GetTooltip", "GetTooltip_GetItemTooltip", HPatchLocation.Prefix);

            // 7. Patch Terraria.Lang.GetItemName to retrieve our item's name object.
            CreateHPatchOperation("Terraria.Lang", "GetItemName", "GetItemName_GetItemName", HPatchLocation.Prefix);
            // This is really just a backup patch, since we set the Item._nameOverride to the item's name text in our LoadContent_LoadPluginAssets() patch method


            ///// Patch checklist for making a basic projectile:

            // 1. Patch Terraria.Main.LoadContent to load our projectile's assets
            // 2. Expand a bunch of design code arrays to be as large as our projectile's ID
            // 3. Create a ReLogic asset for our projectile graphic
            CreateHPatchOperation("Terraria.Main", "LoadContent", "LoadContent_LoadProjectileAssets", HPatchLocation.Postfix);
                // We'll do 1, 2, and 3 in this one patch stub method, since the host method (LoadContent) only runs once

            // 4. Patch Terraria.Player to expand some instance arrays to be as large as our projectile's ID
            CreateHPatchOperation("Terraria.Player", ".ctor", "PlayerCtor_ExpandProjectileArrays", HPatchLocation.Postfix);
                // The instance arrays we need to modify are created in the constructor, so we will recreate them to be large in a postfix patch on the constructor

            // 5. Patch Terraria.Projectile.SetDefaults to give our custom projectile some defaults
            CreateHPatchOperation("Terraria.Projectile", "SetDefaults", "SetDefaults_SetProjectileDefaults", HPatchLocation.Postfix);

            // 6. Patch Terraria.Projectile.AI to give our projectile custom update code
            CreateHPatchOperation("Terraria.Projectile", "AI", "AI_ProjectileUpdate", HPatchLocation.Prefix);
                // We will make our projectile cycle through its animations frames with this patch

            // 7. Patch Terraria.Main.DrawProj so we can add some drawing code for our custom projection
            CreateHPatchOperation("Terraria.Main", "DrawProj", "DrawProj_DrawProjectile", HPatchLocation.Prefix);


            // (Not required) For an extra effect, we'll make our projectile fade-out instead of instantly vanishing when it hits something.
            // To do this, we'll patch Kill() to add a brief fade-out period when the projectile would otherwise be instantly removed.
            CreateHPatchOperation("Terraria.Projectile", "Kill", "Kill_KillProjectile", HPatchLocation.Prefix);
                // This is only half of the fade-out effect. The other half is in the DrawProj patch, which recognizes the fade-out period and makes the projectile's texture more transparent the closer it gets to being removed
        }
        
        #endregion


        #region Weapon Patches

        public static void LoadContent_LoadWeaponAssets(Terraria.Main __instance)
        {
            // The specially named __instance parameter is automatically filled in by Harmony to reference the Main instance that is calling this method.
            
            // We'll do all our setup in a try-catch so we don't crash Terraria if something goes wrong.
            try
            {
                // Load our weapon's texture from the embedded resource bytes we extracted in ConfigurationLoaded()
                ItemGraphic = HHelpers.AssetHandling.CreateTexture2DFromImageBytes(ItemGraphicBytes, __instance.GraphicsDevice);
                    // Using CreateTexture2DFromImageBytes() is compliant with all security levels and is the recommended way to create Texture2Ds.
                    // You could use your own Streams, but using Streams (or any other type in System.IO) will make your plugin violate Security Level 3.
                
                // We will also modify some necessary arrays here (this method will only be called once, so this is a good time to modify these arrays).
                // There are many arrays which are indexed using the item's type (aka ID). We need to expand all those arrays to reach up to our item's ID.
                // We will fill the placeholder spaces between the last vanilla ID and our new ID with default values.
                Helper_ExpandArray(ref Terraria.Item.cachedItemSpawnsByType, WeaponItemID, -1);
                Helper_ExpandArray(ref Terraria.Item.claw, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.Item.staff, WeaponItemID, false);
                    Terraria.Item.staff[WeaponItemID] = true; // Our item is a staff, so we set this to true for our slot
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.AlsoABuildingItem, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.AnimatesAsSoul, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.BonusMeleeSpeedMultiplier, WeaponItemID, 1f);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.BossBag, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.CanBePlacedOnWeaponRacks, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.CanBeQuickusedOnGamepad, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.CanGetPrefixes, WeaponItemID, false);
                    Terraria.ID.ItemID.Sets.CanGetPrefixes[WeaponItemID] = true; // Our item is a weapon & we want to it be able to get prefixes, so we set this to true for our slot
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.Deprecated, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.DrawUnsafeIndicator, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.DrinkParticleColors, WeaponItemID, new Color[0]);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.ExtractinatorMode, WeaponItemID, -1);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.FoodParticleColors, WeaponItemID, new Color[0]);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.ForceConsumption, WeaponItemID, null);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.GamepadExtraRange, WeaponItemID, 0);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.GamepadSmartQuickReach, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.GamepadWholeScreenUseRange, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.gunProj, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.HasAProjectileThatHasAUsabilityCheck, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.IgnoresEncumberingStone, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.IsAKite, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.IsAMaterial, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.IsAPickup, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.IsChainsaw, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.IsDrill, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.IsFishingCrate, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.IsFishingCrateHardmode, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.IsFood, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.IsLavaBait, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.IsPaintScraper, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.ItemIconPulse, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.ItemNoGravity, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.ItemSpawnDecaySpeed, WeaponItemID, 1);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.ItemsThatAllowRepeatedRightClick, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.ItemsThatCountAsBombsForDemolitionistToSpawn, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.KillsToBanner, WeaponItemID, 50);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.LockOnAimAbove, WeaponItemID, 0);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.LockOnIgnoresCollision, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.NebulaPickup, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.NeverAppearsAsNewInInventory, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.OverflowProtectionTimeOffset, WeaponItemID, 0);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.SingleUseInGamepad, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.SkipsInitialUseSound, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.SummonerWeaponThatScalesWithAttackSpeed, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.TextureCopyLoad, WeaponItemID, -1);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.ToolTipDamageMultiplier, WeaponItemID, 1f);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.Torches, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.TrapSigned, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.UsesCursedByPlanteraTooltip, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.WaterTorches, WeaponItemID, false);
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.Yoyo, WeaponItemID, false);
                
                // We also need to expand Terraria.Main.itemAnimations to reach up to our item's ID and include a Terraria.DataStructures.DrawAnimation for our weapon's Item
                Helper_ExpandArray(ref Terraria.Main.itemAnimations, WeaponItemID, null); // Add null DrawAnimations in the dummy slots (and in our weapon's slot as well, since the sprite has a single frame and no animations)

                // We also need to expand Terraria.ID.ItemID.Sets.TextureCopyLoad to reach up to our item's ID
                Helper_ExpandArray(ref Terraria.ID.ItemID.Sets.TextureCopyLoad, WeaponItemID, 0); // We will have all the empty placeholder spots set to 0 so that they mirror load the texture in index 0 (as opposed to crashing from trying to load an out-of-range texture index)
                
                // Next we need to extend the Terraria.GameContent.TextureAssets.Item array to reach our item's ID in size and include our Item's texture
                Helper_ExpandArray(ref Terraria.GameContent.TextureAssets.Item, WeaponItemID, Terraria.GameContent.TextureAssets.Item[0]); // Use the first texture in the list as a default placeholder
                // Now create a ReLogic.Content.Asset<Texture2D> to hold our item graphic
                Type assetType = typeof(ReLogic.Content.Asset<Texture2D>);
                ItemGraphicAsset = (ReLogic.Content.Asset<Texture2D>)HHelpers.ActivateInstanceUsingFirstConstructor(assetType, new object[] { "Item_WeaponProcessorOnAStick" }); // This type has only one constructor
                    // By using the ActivateInstanceUsingFirstConstructor() helper method, we avoid directly using Reflection and thus don't violate plugin Security Level 4
                HHelpers.SetPropertyValueWithReflection("Value", ItemGraphicAsset, ItemGraphic); // Set the Value (which is of type Texture2D)
                HHelpers.SetPropertyValueWithReflection("State", ItemGraphicAsset, ReLogic.Content.AssetState.Loaded); // Set the loaded state to Loaded
                Terraria.GameContent.TextureAssets.Item[WeaponItemID] = ItemGraphicAsset; // Assign the newly created asset to our item's slot in the array

                // We also need to create a LocalizedText to hold our weapon Item's name
                Type localizedTextType = typeof(Terraria.Localization.LocalizedText);
                WeaponItemName = (Terraria.Localization.LocalizedText)HHelpers.ActivateInstanceUsingFirstConstructor(localizedTextType, new object[] { "", WeaponItemNameText }); // This type has only one constructor
                    // By using the ActivateInstanceUsingFirstConstructor() helper method, we avoid directly using Reflection and thus don't violate plugin Security Level 4
            }
            catch (Exception e)
            {
                // Something went wrong, so we will set PluginLoadedSuccessfully to false and thus disable the rest of our plugin code.
                // If you are debugging a plugin, however, you probably DONT want to do this, or you might miss thrown exceptions.
                PluginLoadedSuccessfully = false;
                return;
            }
        }

        public static bool SetDefaults_SetItemDefaults(Terraria.Item __instance, int Type)
        {
            // The specially named __instance parameter is automatically filled in by Harmony to reference the Item instance that is calling this method.
            // The type parameter is automatically filled in by Harmony to be the same value as the type parameter that was passed to the original method.
            
            if (!PluginLoadedSuccessfully) // If there was a problem loading things for our plugin, we will abort this stub method so we dont crash Terraria.
                return true; // By returning true, we tell Harmony to allow the original method to execute

            if (Type == WeaponItemID) // Only apply our weapon's Item defaults if the item that had SetDefaults called on it actually is our weapon.
            {
                // We need to do the same things that SetDefaults() does, since we will prevent the original SetDefaults() from running after our prefix here is done.
                
                if (Terraria.Main.netMode == 1 || Terraria.Main.netMode == 2)
                    __instance.playerIndexTheItemIsReservedFor = 255;
                else
                    __instance.playerIndexTheItemIsReservedFor = Terraria.Main.myPlayer;

                __instance.ResetStats(Type);

                __instance.damage = 64; // How much damage projectiles from this weapon will do
                __instance.mana = 10; // How much mana is required to use this weapon
                __instance.shoot = 3001; // ID of the Projectile to shoot when used (which is our custom projectile)
                __instance.shootSpeed = 5f; // Speed of the Projectile that this weapon shoots
                __instance.knockBack = 1.5f; // Amount of knockback dealt when this weapon's Projectile hits something
                __instance.value = Terraria.Item.sellPrice(0, 2, 4, 8); // How much this item is worth
                __instance.magic = true; // Whether or not this item is a magic weapon (and is thus affected by magic dmg/crit/etc stat boosters)
                __instance.noMelee = true; // Whether or not this item has a melee hitbox and melee effects when used
                __instance.rare = 9; // Rarity factor, which affects the color of the item's text
                __instance.autoReuse = true; // Allows continuous use while left mouse is held down
                __instance.useTime = 13; // How long it takes to use this item one time (in frames)
                __instance.useAnimation = 13; // How long the item stays on screen for (in frames) when it is used
                __instance.width = 42; // Width of the weapon
                __instance.height = 42; // Height of the weapon
                __instance.useStyle = 5; // How the item is swung/held when used
                __instance.UseSound = Terraria.ID.SoundID.Item158; // Sound to play when the item is used

                __instance.RebuildTooltip(); // Generate the item's tooltip text

                HHelpers.SetFieldValueWithReflection("_nameOverride", __instance, WeaponItemNameText); // Set item's name

                // Also, in case the async texture loading replaced our weapon's texture with something else, let's make sure the right texture is in our item's slot
                if (ItemGraphicAsset != null)
                    Terraria.GameContent.TextureAssets.Item[WeaponItemID] = ItemGraphicAsset; // Reassign the field with the asset we created earlier

                return false; // By returning false, we tell Harmony to skip over the original method
            }

            return true; // Tell Harmony to allow the original method to execute
        }

        public static bool RebuildTooltip_SetItemTooltip(Terraria.Item __instance)
        {
            // The specially named __instance parameter is automatically filled in by Harmony to reference the Item instance that is calling this method.
            
            if (!PluginLoadedSuccessfully) // If there was a problem loading things for our plugin, we will abort this stub method so we dont crash Terraria.
                return true; // Allow the original method to execute

            if (__instance.type == WeaponItemID) // Only apply our weapon's tooltip if the item that had RebuildTooltip called on it actually is our weapon.
            {
                // Terraria.UI.ItemTooltip does not have a public constructor, so we must use Activator to create an ItemTooltip instance
                var tooltip = (Terraria.UI.ItemTooltip)Activator.CreateInstance(typeof(Terraria.UI.ItemTooltip), true);

                // Terraria.Localization.LocalizedText also does not have a public constructor, so we need to use Reflection
                Type localizedTextType = typeof(Terraria.Localization.LocalizedText);
                var localizedTooltipText = (Terraria.Localization.LocalizedText)HHelpers.ActivateInstanceUsingFirstConstructor(localizedTextType, new object[] { "", WeaponItemTooltipText }); // This type has only one constructor
                    // By using the ActivateInstanceUsingFirstConstructor() helper method, we avoid directly using Reflection and thus don't violate plugin Security Level 4
                
                // Also, Terraria.UI.ItemTooltip._text is a private field, so we need to use Reflection to set its value.
                HHelpers.SetFieldValueWithReflection("_text", tooltip, localizedTooltipText);
                // The recommended way to do this is to use HHelpers.SetFieldValueWithReflection(), which is compliant with all security levels.
                // You can, of course, use reflection yourself, but then your plugin will violate plugin Security Level 4.

                __instance.ToolTip = tooltip; // Assign the newly-created tooltip to the Item
                WeaponItemTooltip = tooltip; // And hold onto it for later

                return false; // Don't allow the original method to execute
            }

            return true; // Allow the original method to execute
        }

        public static bool GetTooltip_GetItemTooltip(Terraria.UI.ItemTooltip __result, int itemId)
        {
            // By assigning something to the specially named __result parameter, we are changing the return value of the original method.
            // The itemId parameter is automatically filled in by Harmony to be the same value as the itemId parameter that was passed to the original method.
            
            if (!PluginLoadedSuccessfully) // If there was a problem loading things for our plugin, we will abort this stub method so we dont crash Terraria.
                return true; // Allow the original method to execute

            if (itemId == WeaponItemID) // Only skip the original method and return our weapon's tooltip if the itemId is actually our weapon's ID
            {
                __result = WeaponItemTooltip;

                return false; // Don't allow the original method to execute
            }

            return true; // Allow the original method to execute
        }

        public static bool GetItemName_GetItemName(Terraria.Localization.LocalizedText __result, int id)
        {
            // By assigning something to the specially named __result parameter, we are changing the return value of the original method.
            // The id parameter is automatically filled in by Harmony to be the same value as the id parameter that was passed to the original method.

            if (!PluginLoadedSuccessfully) // If there was a problem loading things for our plugin, we will abort this stub method so we dont crash Terraria.
                return true; // Allow the original method to execute

            if (id == WeaponItemID) // Only skip the original method and return our weapon's name if the id is actually our weapon's ID
            {
                __result = WeaponItemName;

                return false; // Don't allow the original method to execute
            }

            return true; // Allow the original method to execute
        }

        #endregion

        #region Projectile Patches

        public static void LoadContent_LoadProjectileAssets(Terraria.Main __instance)
        {
            // The specially named __instance parameter is automatically filled in by Harmony to reference the Main instance that is calling this method.
            
            // We'll do all our setup in a try-catch so we don't crash Terraria if something goes wrong.
            try
            {
                // Load our custom projectile's texture from the embedded resource bytes we extracted in ConfigurationLoaded()
                ProjectileGraphic = HHelpers.AssetHandling.CreateTexture2DFromImageBytes(ProjectileGraphicBytes, __instance.GraphicsDevice, true, new Vector4(1f, 1f, 1f, 1f / 2.2f));
                    // The PNG used in this example was written with an alpha channel gamma space that does not look good in Terraria, hence the use of the final parameter (gammaCorrection) to do some correction.
                    // NOTE: Only use the gammaCorrection parameter if you know what you are doing, otherwise just skip it (or set to default value of null)

                // Just like with creating new items, we need to expand a bunch of projectile design data arrays to reach up to our projectile's ID.
                // We will fill the placeholder spaces between the last vanilla ID and our custom ID with default values.
                Helper_ExpandArray(ref Terraria.Main.projFrames, ProjectileID, 0);
                    Terraria.Main.projFrames[ProjectileID] = 8; // Our custom projectile has 8 frames of animation
                Helper_ExpandArray(ref Terraria.Main.projHook, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.Main.projHostile, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.Main.projPet, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.CanDistortWater, ProjectileID, false);
                //Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.CountsAsHoming, ProjectileID, false);
                    // CountsAsHoming no longer exists as of 1.4.3.0
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.DismountsPlayersOnHit, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.DontApplyParryDamageBuff, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.DontAttachHideToAlpha, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.DrawScreenCheckFluff, ProjectileID, 480);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.ExtendedCanHitCheckRange, ProjectileID, 0f);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.ExtendedCanHitCheckSearch, ProjectileID, null);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.ForcePlateDetection, ProjectileID, null);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.ImmediatelyUpdatesNPCBuffFlags, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.IsADD2Turret, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.IsAGolfBall, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.IsAMineThatDealsTripleDamageWhenStationary, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.IsARocketThatDealsDoubleDamageToPrimaryEnemy, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.IsAWhip, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.LightPet, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.MinionSacrificable, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.MinionShot, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.MinionTargettingFeature, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.NeedsUUID, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.NoLiquidDistortion, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.RocketsSkipDamageForPlayers, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.SentryShot, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.StardustDragon, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.StormTiger, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.TrailCacheLength, ProjectileID, 10);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.TrailingMode, ProjectileID, -1);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.TurretFeature, ProjectileID, false);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.WindPhysicsImmunity, ProjectileID, null);
                    Terraria.ID.ProjectileID.Sets.WindPhysicsImmunity[ProjectileID] = true; // We want our custom projectile to be immune to wind since it is an energy ball
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.YoyosLifeTimeMultiplier, ProjectileID, -1f);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.YoyosMaximumRange, ProjectileID, 200f);
                Helper_ExpandArray(ref Terraria.ID.ProjectileID.Sets.YoyosTopSpeed, ProjectileID, 10f);
                
                // Next we need to extend the Terraria.GameContent.TextureAssets.Projectile array to reach our Projectile's ID in size and include our Projectile's texture
                Helper_ExpandArray(ref Terraria.GameContent.TextureAssets.Projectile, ProjectileID, Terraria.GameContent.TextureAssets.Projectile[0]); // Use the first texture in the list as a default placeholder
                // Now create a ReLogic.Content.Asset<Texture2D> to hold our projectile's graphic
                Type assetType = typeof(ReLogic.Content.Asset<Texture2D>);
                ProjectileGraphicAsset = (ReLogic.Content.Asset<Texture2D>)HHelpers.ActivateInstanceUsingFirstConstructor(assetType, new object[] { "Projectile_WeaponProcessorOnAStick_ElectricBall" }); // This type has only one constructor
                    // By using the ActivateInstanceUsingFirstConstructor() helper method, we avoid directly using Reflection and thus don't violate plugin Security Level 4
                HHelpers.SetPropertyValueWithReflection("Value", ProjectileGraphicAsset, ProjectileGraphic); // Set the Value (which is of type Texture2D)
                HHelpers.SetPropertyValueWithReflection("State", ProjectileGraphicAsset, ReLogic.Content.AssetState.Loaded); // Set the loaded state to Loaded
                Terraria.GameContent.TextureAssets.Projectile[ProjectileID] = ProjectileGraphicAsset; // Assign the newly created asset to our projectile's slot in the array
            }
            catch (Exception e)
            {
                // Something went wrong, so we will set PluginLoadedSuccessfully to false and thus disable the rest of our plugin code.
                // If you are debugging a plugin, however, you probably DONT want to do this, or you might miss thrown exceptions.
                PluginLoadedSuccessfully = false;
                return;
            }
        }

        public static void PlayerCtor_ExpandProjectileArrays(Terraria.Player __instance)
        {
            // The specially named __instance parameter is automatically filled in by Harmony to reference the Player instance that is calling this method.

            // Expand the ownedProjectileCounts array to reach our custom projectile's ID
            Helper_ExpandArray(ref __instance.ownedProjectileCounts, ProjectileID, 0);
        }
        
        public static void SetDefaults_SetProjectileDefaults(Terraria.Projectile __instance, int Type)
        {
            // The specially named __instance parameter is automatically filled in by Harmony to reference the Projectile instance that is calling this method.
            // The type parameter is automatically filled in by Harmony to be the same value as the type parameter that was passed to the original method.

            if (!PluginLoadedSuccessfully) // If there was a problem loading things for our plugin, we will abort this stub method so we dont crash Terraria.
                return;

            if (Type == ProjectileID) // Only apply our custom projectile's defaults if the Projectile that had SetDefaults called on it actually is our projectile
            {
                __instance.width = 40; // Width of the projectile
                __instance.height = 40; // Height of the projectile
                    // In this example, the texture frame size is actually 52x52, but I want the projectile's hitbox to be smaller (since the edges of the texture are almost fully transparent).
                    // We'll compensate for the incorrect size in our custom drawing code.
                __instance.scale = 1f; // Scale of the projectile
                __instance.aiStyle = 1; // The type of update logic that the projectile will use
                    // We actually don't even need to set this, because of our prefix patch for Terraria.Projectile.AI()
                __instance.alpha = 255; // The projectile's opacity
                __instance.magic = true; // Whether or not the projectile counts as a magic projectile (and is thus affected by magic dmg/crit/etc stat boosters)
                __instance.friendly = true; // Whether or not the projectile will damage players
                __instance.penetrate = 5; // How many things the projectile can pierce before despawning
                __instance.ignoreWater = true; // Whether or not the projectile will interact with water
                __instance.timeLeft = 600; // How long (in frames) until the projectile despawns
                __instance.extraUpdates = 1; // How many times the projectile will update per frame (i.e. run its AI() method, among other things)

                __instance.active = true; // The original method sets active to false for projectiles outside of the vanilla ID range, so we must set active to true here in our postfix patch

                __instance.maxPenetrate = __instance.penetrate; // Set initial value for maxPenetrate

                __instance.width = (int)((float)__instance.width * __instance.scale); // Combine design width with scale to produce initial width
                __instance.height = (int)((float)__instance.height * __instance.scale); // Combine design height with scale to produce initial height

                // Also, in case the async texture loading replaced our projectile's texture with something else, let's make sure the right texture is in our projectile's slot
                if (ProjectileGraphicAsset != null)
                    Terraria.GameContent.TextureAssets.Projectile[ProjectileID] = ProjectileGraphicAsset; // Reassign the field with the asset we created earlier
            }
        }

        public static bool AI_ProjectileUpdate(Terraria.Projectile __instance)
        {
            // The specially named __instance parameter is automatically filled in by Harmony to reference the Projectile instance that is calling this method.

            if (!PluginLoadedSuccessfully) // If there was a problem loading things for our plugin, we will abort this stub method so we dont crash Terraria.
                return true; // Allow the original method to execute

            if (__instance.type == 3001) // Only run this update code if the projectile that's calling AI actually is our custom projectile
            {
                // Increment animation frame
                __instance.frame++;
                if (__instance.frame >= 8 * 4) // We'll let the frame variable go up to 4x the actual frame count, then divide frame by 4 in drawing code to get 1/4 of a 60fps framerate
                    __instance.frame = 0;

                // Make some dust particles
                if (Terraria.Main.rand.Next(3) == 0)
                {
                    float scale = 0.35f;
                    if (Terraria.Main.rand.Next(3) == 0)
                        scale = 0.55f;
                    Terraria.Dust dust = Terraria.Dust.NewDustDirect(__instance.position,
                                                                     __instance.width,
                                                                     __instance.height,
                                                                     226, // This dust type is the spark dust from the Electrosphere Launcher
                                                                     __instance.velocity.X * 0.75f,
                                                                     __instance.velocity.Y * 0.75f,
                                                                     50,
                                                                     Color.White,
                                                                     scale);
                    dust.noGravity = true;
                    dust.velocity = Vector2.Lerp(dust.velocity, __instance.velocity, 0.3f); // Influence dust velocity slightly with our projectile velocity
                    int speedRand = Terraria.Main.rand.Next(10);
                    if (speedRand > 8) // Randomly boost the speed of some dust particles
                        dust.velocity *= 1.5f;
                    if (speedRand > 6)
                        dust.velocity *= 1.2f;
                }

                // Add some light to at our particle's position
                Terraria.Lighting.AddLight(__instance.Center, 0.25f, 0.55f, 0.75f);
                
                return false; // Skip over original method
            }

            return true; // Allow original method to run
        }

        public static bool DrawProj_DrawProjectile(Terraria.Main __instance, int i)
        {
            // The specially named __instance parameter is automatically filled in by Harmony to reference the Main instance that is calling this method.
            // The i parameter is automatically filled in by Harmony to be the same value as the i parameter that was passed to the original method.

            if (!PluginLoadedSuccessfully) // If there was a problem loading things for our plugin, we will abort this stub method so we dont crash Terraria.
                return true; // Allow the original method to execute

            if (Terraria.Main.projectile[i].type == 3001) // Only run this drawing code if the projectile that's trying to draw actually is our custom projectile
            {
                // Get our projectile from the main projectile array
                Terraria.Projectile proj = Terraria.Main.projectile[i];

                // This must be called before any projectile is drawn
                __instance.PrepareDrawnProjectileDrawing(proj);

                // Get how much light is at the projectile's current position (will be used for a slight color modulation effect)
                Color lightAtProjPos = Terraria.Lighting.GetColor((int)(proj.position.X + (proj.width * 0.5f)) / 16,
                                                                  (int)((proj.position.Y + (proj.height * 0.5f)) / 16));
                // Lerp at a constant 75% between the tile light and pure white to slightly modulate the projectile's color
                Color projColorMix = Color.Lerp(lightAtProjPos, Color.White, 0.75f);

                // Fade out the projectile if it's close to being removed
                if (proj.timeLeft <= 30)
                    projColorMix = projColorMix * (proj.timeLeft / 30f);

                // Get the projectile's texture and figure out what section contains the frame we want
                Texture2D projTex = Terraria.GameContent.TextureAssets.Projectile[proj.type].Value;
                int texFrameHeight = projTex.Height / Terraria.Main.projFrames[proj.type];
                int texFrameYPos = texFrameHeight * (proj.frame / 4); // Our code in AI() makes proj.frame go up to 4x the actual max frame so that we can divide it here for 1/4 of a 60fps framerate

                // Find the viewportspace position to draw the projectile at
                Vector2 drawPosition = proj.position
                                       + (new Vector2(projTex.Width, texFrameHeight) * 0.5f) // Use the texture's dimensions instead of proj.Width and proj.Height, since our projectile's Width & Height are undersized (for collision purposes)
                                       + (Vector2.UnitY * proj.gfxOffY)
                                       - Terraria.Main.screenPosition;

                // Draw it
                Terraria.Main.EntitySpriteDraw(projTex,
                                               drawPosition,
                                               new Rectangle(0, texFrameYPos, projTex.Width, texFrameHeight),
                                               projColorMix,
                                               proj.rotation,
                                               new Vector2(projTex.Width * 0.5f, texFrameHeight * 0.5f),
                                               1f,
                                               SpriteEffects.None,
                                               0);

                return false; // Skip over original method
            }

            return true; // Allow original method to run
        }

        public static bool Kill_KillProjectile(Terraria.Projectile __instance)
        {
            // The specially named __instance parameter is automatically filled in by Harmony to reference the Projectile instance that is calling this method.

            if (!PluginLoadedSuccessfully) // If there was a problem loading things for our plugin, we will abort this stub method so we dont crash Terraria.
                return true; // Allow the original method to execute

            if (__instance.type == 3001)
            {
                // Instead of instantly being removed, we'll make our projectile fade out over 20 frames
                if (__instance.timeLeft > 30)
                {
                    __instance.timeLeft = 30; // Set the projectile to disappear 30 frames from now
                    __instance.tileCollide = false; // Disable tile collisions during this fade out period
                    return false; // Skip the original Kill method (so our projectile stays alive just a bit longer)
                }
                else if (__instance.timeLeft > 0)
                    return false; // Don't allow the original Kill method to run (yet), since we still have frames left before timeLeft reaches 0
            }

            return true; // Allow original method to run
        }

        #endregion


        #region Helpers

        // Simple helper for expanding the various design code arrays in Terraria
        private static void Helper_ExpandArray<T>(ref T[] array, int newSize, T defaultValue)
        {
            if (array.Length < newSize)
            {
                List<T> values = array.ToList();
                for (int i = array.Length; i <= newSize; i++)
                    values.Add(defaultValue);
                array = values.ToArray();
            }
        }

        #endregion
    }
}