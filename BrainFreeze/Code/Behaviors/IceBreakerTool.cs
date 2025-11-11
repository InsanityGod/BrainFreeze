using BrainFreeze.Code.Items;
using InsanityLib.Util;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.Behaviors;

public class IceBreakerTool(CollectibleObject collObj) : CollectibleBehavior(collObj)
{
    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
    {
        if(blockSel == null || blockSel.Position is null || byEntity is not EntityPlayer playerEntity) return;

        var world = byEntity.Api.World;
        var block = blockSel.GetOrFindBlock(world);
        if (block is null) return;

        //TODO: maybe allow for this interaction on itemslot as well
        //TODO: maybe add a custom animation
        //TODO: maybe add a custom sound effect for ice breaking
        if (block is BlockLiquidContainerBase liquidContainer)
        {
            var entityContainer = world.BlockAccessor.GetBlockEntity<BlockEntityContainer>(blockSel.Position);
            if(!liquidContainer.IsTopOpened && entityContainer is not BlockEntityBarrel { Sealed: false }) return;

            var content = liquidContainer.GetContent(blockSel.Position);
            if (content?.Collectible?.Variant["brainfreeze"] is null || !EnsureHammerEquiped(playerEntity)) return;
            
            handHandling = EnumHandHandling.PreventDefault;
            handling = EnumHandling.PreventDefault;

            if(!ExtractIceCubesFromStack(content, byEntity, slot)) return;
            
            var liquidSlot = entityContainer.Inventory[liquidContainer.GetContainerSlotId(blockSel.Position)];
            liquidSlot.Itemstack = null;
            liquidSlot.MarkDirty();
            entityContainer.MarkDirty(true);
        }
        else if(block is BlockGroundStorage)
        {
            var groundStorage = world.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(blockSel.Position);
            if (groundStorage is null) return;

            var groundStorageSlot = groundStorage.GetSlotAt(blockSel);
            if (groundStorageSlot.Itemstack?.Collectible is not BlockLiquidContainerBase liquidContainer2 || !liquidContainer2.Code.Path.StartsWith("bowl")) return;

            var content = liquidContainer2.GetContent(groundStorageSlot.Itemstack);
            if (content?.Collectible?.Variant["brainfreeze"] is null || !EnsureHammerEquiped(playerEntity)) return;

            handHandling = EnumHandHandling.PreventDefault;
            handling = EnumHandling.PreventDefault;

            if(!ExtractIceCubesFromStack(content, byEntity, slot)) return;
            
            liquidContainer2.SetContent(groundStorageSlot.Itemstack, null);
            groundStorageSlot.MarkDirty();
            groundStorage.MarkDirty();
        }
    }

    public bool ExtractIceCubesFromStack(ItemStack contentStack, EntityAgent entity, ItemSlot toolSlot)
    {
        if (contentStack?.Collectible?.Variant["brainfreeze"] is null || !EnsureHammerEquiped(entity)) return false;
        var world = entity.Api.World;
        var iceCubeItem = world.GetItem(new AssetLocation("brainfreeze", "icecubes")) as Ice;

        var iceCubeCount = (int)(GetLiters(contentStack) / iceCubeItem.LitersPerItem);

        toolSlot.Itemstack.Collectible.DamageItem(world, entity, toolSlot, iceCubeCount / 5);

        while (iceCubeCount > 0)
        {
            var stackSize = Math.Min(iceCubeCount, iceCubeItem.MaxStackSize);

            var stack = new ItemStack(iceCubeItem, stackSize);
            iceCubeItem.SetContent(stack, contentStack);

            if (!entity.TryGiveItemStack(stack)) world.SpawnItemEntity(stack, entity.Pos.AsBlockPos);

            iceCubeCount -= stackSize;
        }

        var lakeIce = world.GetBlock(new AssetLocation("game", "lakeice"));
        world.PlaySoundAt(lakeIce?.Sounds?.GetBreakSound(EnumTool.Chisel), entity, (entity as EntityPlayer)?.Player);
        return true;
    }

    protected static float GetLiters(ItemStack stack)
	{
		WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(stack);
		if (props == null) return 0f;

		return (float)stack.StackSize / props.ItemsPerLitre;
	}

    public bool EnsureHammerEquiped(EntityAgent entity)
    {
        if(entity.LeftHandItemSlot?.Itemstack?.Collectible.Tool != EnumTool.Hammer)
        {
            if (entity.Api is ICoreClientAPI clientApi)
            {
                clientApi.TriggerIngameError(this, "nohammer", Lang.Get("Requires a hammer in the off hand"));
            }
            return false;
        }
        else return true;
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling) => [
        new WorldInteraction {
            ActionLangCode = "brainfreeze:break-ice",
            MouseButton = EnumMouseButton.Right,
            HotKeyCode = "shift"
        }
    ];
}