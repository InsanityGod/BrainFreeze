using BrainFreeze.Code.Items;
using HarmonyLib;
using HydrateOrDiedrate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.HarmonyPatches.FrozenInteractions.Consumption
{
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
                    return false;
                }
            }
            return true;
        }
    }
}
