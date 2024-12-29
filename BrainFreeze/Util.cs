﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BrainFreeze
{
    public static class Util
    {
        public static bool IsFrozen(ItemSlot slot)
        {
            if(slot.Itemstack?.Collectible == null) return false;
            if(slot.Itemstack.Collectible is BlockLiquidContainerBase liquidContainerBase)
            {
                var content = liquidContainerBase.GetContent(slot.Itemstack);
                return content?.Collectible.Variant["frozen"] != null;
            }

            return slot.Itemstack.Collectible.Variant["frozen"] != null;
        }

        public static bool IsFrozenWithWarning(ItemSlot slot)
        {
            var result = IsFrozen(slot);
            if (result && slot.Inventory?.Api is ICoreClientAPI coreClientApi)
            {
                coreClientApi.TriggerIngameError(slot.Itemstack.Collectible, "consumable-frozen", Lang.Get("brainfreeze:consumable-frozen"));
            }
            return result;
        }

        public static string CodeWithoutFrozenPart(this CollectibleObject obj, string code = null)
        {
            var frozenPosition = obj.VariantStrict.IndexOfKey("brainfreeze");
            var parts = (code ?? obj.Code.ToString()).Split('-').ToList();
            parts.RemoveAt(1 + frozenPosition);

            return string.Join("-", parts);
        }
    }
}
