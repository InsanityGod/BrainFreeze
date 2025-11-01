using InsanityLib.Util.SpanUtil;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BrainFreeze.Code;

public static class Util
{
    public static bool IsFrozen(ItemSlot slot)
    {
        if (slot.Empty) return false;
        if (slot.Itemstack.Collectible is BlockLiquidContainerBase liquidContainerBase)
        {
            return liquidContainerBase.GetContent(slot.Itemstack)?.Collectible.Variant["brainfreeze"] != null;
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

    public static string PathWithoutFrozenPart(this CollectibleObject obj)
    {
        var builder = new StringBuilder();
        builder.Append(obj.FirstCodePartAsSpan());
        foreach((var variantKey, var variantValue) in obj.VariantStrict)
        {
            if(string.IsNullOrWhiteSpace(variantValue) || variantKey == "brainfreeze") continue;

            builder.Append('-');
            builder.Append(variantValue);
        }

        return builder.ToString();
    }
}