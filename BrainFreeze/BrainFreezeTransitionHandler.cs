using CustomTransitionLib;
using CustomTransitionLib.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BrainFreeze
{
    public class BrainFreezeTransitionHandler : ICustomTransitionHandler<EnumBrainFreezeTransitionType>
    {
        public string ModId => "brainfreeze";

        public float GetTransitionRateMul(IWorldAccessor world, ItemSlot inSlot, EnumBrainFreezeTransitionType transType, float currentResult)
        {
            var pos = inSlot.Inventory?.Pos;
            float multiplier = 0;
            if(pos != null)
            {
                var fire = world.BlockAccessor.GetBlockEntity(pos) as IFirePit;

                if(fire != null && fire.IsBurning)
                {
                    multiplier = -30;
                }
                else
                {
                    var climate = world.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.NowValues);

                    var room = world.Api.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(pos);
                    if(room.ExitCount == 0)
                    {
                        //TODO make custom compatibility with my currently none existent Better Immersion mod
                        multiplier = -1;
                    }
                    else
                    {
                        multiplier = climate.Temperature >= 0 ? 
                            (-1f / 20f) * climate.Temperature:
                            (-1f / 10f) * climate.Temperature;
                    }
                }
            }
            
            if(transType == EnumBrainFreezeTransitionType.Thaw)
            {
                multiplier *= -1;
            }

            return multiplier;
        }

        public void PostOnTransitionNow(CollectibleObject collectible, ItemSlot slot, TransitionableProperties props, EnumBrainFreezeTransitionType transType, ref ItemStack result)
        {
            //TODO: I wish there was a better way to do this...
            if(result.Collectible?.MatterState == EnumMatterState.Liquid)
            {
                var wasLocked = slot.Inventory?.TakeLocked;
                if(slot.Inventory != null)
                {
                    slot.Inventory.TakeLocked = false;
                }

                slot.Itemstack = result;
                if (slot.CanTake() && slot.Inventory?.Pos != null)
                {
                    if (slot.Inventory.Api.World.BlockAccessor.GetBlock(slot.Inventory.Pos) is BlockLiquidContainerBase liquidContainer)
                    {
                        liquidContainer.SetContent(slot.Inventory.Pos, result);
                    }
                    else
                    {
                        slot.Inventory.Api.World.PlaySoundAt(new AssetLocation("sounds/environment/smallsplash"), slot.Inventory.Pos.X, slot.Inventory.Pos.Y, slot.Inventory.Pos.Z);
                    }
                    result.StackSize = 0;
                }

                if(slot.Inventory != null)
                {
                    slot.Inventory.TakeLocked = wasLocked.Value;
                }
            }
        }
    }
}
