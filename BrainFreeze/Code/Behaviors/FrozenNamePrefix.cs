using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BrainFreeze.Code.Behaviors
{
    public class FrozenNamePrefix : CollectibleBehavior
    {
        public FrozenNamePrefix(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
        {
            var code = $"{itemStack.Collectible.Code.Domain}:item-{itemStack.Collectible.Code.Path}";
            if (sb.ToString() == code)
            {
                var itemCode = itemStack.Collectible.Code;
                sb.Clear();
                sb.Append(Lang.Get("brainfreeze:frozen"));
                sb.Append(' ');
                var withoutFrozenCode = $"{itemCode.Domain}:item-{itemStack.Collectible.CodeWithoutFrozenPart(itemCode.Path)}";
                var withoutFrozenStr = Lang.Get(withoutFrozenCode);
                if(withoutFrozenCode == withoutFrozenStr)
                {
                    withoutFrozenStr = Lang.Get($"{itemCode.Domain}:incontainer-item-{itemStack.Collectible.CodeWithoutFrozenPart(itemCode.Path)}");
                }
                sb.Append(withoutFrozenStr);
            }
        }
    }
}