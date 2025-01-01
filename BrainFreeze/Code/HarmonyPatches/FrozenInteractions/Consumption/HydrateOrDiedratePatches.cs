using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using BrainFreeze.Code.Items;
using HarmonyLib;
using HydrateOrDiedrate.patches;
using Vintagestory.API.Common;

namespace BrainFreeze.Code.HarmonyPatches.FrozenInteractions.Consumption
{
    //TODO has to be changed during update of HydrateOrDieDrate
    [HarmonyPatchCategory("hydrateordiedrate")]
    public static class HydrateOrDiedratePatches
    {
        [HarmonyPatch("HydrateOrDiedrate.HydrationCalculator", "GetTotalHydration")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> GetTotalHydrationTranspilerPatch(IEnumerable<CodeInstruction> instructions) 
        {
            var codes = instructions.ToList();

            var getIceCubeContentOrExisting = AccessTools.Method(typeof(HydrateOrDiedratePatches), nameof(GetIceCubeContentOrExisting));

            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if(code.opcode == OpCodes.Brfalse_S && codes[i-1].opcode == OpCodes.Ldloc_3)
                {
                    codes.InsertRange(i + 1, new CodeInstruction[] 
                    {
                        new(OpCodes.Ldloc_3), //Load ItemStack
                        new(OpCodes.Ldarg_1), //Load IWorldAccessor
                        new(OpCodes.Call, getIceCubeContentOrExisting), //Call method to get content
                        new(OpCodes.Stloc_3) //Assign result to variable
                    });
                    break;
                }
            }

            return codes;
        }

        [HarmonyPatch("HydrateOrDiedrate.patches.TryEatStopCollectibleObjectPatch", "Prefix")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FixHydrationForIceCubes(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var methodToReplace = AccessTools.Method(typeof(HydrationManager), nameof(HydrationManager.GetHydration));
            for (int i = 0; i < codes.Count ; i++)
            {
                var code = codes[i];
                if (code.opcode == OpCodes.Call && code.operand == methodToReplace)
                {
                    codes[i - 1] = new (OpCodes.Ldarg_1);
                    codes[i] = new(OpCodes.Call, AccessTools.Method(typeof(HydrateOrDiedratePatches), nameof(GetHydration)));
                    break;
                }
            }

            return codes;
        }

        [HarmonyPatch("HydrateOrDiedrate.patches.CollectibleObjectGetHeldItemInfoPatch", "Postfix")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FixHydrationDisplayForIceCubes(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var methodToReplace = AccessTools.Method(typeof(HydrationManager), nameof(HydrationManager.GetHydration));
            for (int i = 0; i < codes.Count ; i++)
            {
                var code = codes[i];
                if (code.opcode == OpCodes.Call && code.operand == methodToReplace)
                {
                    codes[i - 1] = new (OpCodes.Ldarg_0);
                    codes[i] = new(OpCodes.Call, AccessTools.Method(typeof(HydrateOrDiedratePatches), nameof(GetHydration)));
                    break;
                }
            }

            return codes;
        }

        public static float GetHydration(ICoreAPI api, ItemSlot slot)
        {
            var content = (slot.Itemstack.Collectible is IceCube iceCube ? iceCube.GetContent(slot.Itemstack) : null) ?? slot.Itemstack;
            var code = content?.Collectible?.Code?.ToString() ?? "Unknown Item";

            var result = HydrationManager.GetHydration(api, code);

            return result;
        }

        public static ItemStack GetIceCubeContentOrExisting(ItemStack itemStack, IWorldAccessor world) => (itemStack.Collectible as IceCube)?.GetContent(itemStack, world) ?? itemStack;
    }
}
