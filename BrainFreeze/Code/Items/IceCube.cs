using BrainFreeze.Code.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.Items
{
    public class IceCube : Item
    {
        private int iceCubeId = 0;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            iceCubeId = api.World.GetItem(new AssetLocation("brainfreeze:icecubes")).Id;
        }

        public ItemStack GetContent(ItemStack iceCube, IWorldAccessor world = null)
        {
            var content = iceCube.Attributes?.GetItemstack("IceCubeIngredient");
            if(content == null)
            {
                var creativeId = iceCube.Attributes?.TryGetInt("CreativeIngredientId");
                if(creativeId != null && world != null)
                {
                    var item = world.GetItem(creativeId.Value);
                    if(item != null)
                    {
                        content = new ItemStack(item);
                        SetContent(iceCube, content);
                    }
                }
            }
            if (content != null)
            {
                if (content.Collectible == null && world != null) content.ResolveBlockOrItem(world);
                if (content?.Collectible != null)
                {
                    var containableProps = content.Collectible.Attributes["waterTightContainerProps"].AsObject<WaterTightContainableProps>();

                    content.StackSize = (int)(containableProps?.ItemsPerLitre ?? 100) * iceCube.StackSize;
                }
            }
            return content;
        }

        public void SetContent(ItemStack iceCube, ItemStack ingredient)
        {
            iceCube.Attributes ??= new TreeAttribute();
            var input = ingredient.Clone(); //TODO is cloning really necesary here?
            iceCube.Attributes.SetItemstack("IceCubeIngredient", input);
        }

        #region NutritionAndHydration

        public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
        {
            var ingredient = GetContent(itemstack, world);

            if (ingredient == null) return null;
            //TODO add coldness effect

            WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(ingredient);
            if (props?.NutritionPropsPerLitre != null)
            {
                FoodNutritionProperties nutriProps = props.NutritionPropsPerLitre.Clone();
                float litre = 1; //TODO maybe have a magic number class or config for this
                nutriProps.Health *= litre;
                nutriProps.Satiety *= litre;
                return nutriProps;
            }
            return base.GetNutritionProperties(world, itemstack, forEntity);
        }

        #endregion

        #region DisplayText

        public override string GetHeldItemName(ItemStack itemStack)
        {
            var baseName = base.GetHeldItemName(itemStack);

            var ingredient = GetContent(itemStack);
            if (ingredient?.Collectible == null) return baseName;

            var ingredientName = ingredient.Collectible.GetHeldItemName(ingredient);
            var comp = ingredientName.Split(' ');

            if (comp.Length > 1) return $"{baseName} ({comp[1]})";
            return $"{ingredientName} {baseName}";
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            var ingredient = GetContent(inSlot.Itemstack, world);
            var name = ingredient?.Collectible?.GetHeldItemName(ingredient)?.ToLower();
            dsc.AppendLine(Lang.Get("brainfreeze:icecubes-dynamicdesc", name ?? "frozen unknown liquid"));
        }

        #endregion DisplayText

        #region TransitionStates

        public override bool RequiresTransitionableTicking(IWorldAccessor world, ItemStack itemstack)
        {
            var content = GetContent(itemstack, world);
            if (content == null) return false;
            //TODO look into switching transition result for frozen variants (so distilled ice cubes don't turn into water ice cubes lol)
            //TODO Check name of normal ice cubes
            //TODO fix creative inventory :p
            //TODO soup cubes? when?
            return content.Collectible.RequiresTransitionableTicking(world, content);
        }

        public override void TryMergeStacks(ItemStackMergeOperation op)
        {
            base.TryMergeStacks(op);
        }

        public override TransitionableProperties[] GetTransitionableProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
        {
            if (itemstack.Id != iceCubeId)
            {
                //In case it's called with current collectible instead of contained item
                return itemstack.Collectible.GetTransitionableProperties(world, itemstack, forEntity);
            }

            var content = GetContent(itemstack, world);
            if (content == null) return Array.Empty<TransitionableProperties>();

            return content.Collectible.GetTransitionableProperties(world, content, forEntity);
        }

        public override void SetTransitionState(ItemStack stack, EnumTransitionType type, float transitionedHours)
        {
            if (stack.Id != iceCubeId)
            {
                //In case it's called with current collectible instead of contained item
                stack.Collectible.SetTransitionState(stack, type, transitionedHours);
                return;
            }

            var content = GetContent(stack);
            if (content?.Collectible == null) return; //TODO we may have an issue here but lets just hope this doesn't somehow happen

            content.Collectible.SetTransitionState(stack, type, transitionedHours);
        }

        public override float GetTransitionRateMul(IWorldAccessor world, ItemSlot inSlot, EnumTransitionType transType)
        {
            var itemstack = inSlot.Itemstack;
            if (itemstack.Id != iceCubeId)
            {
                //In case it's called with current collectible instead of contained item
                return itemstack.Collectible.GetTransitionRateMul(world, inSlot, transType);
            }

            var content = GetContent(itemstack, world);
            if (content == null) return 1f;

            return content.Collectible.GetTransitionRateMul(world, inSlot, transType);
        }

        public override ItemStack OnTransitionNow(ItemSlot slot, TransitionableProperties props)
        {
            var itemstack = slot.Itemstack;
            if (itemstack.Id != iceCubeId)
            {
                //In case it's called with current collectible instead of contained item
                return itemstack.Collectible.OnTransitionNow(slot, props);
            }

            var content = GetContent(slot.Itemstack);
            return content.Collectible.OnTransitionNow(slot, props);
        }

        public override TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
        {
            var content = GetContent(inslot.Itemstack, world);
            if (inslot is ItemSlotCreative || content == null)
            {
                return base.UpdateAndGetTransitionStates(world, inslot);
            }

            var currentStack = inslot.Itemstack;

            inslot.Itemstack = content;
            var result = base.UpdateAndGetTransitionStates(world, inslot);

            if (inslot.Itemstack == null || inslot.Itemstack.StackSize == 0 || inslot.Itemstack.Collectible.Variant["brainfreeze"] == null)
            {
                //transition has deleted our content :P
                return result;
            }
            //TODO check transition

            inslot.Itemstack = currentStack;
            inslot.Itemstack.Attributes.SetItemstack("IceCubeIngredient", content);

            return result;
        }

        #endregion TransitionStates

        #region CustomRendering

        private Dictionary<int, MultiTextureMeshRef> Meshrefs => ObjectCacheUtil.GetOrCreate(api, "icecubemeshrefs", () => new Dictionary<int, MultiTextureMeshRef>());

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            var ingredient = GetContent(itemstack, capi.World);
            if (ingredient != null && !Meshrefs.TryGetValue(ingredient.Id, out renderinfo.ModelRef))
            {
                MultiTextureMeshRef modelref = capi.Render.UploadMultiTextureMesh(GenMesh(ingredient.Item, capi.ItemTextureAtlas));
                renderinfo.ModelRef = Meshrefs[ingredient.Id] = modelref;
            }
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        }

        public MeshData GenMesh(Item ingredient, ITextureAtlasAPI targetAtlas)
        {
            var manager = targetAtlas as ItemTextureAtlasManager;
            var capi = api as ICoreClientAPI;
            capi.Tesselator.TesselateItem(this, out var mesh, new IceCubeTexPositionSource(manager, ingredient));
            return mesh;
        }

        #endregion CustomRendering
    }
}