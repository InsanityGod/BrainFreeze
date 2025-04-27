using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.HarmonyPatches.FrozenInteractions
{
    [HarmonyPatch]
    public static class PreventMovingFrozenLiquidItemSlot
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            var derivedTypes = AccessTools.AllTypes().Where(type => type != typeof(ItemSlotCreative) && typeof(ItemSlot).IsAssignableFrom(type));
            //Excluding ItemSlotCreative because it decided it wanted to be special and rename the parameter... which means harmony will crash if we patch it

            foreach (var type in derivedTypes)
            {
                var method1 = AccessTools.Method(type, "ActivateSlotRightClick", parameters: new Type[] { typeof(ItemSlot), typeof(ItemStackMoveOperation).MakeByRefType() });
                if (method1 != null) yield return method1;
                var method2 = AccessTools.Method(type, "ActivateSlotLeftClick", parameters: new Type[] { typeof(ItemSlot), typeof(ItemStackMoveOperation).MakeByRefType() });
                if (method2 != null) yield return method2;
            }
        }

        public static bool Prefix(ItemSlot __instance, ItemSlot sourceSlot)
        {
            if (sourceSlot.Inventory is InventoryPlayerMouseCursor)
            {
                if (sourceSlot.Empty) return true;

                var itemStack = sourceSlot.Itemstack;
                if (itemStack.Collectible is BlockLiquidContainerBase liquidContainer)
                {
                    itemStack = liquidContainer.GetContent(itemStack);
                }

                if (itemStack?.Collectible?.Variant["brainfreeze"] != null)
                {
                    var typeName = __instance.GetType().Name;
                    if (__instance is ItemSlotWatertight || typeName == "ItemSlotMixingBowl" || typeName == "ItemSlotPotInput")
                    {
                        if (__instance.Inventory?.Api is ICoreClientAPI clientApi)
                        {
                            clientApi.TriggerIngameError(__instance, "liquid-frozen", Lang.Get("brainfreeze:liquid-frozen"));
                        }

                        return false;
                    }
                }

                return true;
            }

            ItemStack itemStack1 = __instance.Itemstack;
            ItemStack itemStack2 = sourceSlot.Itemstack;
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
    }
}