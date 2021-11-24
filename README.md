# TTPlugins
TTPlugins is the usercode plugin framework for Terraria Tweaker 2, a Terraria client patcher. TTPlugins is included with Terraria Tweaker 2.3+ and allows users to modify Terraria by writing plugins that contain dynamic patches. In this way, Terraria Tweaker's patching framework is leveraged to simplify the process of modifying the compiled Terraria assembly. Even if you have minimal knowledge of .NET or C#, creating your own Terraria modifications is very accessible with TTPlugins.

## What is a plugin?
Generally speaking, a plugin is any module of external code that runs on a framework provided by some other code. TTPlugins provides a plugin interface for writing & managing dynamic patch methods that are applied to Terraria at runtime, all under the supervision of the Terraria Tweaker patching framework.

### **TTPlugins features the following:**
* Support for source code plugins and precompiled plugin assemblies
* Adjustable plugin security levels to restrict partially trusted code
* Persistent plugin savedata for storing data like user preferences between Terraria launches
* Various framework helpers available to user plugin code

### Dynamic patching
TTPlugins uses the fantastic [Harmony](https://github.com/pardeike/Harmony) library to patch Terraria at runtime. Harmony modifies the execution flow of .NET applications in memory and does not touch any on-disk files.

### How easy is it?
Here's a quick example of a super basic but fully functional plugin that gives the player superspeed. This is the entire plugin source code. Optional plugin features, like Identity and persistent savedata, are not present in this example.
```C#
using System;
using com.tiberiumfusion.ttplugins.HarmonyPlugins;
namespace MyPlugin
{
    public class SuperSpeed : HPlugin
    {
        public override void PrePatch()
        {
            CreateHPatchOperation("Terraria.Player", "UpdateEquips", "SuperSpeedPatch", HPatchLocation.Prefix);
        }
		
	public static void SuperSpeedPatch(Terraria.Player __instance)
        {
            __instance.moveSpeed += 20.0f;
        }
    }
}
```

## For plugin authors
If you are interesting in writing your own plugins, please visit the [Wiki](https://github.com/TiberiumFusion/TTPlugins/wiki) for tutorials, videos, and reference material.

### Example plugins
This repository contains several [example plugins](https://github.com/TiberiumFusion/TTPlugins/tree/master/ExamplePlugins) that cover a variety of common & advanced plugin tasks.

### Technical documentation
Complete reference docs for TTPlugins can be found [here](https://www.tiberiumfusion.com/product/ttplugins/reference/html/432f1745-05bc-1912-8400-537f02fafa44.htm). Please note that this material has very sparse descriptions & remarks and may only be helpful to advanced plugin authors. The primary how-to documentation is on the [Wiki](https://github.com/TiberiumFusion/TTPlugins/wiki).
