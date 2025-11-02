using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BrainFreeze.Code.Behaviors;

public class FrozenNamePrefix(CollectibleObject collObj) : CollectibleBehavior(collObj)
{
    public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
    {
        var itemCode = itemStack.Collectible.Code;
        
        var withoutFrozenCode = $"{itemCode.Domain}:item-{itemStack.Collectible.PathWithoutFrozenPart()}";
        var withoutFrozenStr = Lang.Get(withoutFrozenCode);
        if(withoutFrozenCode == withoutFrozenStr)
        {
            withoutFrozenStr = Lang.Get($"{itemCode.Domain}:incontainer-item-{itemStack.Collectible.PathWithoutFrozenPart()}");
        }

        sb.Replace($"{itemStack.Collectible.Code.Domain}:item-{itemStack.Collectible.Code.Path}",Lang.Get("brainfreeze:frozen", withoutFrozenStr));
    }
}