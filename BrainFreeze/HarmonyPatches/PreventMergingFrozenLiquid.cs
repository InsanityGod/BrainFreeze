using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BrainFreeze.HarmonyPatches
{
    [HarmonyPatch(typeof(BlockLiquidContainerBase), nameof(BlockLiquidContainerBase.TryMergeStacks))]
    public static class PreventMergingFrozenLiquid
    {
        public static bool Prefix(BlockLiquidContainerBase __instance, ItemStackMergeOperation op)
        {

            ItemStack itemStack1 = op.SourceSlot?.Itemstack;
            ItemStack itemStack2 = op.SinkSlot?.Itemstack;
            if(itemStack1?.Collectible is BlockLiquidContainerBase blockLiquidContainer)
            {
                itemStack1 = blockLiquidContainer.GetContent(itemStack1);
            }

            if(itemStack2?.Collectible is BlockLiquidContainerBase blockLiquidContainer2)
            {
                itemStack2 = blockLiquidContainer2.GetContent(itemStack2);
            }

            if(itemStack1?.StackSize == itemStack2?.StackSize) return true; //So we can actually stack full buckets of frozen liquid

            if (itemStack1?.Item?.Variant["frozen"] != null || itemStack2?.Item?.Variant["frozen"] != null)
            {
                if(Traverse.Create(__instance).Field<ICoreAPI>("api").Value is ICoreClientAPI clientApi)
                {
                    clientApi.TriggerIngameError(null, "liquid-frozen", Lang.Get("brainfreeze:liquid-frozen"));
                }
                return false;
            }
            return true;
        }
    }
}
