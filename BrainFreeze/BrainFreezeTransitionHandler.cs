using CustomTransitionLib;
using CustomTransitionLib.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace BrainFreeze
{
    public class BrainFreezeTransitionHandler : ICustomTransitionHandler<EnumBrainFreezeTransitionType>
    {
        public string ModId => "brainfreeze";

        //TODO make it so you can't split ice stack

        public float GetTransitionRateMul(IWorldAccessor world, ItemSlot inSlot, EnumBrainFreezeTransitionType transType, float currentResult)
        {
            var pos = inSlot.Inventory?.Pos;
            float multiplier = 0;
            if(pos != null)
            {
                var climate = world.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.NowValues);
                multiplier = climate.Temperature >= 0 ? 
                    (-1f / 20f) * climate.Temperature:
                    (-1f / 10f) * climate.Temperature;
            }
            
            if(transType == EnumBrainFreezeTransitionType.Thaw)
            {
                multiplier *= -1;
            }

            return multiplier;
        }
    }
}
