using HarmonyLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.HarmonyPatches.FrozenInteractions;


[HarmonyPatch(typeof(BlockLiquidContainerBase))]
public static class PreventFrozenLiquidContainerBaseInteractions
{
    [HarmonyPatch(nameof(BlockLiquidContainerBase.TryPutLiquid), argumentTypes: new Type[] { typeof(ItemStack), typeof(ItemStack), typeof(float) })]
    [HarmonyPrefix]
    public static bool TryPutLiquidPrefix(BlockLiquidContainerBase __instance, ItemStack containerStack, ItemStack liquidStack, float desiredLitres, ref int __result)
    {
        if (liquidStack?.Collectible?.Variant["brainfreeze"] != null)
        {
            if (Traverse.Create(__instance).Field<ICoreAPI>("api").Value is ICoreClientAPI clientApi)
            {
                clientApi.TriggerIngameError(null, "liquid-frozen", Lang.Get("brainfreeze:liquid-frozen")); //TODO apply on liquid based meals as well
            }
            __result = 0;
            return false;
        }
        return true;
    }

    [HarmonyPatch(nameof(BlockLiquidContainerBase.TryMergeStacks))]
    [HarmonyPrefix]
    public static bool TryMergeStacksPrefix(BlockLiquidContainerBase __instance, ItemStackMergeOperation op)
    {
        ItemStack itemStack1 = op.SourceSlot?.Itemstack;
        ItemStack itemStack2 = op.SinkSlot?.Itemstack;
        if (itemStack1?.Collectible is BlockLiquidContainerBase blockLiquidContainer)
        {
            itemStack1 = blockLiquidContainer.GetContent(itemStack1);
        }

        if (itemStack2?.Collectible is BlockLiquidContainerBase blockLiquidContainer2)
        {
            itemStack2 = blockLiquidContainer2.GetContent(itemStack2);
        }

        if (itemStack1?.StackSize == itemStack2?.StackSize) return true; //So we can actually stack full buckets of frozen liquid

        if (itemStack1?.Item?.Variant["brainfreeze"] != null || itemStack2?.Item?.Variant["brainfreeze"] != null)
        {
            if (Traverse.Create(__instance).Field<ICoreAPI>("api").Value is ICoreClientAPI clientApi)
            {
                clientApi.TriggerIngameError(null, "liquid-frozen", Lang.Get("brainfreeze:liquid-frozen"));
            }
            return false;
        }
        return true;
    }

    [HarmonyPatch(nameof(BlockLiquidContainerBase.OnBlockInteractStart))]
    [HarmonyPrefix]
    public static bool OnBlockInteractStartPrefix(BlockLiquidContainerBase __instance, IPlayer byPlayer, ref bool __result)
    {
        var itemslot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (itemslot == null) return true;

        var content = itemslot.Itemstack == null ? null : __instance.GetContent(itemslot.Itemstack);

        if (content?.Item?.Variant["brainfreeze"] != null)
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
