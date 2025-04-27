using InsanityLib.Handlers;
using Newtonsoft.Json.Linq;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.Transition
{
    public class TemperatureTransitionHandler : TransitionHandler
    {
        public bool Invert { get; private set; }
        
        public override void LoadAttributes(JsonObject attributes)
        {
            if(attributes == null) return;
            Invert = attributes["Invert"].AsBool(Invert);
        }

        public const float DefaultFreezePoint = 0;
        public static float GetFreezingPoint(ItemSlot slot)
        {
            var attr = slot.Itemstack?.Collectible.Attributes;
            return attr != null ? attr["freezePoint"].AsFloat() : DefaultFreezePoint;
        }

        public override float GetTransitionRateMul(IWorldAccessor world, ItemSlot inSlot, float currentResult)
        {
            float multiplier = 0;

            var pos = inSlot.Inventory?.Pos ?? (inSlot.Inventory as InventoryBasePlayer)?.Player.Entity.Pos.AsBlockPos;
            if(pos == null) return multiplier;
            if (!world.BlockAccessor.IsValidPos(pos)) return multiplier; //TODO maybe add logging

            var freezingPoint = GetFreezingPoint(inSlot); // 0
            var temperature = GetTemperatureAtPos(world, inSlot, pos); //33
            var diffMult = Math.Abs(temperature - freezingPoint) / 5; // 33 /5 = 6.5

            // 33 => 0 ? 6.5 : -6.5
            if (Invert) return temperature >= freezingPoint ? diffMult : -diffMult;
            // 33 <= 0 ? 6.5 : -6.5
            return temperature <= freezingPoint ? diffMult : -diffMult;
        }

        //inSlot is just here to make hooking into this with harmony patches is easier (in case you want to make special inventory slots with heating properties)
        public static float GetTemperatureAtPos(IWorldAccessor world, ItemSlot inSlot, BlockPos pos)
        {
            var entityAtPos = world.BlockAccessor.GetBlockEntity(pos);
            if(entityAtPos is IFirePit firePit && firePit.IsBurning) return 160;

            var climate = world.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.NowValues);
            if(climate.Temperature < 10)
            {
                var room = world.Api.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(pos);
            
                if(room.ExitCount == 0) return 10; //TODO compat for a mod I intend to make in the future
            }
            
            return climate.Temperature;
        }

        public override void PostOnTransitionNow(CollectibleObject collectible, ItemSlot slot, TransitionableProperties props,  ref ItemStack result)
        {
            //TODO: I wish there was a better way to do this...
            if (result.Collectible?.MatterState == EnumMatterState.Liquid)
            {
                var wasLocked = slot.Inventory?.TakeLocked;
                if (slot.Inventory != null)
                {
                    slot.Inventory.TakeLocked = false;
                }

                //Some code to preserve transition states //TODO see if we can do this in a cleaner way
                if (result.Collectible.TransitionableProps != null && slot.Inventory.Api != null)
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
                    for (int i = 0; i < len; i++)
                    {
                        var newProp = result.Collectible.TransitionableProps[i];
                        var existingIndex = collectible.TransitionableProps.IndexOf(existingProp => existingProp.Type == newProp.Type);
                        if (existingIndex == -1)
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

                HandleLiquidTransitionResult(slot, ref result);

                if (slot.Inventory != null)
                {
                    slot.Inventory.TakeLocked = wasLocked.Value;
                }
            }

            if(slot.Inventory?.Pos != null)
            {
                //HACK: workaround for GroundStoreAble not updating correctly for some reason
                slot.Inventory.Api?.World.BlockAccessor.GetBlockEntity(slot.Inventory.Pos)?.MarkDirty();
            }
        }

        public static void HandleLiquidTransitionResult(ItemSlot slot, ref ItemStack result)
        {
            slot.Itemstack = result;
            if(result.Collectible.MatterState != EnumMatterState.Liquid) return;
            var pos = slot.Inventory is InventoryBasePlayer playerInv ? playerInv.Player.Entity.Pos.AsBlockPos : slot.Inventory?.Pos;
            if (slot is not DummySlot && slot.CanTake() && pos != null)
            {
                if (slot.Inventory.Api.World.BlockAccessor.GetBlock(pos) is BlockLiquidContainerBase liquidContainer)
                {
                    liquidContainer.SetContent(pos, result);
                }
                else
                {
                    slot.Inventory.Api.World.PlaySoundAt(new AssetLocation("sounds/environment/smallsplash"), pos.X, pos.Y, pos.Z);
                    result.StackSize = 0;
                }
                if (slot.Inventory is InventoryBasePlayer)
                {
                    //Edge case handling for ice cubes
                    result.StackSize = 0;
                }
            }
        }
    }
}