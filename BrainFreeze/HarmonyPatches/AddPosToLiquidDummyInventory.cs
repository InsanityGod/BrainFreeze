using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BrainFreeze.HarmonyPatches
{
    [HarmonyPatch(typeof(BlockContainer), "GetContentInDummySlot")]
    public static class AddPosToLiquidDummyInventory
    {
        public static void Postfix(ItemSlot inslot, ref ItemSlot __result)
        {
            if(__result == null) return;
            __result.Inventory.Pos = inslot.Inventory?.Pos;
            if(__result.Inventory.Pos == null && inslot.Inventory is InventoryBasePlayer playerInv)
            {
                __result.Inventory.Pos = playerInv.Player.WorldData.EntityPlayer?.Pos.AsBlockPos;
            }
        }
    }
}
