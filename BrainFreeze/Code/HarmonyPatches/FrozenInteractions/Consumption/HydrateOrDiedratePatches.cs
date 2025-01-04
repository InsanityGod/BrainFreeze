using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using BrainFreeze.Code.Items;
using HarmonyLib;
using HydrateOrDiedrate.patches;
using Vintagestory.API.Common;

namespace BrainFreeze.Code.HarmonyPatches.FrozenInteractions.Consumption
{
    [HarmonyPatchCategory("hydrateordiedrate")]
    public static class HydrateOrDiedratePatches
    {
        [HarmonyPatch("HydrationManager", "GetHydration")]
        [HarmonyPrefix]
        public static bool GetHydrationPrefix(ItemStack itemStack, ref float __result)
        {
            if (itemStack.Collectible is IceCube iceCube)
            {
                var content = iceCube.GetContent(itemStack);
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
