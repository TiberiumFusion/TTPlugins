# TTPlugins
TTPlugins is the user plugin framework for Terraria Tweaker 2, a Terraria client patcher. TTPlugins is included with Terraria Tweaker 2.3+ and provides a convenient means for users to modify Terraria by writing high-level, dynamic patches in C#.

* Build tools are optional. Write some C# 5.0 compatible source code and TTPlugins will compile it for you.
* Persistent plugin savedata allows for storing data like user preferences between Terraria launches.
* User-adjustable plugin security level limits plugin code to a restricted subset of .NET features.
* Plugin framework provides ways for plugins to use certain powerful .NET features in a more secure, restricted manner.

### Dynamic patching
TTPlugins uses the fantastic [Harmony](https://github.com/pardeike/Harmony) library to patch Terraria at runtime. Harmony modifies the execution flow of .NET applications in memory and does not touch any on-disk files.

### How easy is it?
Below is an example of a very basic but complete \*.cs plugin file that gives the player superspeed. This is the entire plugin source code. Optional plugin features are not present in this example.
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
Please refer to the [Wiki](https://github.com/TiberiumFusion/TTPlugins/wiki) for primary documentation, including step-by-step tutorials and general library reference.

### Example plugins
This repository contains several [example plugins](https://github.com/TiberiumFusion/TTPlugins/tree/master/ExamplePlugins) that cover some common & advanced plugin tasks.

### Technical documentation
Complete reference docs for TTPlugins can be found [here](https://www.tiberiumfusion.com/product/ttplugins/reference/html/432f1745-05bc-1912-8400-537f02fafa44.htm). Please note that this material has very sparse descriptions & remarks and may only be helpful to advanced plugin authors. The primary how-to documentation is on the [Wiki](https://github.com/TiberiumFusion/TTPlugins/wiki).
