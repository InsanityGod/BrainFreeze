using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BrainFreeze.Code
{
    public static class Util
    {
        public static bool IsFrozen(ItemSlot slot)
        {
            if (slot.Itemstack?.Collectible == null) return false;
            if (slot.Itemstack.Collectible is BlockLiquidContainerBase liquidContainerBase)
            {
                var content = liquidContainerBase.GetContent(slot.Itemstack);
                return content?.Collectible.Variant["brainfreeze"] != null;
            }

            return slot.Itemstack.Collectible.Variant["brainfreeze"] != null;
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
            var i = 0;
            for (var index = 0; index < obj.VariantStrict.Count; index++)
            {
                if(obj.VariantStrict.GetKeyAtIndex(index) == "brainfreeze") break;
                if (!string.IsNullOrEmpty(obj.VariantStrict.GetValueAtIndex(index))) i++; //this is to deal with some weird empty variants added by other mods
            }
            

            //var frozenPosition = obj.VariantStrict.IndexOfKey("brainfreeze");
            var parts = (code ?? obj.Code.ToString()).Split('-').ToList();
            //TODO?: parts.RemoveAt(1 + frozenPosition);
            parts.RemoveAt(i + 1);

            return string.Join("-", parts);
        }
    }
}