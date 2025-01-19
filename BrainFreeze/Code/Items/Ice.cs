using BrainFreeze.Code.Rendering;
using BrainFreeze.Code.Transition;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class Ice : Item
    {
        public float LitersPerItem { get; set; } = 1;
        
        private string cacheKey = "DefaultCache";
        
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            cacheKey = $"{Code}-meshref";
            if(Attributes?.Exists == true)
            {
                LitersPerItem = Attributes[nameof(LitersPerItem)].AsFloat(LitersPerItem);
            }
        }

        public int? GetContentId(ItemStack ice)
        {
            var content = ice.Attributes?.GetItemstack("IceIngredient");

            return content?.Id;
        }

        public ItemStack GetContent(ItemStack ice, IWorldAccessor world = null)
        {
            var content = ice.Attributes?.GetItemstack("IceIngredient");
            if(content == null)
            {
                var creativeId = ice.Attributes?.TryGetInt("CreativeIngredientId");
                if(creativeId != null && world != null)
                {
                    var item = world.GetItem(creativeId.Value);
                    if(item != null)
                    {
                        content = new ItemStack(item);
                        SetContent(ice, content);
                    }
                }
            }

            if (content != null)
            {
                if (content.Collectible == null && world != null) content.ResolveBlockOrItem(world);
                if (content?.Collectible != null)
                {
                    var containableProps = content.Collectible.Attributes["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
                    var itemsPerLiter = containableProps?.ItemsPerLitre ?? 100;
                    content.StackSize = (int)(ice.StackSize * LitersPerItem * itemsPerLiter);
                }
            }
            return content;
        }

        public void SetContent(ItemStack ice, ItemStack ingredient)
        {
            ice.Attributes ??= new TreeAttribute();
            var input = ingredient.Clone();
            ice.Attributes.SetItemstack("IceIngredient", input);
        }

        #region NutritionAndHydration

        public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
        {
            if(itemstack == null) return null;
            var ingredient = GetContent(itemstack, world);

            if (ingredient == null) return null;

            WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(ingredient);
            if (props?.NutritionPropsPerLitre != null)
            {
                FoodNutritionProperties nutriProps = props.NutritionPropsPerLitre.Clone();
                float litre = 1;
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

            //TODO look at name generation
            if (comp.Length > 1) return $"{baseName} ({string.Join(' ', comp.Skip(1))})";
            return $"{ingredientName} {baseName}";
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            var ingredient = GetContent(inSlot.Itemstack, world);
            if(ingredient.Collectible.Code != null)
            {
                var code = $"{Code.Domain}:{Code.Path}-{ingredient.Collectible.Code.Path}";
                var result = Lang.GetMatching(code);
                if(result != code)
                {
                    dsc.AppendLine(result);
                }
            }
            dsc.AppendLine();

            var name = ingredient?.Collectible?.GetHeldItemName(ingredient)?.ToLower();
            var langCode = $"{Code.Domain}:{Code.Path}-dynamicdesc";
            var str = Lang.Get(langCode, name ?? "frozen unknown liquid");
            if (str != langCode) dsc.AppendLine(str);
        }

        #endregion DisplayText

        #region TransitionStates

        public override bool RequiresTransitionableTicking(IWorldAccessor world, ItemStack itemstack)
        {
            var content = GetContent(itemstack, world);
            if (content == null) return false;

            //TODO soup cubes? when?
            return content.Collectible.RequiresTransitionableTicking(world, content);
        }

        public override TransitionableProperties[] GetTransitionableProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
        {
            if (itemstack.Id != Id)
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
            if (stack.Id != Id)
            {
                //TODO check
                //In case it's called with current collectible instead of contained item
                stack.Collectible.SetTransitionState(stack, type, transitionedHours);
                return;
            }

            var content = GetContent(stack);
            if (content?.Collectible == null) return; //We may have an issue here but lets just hope this doesn't somehow happen

            content.Collectible.SetTransitionState(content, type, transitionedHours);
            SetContent(stack, content);
        }

        public override float GetTransitionRateMul(IWorldAccessor world, ItemSlot inSlot, EnumTransitionType transType)
        {
            var itemstack = inSlot.Itemstack;
            if (itemstack.Id != Id)
            {
                //In case it's called with current collectible instead of contained item
                return itemstack.Collectible.GetTransitionRateMul(world, inSlot, transType);
            }

            var content = GetContent(itemstack, world);
            if (content == null) return 1f;

            var cache = inSlot.Itemstack;
            inSlot.Itemstack = content;
            var result = content.Collectible.GetTransitionRateMul(world, inSlot, transType);
            inSlot.Itemstack = cache;

            return result;
        }

        public override ItemStack OnTransitionNow(ItemSlot slot, TransitionableProperties props)
        {
            var itemstack = slot.Itemstack;
            ItemStack result;
            if (itemstack.Id != Id)
            {
                //TODO check
                //In case it's called with current collectible instead of contained item
                result = itemstack.Collectible.OnTransitionNow(slot, props);
            }
            else
            {
                var content = GetContent(slot.Itemstack);
                result = content.Collectible.OnTransitionNow(slot, props);
            }

            BrainFreezeTransitionHandler.HandleLiquidTransitionResult(slot, ref result);

            return result;
        }

        public override TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
        {

            if (inslot is ItemSlotCreative) return base.UpdateAndGetTransitionStates(world, inslot);
            var content = GetContent(inslot.Itemstack, world);
            if (content == null) return base.UpdateAndGetTransitionStates(world, inslot);
            var currentStack = inslot.Itemstack;

            inslot.Itemstack = content;
            var result = base.UpdateAndGetTransitionStates(world, inslot);

            if (inslot.Itemstack == null || inslot.Itemstack.StackSize == 0 || inslot.Itemstack.Collectible.Variant["brainfreeze"] == null)
            {
                //transition has deleted our content :P
                return result;
            }

            inslot.Itemstack = currentStack;
            inslot.Itemstack.Attributes.SetItemstack("IceIngredient", content);

            return result;
        }

        #endregion TransitionStates

        #region CustomRendering

        private Dictionary<int, MultiTextureMeshRef> Meshrefs => ObjectCacheUtil.GetOrCreate(api, cacheKey, () => new Dictionary<int, MultiTextureMeshRef>());

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            var meshRefId = itemstack.TempAttributes.GetAsInt("meshRefId");
            if(meshRefId == 0)
            {
                var ingredient = GetContent(itemstack, capi.World);
                if (ingredient != null)
                {
                    if(!Meshrefs.TryGetValue(ingredient.Id, out renderinfo.ModelRef))
                    {
                        MultiTextureMeshRef modelref = capi.Render.UploadMultiTextureMesh(GenMesh(ingredient.Item, capi.ItemTextureAtlas));
                        renderinfo.ModelRef = Meshrefs[ingredient.Id] = modelref;
                    }
                    itemstack.TempAttributes.SetInt("meshRefId", ingredient.Id);
                }
            }
            else Meshrefs.TryGetValue(meshRefId, out renderinfo.ModelRef);
            
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        }

        public MeshData GenMesh(Item ingredient, ITextureAtlasAPI targetAtlas)
        {
            var manager = targetAtlas as ItemTextureAtlasManager;
            var capi = api as ICoreClientAPI;
            
            capi.Tesselator.TesselateItem(this, out var mesh, new IceTexPositionSource(manager, ingredient));
            return mesh;
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            foreach(var mesh in Meshrefs.Values)
            {
                mesh.Dispose();
            }
            ObjectCacheUtil.Delete(api, cacheKey);
            base.OnUnloaded(api);
        }

        #endregion CustomRendering
    }
}