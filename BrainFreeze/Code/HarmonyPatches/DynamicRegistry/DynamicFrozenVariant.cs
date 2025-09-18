using BrainFreeze.Code.Behaviors;
using BrainFreeze.Config;
using HarmonyLib;
using InsanityLib.Util.ContentFeatures;
using InsanityLib.Util.SpanUtil;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace BrainFreeze.Code.HarmonyPatches.DynamicRegistry;

//Just pretend you didn't see this class
[HarmonyPatch(typeof(RegistryObjectType))]
public static class DynamicFrozenVariant //TODO cleanup
{
    public const string LoggingKey = "BrainFreezeAutoRegistry";

    [HarmonyPatch("CreateBasetype")]
    [HarmonyPostfix]
    public static void CreateBasetypePostfix(ICoreAPI api, RegistryObjectType __instance)
    {
        if(!BrainFreezeConfig.Instance.AutoRegFrozenVariants.ContainsKey(__instance.Code.Path.AsSpan().FirstCodePartAsSpan().ToString())) return;
        __instance.VariantGroups ??= [];

        if (__instance.VariantGroups.Length == 1)
        {
            var variant = __instance.VariantGroups[0];
            __instance.VariantGroups = [.. __instance.VariantGroups.Prepend(new RegistryObjectVariantGroup
            {
                Code = variant.Code,
                Combine = EnumCombination.Add,
                LoadFromProperties = variant.LoadFromProperties,
                States = variant.States,
                IsValue = variant.IsValue,
                LoadFromPropertiesCombine = variant.LoadFromPropertiesCombine,
                OnVariant = variant.OnVariant,
            })];
        }
        else if (__instance.VariantGroups.Length > 1)
        {
            //TODO this is too complex for now so ignore it
            //TODO create AdditiveMultiply combination move
            return;
        }

        var newVariant = new RegistryObjectVariantGroup
        {
            Code = "brainfreeze",
            States = ["brainfreeze"],
            Combine = __instance.VariantGroups.Length == 0 ? EnumCombination.Add : EnumCombination.Multiply,
        };

        __instance.VariantGroups = [..__instance.VariantGroups, newVariant];
        if (__instance.SkipVariants is not null) __instance.SkipVariants = [.. __instance.SkipVariants, ..__instance.SkipVariants.Select(variant => new AssetLocation(variant.Domain, $"{variant.Path}-brainfreeze"))];
    }

    [HarmonyPatch("solveByType")]
    [HarmonyPrefix]
    public static void SolveByTypePrefix(ref string codePath)
    {
        if (codePath.EndsWith("-brainfreeze"))
        {
            codePath = codePath[..^12]; //12 is the length of "-brainfreeze"
        }
    }

    public static bool TryFinalizeFrozenCollectible(ICoreAPI api, Item frozenItem)
    {
        try
        {
            return FinalizeFrozenCollectible(api, frozenItem);
        }
        catch(Exception ex)
        {
            api.Logger.Error("[brainfreeze] [{0}] Error during finalizing of frozen variant '{1}': {2}", frozenItem.Code.Domain, frozenItem.Code.Path, ex);
            return false;
        }
    }

    private static bool FinalizeFrozenCollectible(ICoreAPI api, Item frozenItem)
    {
        //frozenItem.MatterState = EnumMatterState.Solid; //Technically it should be solid but this causes issues for item moving
        frozenItem.CollectibleBehaviors ??= [];

        var frozenPrefix = new FrozenNamePrefix(frozenItem);
        frozenItem.CollectibleBehaviors = frozenItem.CollectibleBehaviors.Append(frozenPrefix);
        frozenPrefix.OnLoaded(api);
        var tst = frozenItem.CodeWithoutFrozenPart();
        var nonFrozenItem = api.World.GetItem(new AssetLocation(tst));
        if (nonFrozenItem is null)
        {
            api.Logger.Warning("[BrainFreeze] Could not find {0} for auto frozen variant registry", frozenItem.CodeWithoutFrozenPart());
            return false;
        }

        frozenItem.Attributes ??= new JsonObject(new JObject());
        nonFrozenItem.Attributes ??= new JsonObject(new JObject());

        BrainFreezeConfig.Instance.AutoRegFrozenVariants.TryGetValue(nonFrozenItem.Code.Path.Split('-')[0], out float freezePoint);

        frozenItem.Attributes.Token["freezePoint"] = freezePoint;
        nonFrozenItem.Attributes.Token["freezePoint"] = freezePoint;

        //Copy hydration values
        if(nonFrozenItem.Attributes["hydration"].Exists)
        {
            frozenItem.Attributes.Token["hydration"] = nonFrozenItem.Attributes["hydration"].Token.DeepClone();
        }

        nonFrozenItem.TransitionableProps ??= [];
        frozenItem.TransitionableProps ??= [];

        var itemStack = new JsonItemStack
        {
            Code = frozenItem.Code,
            Type = EnumItemClass.Item
        };
        itemStack.Resolve(api.World, LoggingKey);

        nonFrozenItem.TransitionableProps = [new TransitionableProperties
        {
            Type = (EnumTransitionType)CustomTransition.ExtendedEnum.FromString("brainfreeze:freeze").Value,
            FreshHours = NatFloat.Zero,
            TransitionHours = new NatFloat(16, 0, EnumDistribution.UNIFORM),
            TransitionRatio = 1,
            TransitionedStack = itemStack,
        }, .. nonFrozenItem.TransitionableProps];

        itemStack = new JsonItemStack
        {
            Code = nonFrozenItem.Code,
            Type = EnumItemClass.Item
        };
        itemStack.Resolve(api.World, LoggingKey);

        frozenItem.TransitionableProps = [.. frozenItem.TransitionableProps.Prepend(new TransitionableProperties
        {
            Type = (EnumTransitionType)CustomTransition.ExtendedEnum.FromString("brainfreeze:melt").Value,
            FreshHours = NatFloat.Zero,
            TransitionHours = new NatFloat(16, 0, EnumDistribution.UNIFORM),
            TransitionRatio = 1,
            TransitionedStack = itemStack,
        })];

        var inContainerProps = (JContainer)frozenItem.Attributes["waterTightContainerProps"].Token;
        inContainerProps ??= (JContainer)nonFrozenItem.Attributes["waterTightContainerProps"].Token?.DeepClone();

        if(inContainerProps != null)
        {
            inContainerProps["AllowSpill"] = false;
        }
        else api.Logger.Warning("waterTightContainerProps where not defined for {0}, is this really a liquid?? (BrainFreeze)", frozenItem.CodeWithoutFrozenPart());

        var blendedOverlays = new BlendedOverlayTexture[]
        {
            new()
            {
                Base = new AssetLocation("game:block/liquid/ice/lake1")
            }
        }; 
        //TODO if the base texture is not 32x32 this is ignored and gives warning, add extra handling for this

        var firstTexture = frozenItem.FirstTexture;
        if (firstTexture != null)
        {

            if (firstTexture.Base.ToString() == "game:block/liquid/waterportion")
            {
                firstTexture.Base = new AssetLocation("game:block/liquid/ice/lake1");
            }
            else
            {
                firstTexture.BlendedOverlays = blendedOverlays;
            }
        }

        if(inContainerProps != null)
        {
            var inContainerTextureStr = inContainerProps["texture"]["base"].ToString();
            if (inContainerTextureStr == "block/liquid/waterportion" || inContainerTextureStr == "game:block/liquid/waterportion")
            {
                inContainerProps["texture"]["base"] = "game:block/liquid/ice/lake1";
            }
            else
            {
                inContainerProps["texture"]["blendedOverlays"] = JToken.FromObject(blendedOverlays);
            }
        }

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

            foreach(var creativeStack in frozenItem.CreativeInventoryStacks)
            {
                creativeStack.Tabs = creativeStack.Tabs.Append("brainfreeze");
            }
        }

        var thawTrans = (EnumTransitionType)CustomTransition.ExtendedEnum.FromString("brainfreeze:melt").Value;
        //Fix transitions
        if (frozenItem.TransitionableProps != null)
        {
            foreach (var transition in frozenItem.TransitionableProps.Where(trans => trans.Type != thawTrans))
            {
                var code = transition.TransitionedStack?.Code?.ToString();
                if (code != null)
                {
                    var frozenAssetLocation = new AssetLocation($"{code}-brainfreeze");
                    if (api.World.GetItem(frozenAssetLocation) != null)
                    {
                        transition.TransitionedStack.Code = frozenAssetLocation;
                        transition.TransitionedStack.Resolve(api.World, LoggingKey);
                    }
                }
            }
        }

        return true;
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
                Tabs = new string[] { "general", "items", "brainfreeze" },
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
        iceCube.CreativeInventoryStacks = [.. creativeStacks];
    }
}