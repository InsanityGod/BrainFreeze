using BrainFreeze.Code.Behaviors;
using BrainFreeze.Code.Transition;
using CustomTransitionLib;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace BrainFreeze.Code.HarmonyPatches.DynamicRegistry
{
    [HarmonyPatch(typeof(RegistryObjectType), "CreateBasetype")]
    public static class DynamicFrozenVariant
    {
        public const string LoggingKey = "BrainFreezeAutoRegistry";

        public static HashSet<string> AllowedCodes { get; internal set; }

        //TODO should probably reset this list during cleanup in case other mods modify this but don't reset it

        public static void Postfix(RegistryObjectType __instance)
        {
            if (BrainFreezeModSystem.Config.SuperBrainFreeze || !AllowedCodes.Contains(__instance.Code.Path.Split('-')[0])) return;
            __instance.VariantGroups ??= Array.Empty<RegistryObjectVariantGroup>();

            if (__instance.VariantGroups.Length == 1)
            {
                var variant = __instance.VariantGroups[0];
                __instance.VariantGroups = __instance.VariantGroups.Prepend(new RegistryObjectVariantGroup
                {
                    Code = variant.Code,
                    Combine = EnumCombination.Add, //TODO
                    LoadFromProperties = variant.LoadFromProperties,
                    States = variant.States,
                    IsValue = variant.IsValue,
                    LoadFromPropertiesCombine = variant.LoadFromPropertiesCombine,
                    OnVariant = variant.OnVariant,
                }).ToArray();
            }
            else if (__instance.VariantGroups.Length > 1)
            {
                //TODO this is too complex for now so ignore it
                return;
            }

            var newVariant = new RegistryObjectVariantGroup
            {
                Code = "brainfreeze",
                States = new string[] { "frozen" },
                Combine = __instance.VariantGroups.Length == 0 ? EnumCombination.Add : EnumCombination.Multiply,
            };
            __instance.VariantGroups = __instance.VariantGroups.Append(newVariant);
            if (__instance.SkipVariants != null)
            {
                var skippedFrozen = __instance.SkipVariants.Select(variant => new AssetLocation($"{variant}-frozen")).ToArray();
                __instance.SkipVariants = __instance.SkipVariants.Append(skippedFrozen).ToArray();
            }
        }

        public static void FinalizeFrozenCollectible(ICoreAPI api, Item frozenItem)
        {
            frozenItem.MatterState = EnumMatterState.Solid;
            frozenItem.CollectibleBehaviors ??= Array.Empty<CollectibleBehavior>();

            var frozenPrefix = new FrozenNamePrefix(frozenItem);
            frozenItem.CollectibleBehaviors = frozenItem.CollectibleBehaviors.Append(frozenPrefix);
            frozenPrefix.OnLoaded(api);

            var nonFrozenItem = api.World.GetItem(new AssetLocation(frozenItem.CodeWithoutFrozenPart()));
            if (nonFrozenItem == null)
            {
                api.Logger.Warning("Could not find {0} for auto frozen variant registry (BrainFreeze)", frozenItem.CodeWithoutFrozenPart());
                return;
            }
            nonFrozenItem.TransitionableProps ??= Array.Empty<TransitionableProperties>();
            frozenItem.TransitionableProps ??= Array.Empty<TransitionableProperties>();

            var itemStack = new JsonItemStack
            {
                Code = frozenItem.Code,
                Type = EnumItemClass.Item
            };
            itemStack.Resolve(api.World, LoggingKey);

            nonFrozenItem.TransitionableProps = nonFrozenItem.TransitionableProps.Prepend(new TransitionableProperties
            {
                Type = EnumBrainFreezeTransitionType.Freeze.ConvertToFake(),
                FreshHours = NatFloat.Zero,
                TransitionHours = new NatFloat(16, 0, EnumDistribution.UNIFORM),
                TransitionRatio = 1,
                TransitionedStack = itemStack,
            }).ToArray();

            itemStack = new JsonItemStack
            {
                Code = nonFrozenItem.Code,
                Type = EnumItemClass.Item
            };
            itemStack.Resolve(api.World, LoggingKey);

            frozenItem.TransitionableProps = frozenItem.TransitionableProps.Prepend(new TransitionableProperties
            {
                Type = EnumBrainFreezeTransitionType.Thaw.ConvertToFake(),
                FreshHours = NatFloat.Zero,
                TransitionHours = new NatFloat(16, 0, EnumDistribution.UNIFORM),
                TransitionRatio = 1,
                TransitionedStack = itemStack,
            }).ToArray();

            var inContainerProps = (JContainer)frozenItem.Attributes["waterTightContainerProps"].Token;
            inContainerProps ??= (JContainer)nonFrozenItem.Attributes["waterTightContainerProps"].Token?.DeepClone();

            if(inContainerProps != null)
            {
                inContainerProps["AllowSpill"] = false;
            }
            else api.Logger.Warning("waterTightContainerProps where not defined for {0}, is this really a liquid?? (BrainFreeze)", frozenItem.CodeWithoutFrozenPart());

            //TODO improve this to be more fool proof
            var firstTexture = frozenItem.FirstTexture;
            if (firstTexture != null)
            {
                if (firstTexture.Base.ToString() == "game:block/liquid/waterportion")
                {
                    firstTexture.Base = new AssetLocation("game:block/liquid/ice/lake1");
                    inContainerProps["texture"]["base"] = "game:block/liquid/ice/lake1";
                }
                else
                {
                    firstTexture.BlendedOverlays = new BlendedOverlayTexture[]
                    {
                        new()
                        {
                            Base = new AssetLocation("game:block/liquid/ice/lake1")
                        }
                    };
                    inContainerProps["texture"] = JToken.FromObject(firstTexture);
                }
            }
            //collectibleObject.Textures

            //TODO

            if(frozenItem.CreativeInventoryStacks != null)
            {
                foreach (var content in frozenItem.CreativeInventoryStacks.SelectMany(inf => inf.Stacks)
                    .Where(stack => stack.Attributes != null)
                    .SelectMany(stack => stack.Attributes["ucontents"].AsArray()))
                {
                    //Fix code reference to frozen variant
                    content.Token["code"] = content["code"].AsString().Replace(nonFrozenItem.Code.Path, frozenItem.Code.Path);
                }

                foreach (var item in frozenItem.CreativeInventoryStacks.SelectMany(inf => inf.Stacks))
                {
                    //resolve fixed variant
                    item.Resolve(api.World, LoggingKey);
                }
            }

            //Fix transitions
            if (frozenItem.TransitionableProps != null)
            {
                foreach (var transition in frozenItem.TransitionableProps.Where(trans => trans.Type != EnumBrainFreezeTransitionType.Thaw.ConvertToFake()))
                {
                    var code = transition.TransitionedStack?.Code?.ToString();
                    if (code != null)
                    {
                        var frozenAssetLocation = new AssetLocation($"{code}-frozen");
                        if (api.World.GetItem(frozenAssetLocation) != null)
                        {
                            transition.TransitionedStack.Code = frozenAssetLocation;
                            transition.TransitionedStack.Resolve(api.World, LoggingKey);
                        }
                    }
                }
            }
        }

        public static void FinalizeIceCube(ICoreAPI api)
        {
            var iceCube = api.World.GetItem(new AssetLocation("brainfreeze:icecubes"));
            
            var creativeStacks = new List<CreativeTabAndStackList>();
            foreach(var item in api.World.Items.Where(item => item.Variant != null && item.Variant["brainfreeze"] != null))
            {
                var ingredient = new ItemStack(item);
                var tree = new TreeAttribute();
                tree.SetItemstack("IceCubeIngredient", ingredient);

                var attr = new JsonObject(new JObject());
                attr.Token["CreativeIngredientId"] = JToken.FromObject(item.Id);
                
                var stack = new CreativeTabAndStackList
                {
                    Tabs = new string[] { "general", "items" },
                    Stacks = new JsonItemStack[]
                    {
                        new()
                        {
                            Code = iceCube.Code,
                            Type = EnumItemClass.Item,
                            Attributes = attr
                        }
                    }
                };

                foreach(var toResolve in stack.Stacks)
                {
                    toResolve.Resolve(api.World, LoggingKey);
                }

                creativeStacks.Add(stack);
            }
            iceCube.CreativeInventoryStacks = creativeStacks.ToArray();
        }
    }
}