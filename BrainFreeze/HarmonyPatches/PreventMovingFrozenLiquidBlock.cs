using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BrainFreeze.HarmonyPatches
{
    [HarmonyPatch(typeof(BlockLiquidContainerBase), nameof(BlockLiquidContainerBase.OnBlockInteractStart))]
    public static class PreventMovingFrozenLiquidBlock
    {
        public static bool Prefix(BlockLiquidContainerBase __instance, IPlayer byPlayer, ref bool __result)
        {
            var itemslot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if(itemslot == null) return true;

            var content = itemslot.Itemstack == null ? null : __instance.GetContent(itemslot.Itemstack);
        
            if (content?.Item?.Variant["frozen"] != null)
            {
                if(itemslot.Inventory?.Api is ICoreClientAPI clientApi)
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
