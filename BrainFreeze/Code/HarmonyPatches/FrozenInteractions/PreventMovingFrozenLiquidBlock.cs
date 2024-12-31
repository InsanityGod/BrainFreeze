using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.HarmonyPatches.FrozenInteractions
{
    [HarmonyPatch(typeof(BlockLiquidContainerBase), nameof(BlockLiquidContainerBase.OnBlockInteractStart))]
    public static class PreventMovingFrozenLiquidBlock
    {
        public static bool Prefix(BlockLiquidContainerBase __instance, IPlayer byPlayer, ref bool __result)
        {
            var itemslot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (itemslot == null) return true;

            var content = itemslot.Itemstack == null ? null : __instance.GetContent(itemslot.Itemstack);

            if (content?.Item?.Variant["frozen"] != null)
            {
                if (itemslot.Inventory?.Api is ICoreClientAPI clientApi)
                {
                    clientApi.TriggerIngameError(__instance, "liquid-frozen", Lang.Get("brainfreeze:liquid-frozen"));
                }
                __result = true;
                return false;
            }

            return true;
        }
    }
}