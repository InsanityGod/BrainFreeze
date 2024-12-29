﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BrainFreeze.Behaviors
{
    public class FrozenNamePrefix : CollectibleBehavior
    {

        public FrozenNamePrefix(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
        {
            var code = $"{itemStack.Collectible.Code.Domain}:item-{itemStack.Collectible.Code.Path}";
            if(sb.ToString() == code)
            {
                var itemCode = itemStack.Collectible.Code;
                sb.Clear();
                sb.Append(Lang.Get("brainfreeze:frozen"));
                sb.Append(' ');
                sb.Append(Lang.Get($"{itemCode.Domain}:item-{itemStack.Collectible.CodeWithoutFrozenPart(itemCode.Path)}"));
            }
        }
    }
}
