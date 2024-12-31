using BrainFreeze.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BrainFreeze.Behaviors
{
    public class IceBreakerTool : CollectibleBehavior
    {
        public IceBreakerTool(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            var world = byEntity.Api.World;
            if(byEntity.Api.Side == EnumAppSide.Server && blockSel.Block == null)
            {
                //Block is somehow not auto filled on server :P
                blockSel.Block = world.BlockAccessor.GetBlock(blockSel.Position);
            }

            if(blockSel?.Block == null || byEntity is not EntityPlayer playerEntity) return;

            var offhandCollectible = byEntity.LeftHandItemSlot?.Itemstack?.Collectible;

            if(offhandCollectible == null || !offhandCollectible.Code.ToString().StartsWith("game:hammer"))
            {
                if(byEntity.Api is ICoreClientAPI clientApi)
                {
                    clientApi.TriggerIngameError(this, "nohammer", Lang.Get("Requires a hammer in the off hand"));
                }
                return;
            }
            //TODO: maybe allow for this interaction on itemslot as well
            //TODO: maybe add a custom animation
            //TODO: maybe add a custom sound effect for ice breaking
            if(blockSel.Block is BlockLiquidContainerBase liquidContainer)
            {
                var entityContainer = world.BlockAccessor.GetBlockEntity<BlockEntityContainer>(blockSel.Position);
                if(entityContainer == null) return;

                var content = liquidContainer.GetContent(blockSel.Position);
                if(content == null) return;
                if (content?.Collectible?.Variant["brainfreeze"] == null) return;
                handHandling = EnumHandHandling.PreventDefault;
                handling = EnumHandling.PreventDefault;

                var iceCubeCount = (int)liquidContainer.GetCurrentLitres(blockSel.Position);

                var iceCubeItem = world.GetItem(new AssetLocation("brainfreeze:icecubes"));

                slot.Itemstack.Collectible.DamageItem(world, playerEntity, slot, iceCubeCount / 5);

                while(iceCubeCount > 0)
                {
                    var stackSize = Math.Min(iceCubeCount, iceCubeItem.MaxStackSize);

                    var stack = new ItemStack(iceCubeItem, stackSize);
                    (iceCubeItem as IceCube).SetContent(stack, content);

                    playerEntity.TryGiveItemStack(stack);
                    iceCubeCount -= stackSize;
                }
                
                content.StackSize = 0;

                liquidContainer.SetContent(blockSel.Position, content);

                var liquidSlot = entityContainer.Inventory[liquidContainer.GetContainerSlotId(blockSel.Position)];
                liquidSlot.Itemstack = null;
                liquidSlot.MarkDirty();
                entityContainer.MarkDirty(true);
                if(world.Api is ICoreClientAPI)
                {
                    var lakeIce = world.GetBlock(new AssetLocation("game:lakeice"));
                    world.PlaySoundAt(lakeIce?.Sounds?.GetBreakSound(playerEntity.Player), entityContainer.Pos.X, entityContainer.Pos.Y, entityContainer.Pos.Z);
                }
            }
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            return new WorldInteraction[] 
            {
                new() {
                    ActionLangCode = "brainfreeze:break-ice",
                    MouseButton = EnumMouseButton.Right
                }
            };
        }
    }
}
