using BrainFreeze.Code.Items;
using HarmonyLib;
using HydrateOrDiedrate;
using InsanityLib.Extended.Transitions;
using System.Linq;
using Vintagestory.API.Common;

namespace BrainFreeze.Code.HarmonyPatches.FrozenInteractions.Consumption;

[HarmonyPatchCategory("hydrateordiedrate")]
public static class HydrateOrDiedratePatches
{
    [HarmonyPatch("HydrationManager", "GetHydration")]
    [HarmonyPrefix]
    public static bool GetHydrationPrefix(ItemStack itemStack, ref float __result)
    {
        if (itemStack.Collectible is Ice ice)
        {
            var content = ice.GetContent(itemStack);
            if(content != null)
            {
                __result = HydrationManager.GetHydration(content);

                if(__result <= 0)
                {
                    //TODO create extra helper methods for this in InsanityLib
                    //Double check original liquid
                    var meltTransition = content.Collectible.TransitionableProps.FirstOrDefault(static trans =>
                    {
                        var handler = CustomTransition.ExtendedEnum.FindHandler(trans.Type);

                        return handler is not null && handler.TransitionCode.Domain == "brainfreeze" && handler.TransitionCode.Path == "melt";
                    });

                    if(meltTransition is not null)
                    {
                        var newStack = meltTransition.TransitionedStack.ResolvedItemStack?.Clone();
                        if(newStack is not null)
                        {
                            newStack.StackSize = (int)(meltTransition.TransitionRatio * itemStack.StackSize);

                            __result = HydrationManager.GetHydration(newStack);
                        }
                    }
                }
                return false;
            }
        }
        return true;
    }
}
