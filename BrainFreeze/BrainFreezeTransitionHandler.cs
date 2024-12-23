﻿using CustomTransitionLib;
using CustomTransitionLib.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
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
                        //TODO allow for setting a freezing point in attributes maybe
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

                //Some code to preserve transition states //TODO see if we can do this in a cleaner way
                if(result.Collectible.TransitionableProps != null && slot.Inventory.Api != null)
                {
                    var transitionState = ((ITreeAttribute)slot.Itemstack.Attributes["transitionstate"]).Clone();
				    var freshHours = (transitionState["freshHours"] as FloatArrayAttribute).value;
				    var transitionHours = (transitionState["transitionHours"] as FloatArrayAttribute).value;
				    var transitionedHours = (transitionState["transitionedHours"] as FloatArrayAttribute).value;

                    var len = result.Collectible.TransitionableProps.Length;
                    float[] newFreshHours = new float[len];
                    float[] newTransitionHours = new float[len];
                    float[] newTransitionedHours = new float[len];

                    var api = slot.Inventory.Api;
                    for(int i = 0; i < len; i++)
                    {
                        var newProp = result.Collectible.TransitionableProps[i];
                        var existingIndex = collectible.TransitionableProps.IndexOf(existingProp => existingProp.Type == newProp.Type);
                        if(existingIndex == -1)
                        {
                            newFreshHours[i] = newProp.FreshHours.nextFloat(1, api.World.Rand);
                            newTransitionHours[i] = newProp.TransitionHours.nextFloat(1, api.World.Rand);
                            continue;
                        }

                        newFreshHours[i] = freshHours[existingIndex];
                        newTransitionHours[i] = transitionHours[existingIndex];
                        newTransitionedHours[i] = transitionedHours[existingIndex];
                    }

                    transitionState["freshHours"] = new FloatArrayAttribute(newFreshHours);
                    transitionState["transitionHours"] = new FloatArrayAttribute(newTransitionHours);
                    transitionState["transitionedHours"] = new FloatArrayAttribute(newTransitionedHours);

                    result.Attributes["transitionstate"] = transitionState;
                }

                slot.Itemstack = result;
                if (slot is not DummySlot && slot.CanTake() && slot.Inventory?.Pos != null)
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
