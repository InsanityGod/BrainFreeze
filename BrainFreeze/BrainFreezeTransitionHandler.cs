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
            if(world.Api.Side == EnumAppSide.Server)
            {
                Console.WriteLine("Server Side Trigger");
            }
            else
            {
                Console.WriteLine("Client side trigger");
            }
            //TODO This is being called way too often client side
            var pos = inSlot?.Inventory?.Pos;
            if(pos == null) return 0;

            var climate = world.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.NowValues);
            switch (transType)
            {
                case EnumBrainFreezeTransitionType.Freeze:
                    if(climate.Temperature > 0) return 0;
                    return (20 - climate.Temperature) / 20;
            }


            return 1;
        }
    }
}
