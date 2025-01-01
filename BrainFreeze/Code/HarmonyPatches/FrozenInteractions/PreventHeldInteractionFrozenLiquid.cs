using HarmonyLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.HarmonyPatches.FrozenInteractions
{
    //TODO cleanup harmony patches
    [HarmonyPatch(typeof(BlockLiquidContainerBase), nameof(BlockLiquidContainerBase.TryPutLiquid), argumentTypes: new Type[] { typeof(ItemStack), typeof(ItemStack), typeof(float) })]
    public static class PreventTryFillFromBlockForFrozenLiquids
    {
        public static bool Prefix(BlockLiquidContainerBase __instance, ItemStack containerStack, ItemStack liquidStack, float desiredLitres, ref int __result)
        {
            if (liquidStack?.Collectible?.Variant["frozen"] != null)
            {
                if (Traverse.Create(__instance).Field<ICoreAPI>("api").Value is ICoreClientAPI clientApi)
                {
                    clientApi.TriggerIngameError(null, "liquid-frozen", Lang.Get("brainfreeze:liquid-frozen"));
                }
                __result = 0;
                return false;
            }
            return true;
        }
    }
}