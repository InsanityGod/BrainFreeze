using BrainFreeze.Code.Behaviors;
using BrainFreeze.Config;
using HarmonyLib;
using InsanityLib.Util.ContentFeatures;
using InsanityLib.Util.SpanUtil;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace BrainFreeze.Code.HarmonyPatches.DynamicRegistry;

//Just pretend you didn't see this class
[HarmonyPatch]
public static class DynamicFrozenVariant //TODO cleanup
{
    public const string LoggingKey = "BrainFreezeAutoRegistry";

    [HarmonyPatch(typeof(RegistryObjectType), "CreateBasetype")]
    [HarmonyPostfix]
    public static void CreateBasetypePostfix(ICoreAPI api, RegistryObjectType __instance)
    {
        if(__instance.VariantGroups is not null && __instance.VariantGroups.Length > 0) return; //In this case we do it later
        if(!BrainFreezeConfig.Instance.AutoRegFrozenVariants.ContainsKey(__instance.Code.Path.AsSpan().FirstCodePartAsSpan().ToString())) return;
        __instance.VariantGroups = [
            new RegistryObjectVariantGroup
            {
                Code = "brainfreeze",
                States = ["brainfreeze"],
                Combine = EnumCombination.Add,
            }
        ];

        if (__instance.SkipVariants is not null) __instance.SkipVariants = [.. __instance.SkipVariants, ..__instance.SkipVariants.Select(variant => new AssetLocation(variant.Domain, $"{variant.Path}-brainfreeze"))];
    }

    [HarmonyPatch(typeof(ModRegistryObjectTypeLoader), "GatherVariants")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> AppendVariants(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);

        matcher.MatchEndForward(
            CodeMatch.Calls(AccessTools.Method(typeof(ResolvedVariant), nameof(ResolvedVariant.ResolveCode)))
        );

        matcher.MatchEndForward(
            new CodeMatch(OpCodes.Endfinally),
            new CodeMatch()
        );

        matcher.InsertAfter(
            CodeInstruction.LoadArgument(1),
            CodeInstruction.LoadLocal(0),
            CodeInstruction.LoadArgument(4, true),
            CodeInstruction.LoadArgument(5, true),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DynamicFrozenVariant), nameof(AppendBrainFreeze)))
        );

        return matcher.InstructionEnumeration();
    }

    private static void AppendBrainFreeze(AssetLocation baseCode, List<ResolvedVariant> variantsFinal, ref AssetLocation[] allowedVariants, ref AssetLocation[] skipVariants)
    {
        if(variantsFinal.Any(static final => final.CodeParts.ContainsKey("brainfreeze"))) return;
        if(!BrainFreezeConfig.Instance.AutoRegFrozenVariants.ContainsKey(baseCode.Path)) return;

        var originalLength = variantsFinal.Count;
        for (int i = 0; i < originalLength; i++)
        {
            var newVariant = new ResolvedVariant
            {
                CodeParts = new(variantsFinal[i].CodeParts)
            };
            newVariant.AddCodePart("brainfreeze", "brainfreeze");
            newVariant.ResolveCode(baseCode);
            variantsFinal.Add(newVariant);
        }

        if(allowedVariants is not null)
        {
            allowedVariants = [
                ..allowedVariants,
                ..allowedVariants.Select(static existing => $"{existing}-brainfreeze")
            ];
        }

        if(skipVariants is not null)
        {
            skipVariants = [
                ..skipVariants,
                ..skipVariants.Select(static existing => $"{existing}-brainfreeze")
            ];
        }
    }

    [HarmonyPatch(typeof(RegistryObjectType), "solveByType")]
    [HarmonyPrefix]
    public static void MakeByTypeIgnoreBrainFreeze(ref string codePath)
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
        var nonFrozenCode = new AssetLocation(frozenItem.Code.Domain, frozenItem.PathWithoutFrozenPart());
        var nonFrozenItem = api.World.GetItem(nonFrozenCode);
        if (nonFrozenItem is null)
        {
            api.Logger.Warning("[BrainFreeze] Could not find {0} for auto frozen variant registry", nonFrozenCode);
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
        else api.Logger.Warning("waterTightContainerProps where not defined for {0}:{1}, is this really a liquid?? (BrainFreeze)", frozenItem.Code.Domain, frozenItem.PathWithoutFrozenPart());

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

    [HarmonyPatch(typeof(ClientSystemStartup), "prepareAsync", typeof(IList<Item>))]
    [HarmonyPrefix]
    public static void PrepareFrozenTextures(IList<Item> items, ClientMain ___game)
    {
        if(___game.Side != EnumAppSide.Client) return;
        foreach(var item in items)
        {
            if (item.Variant["brainfreeze"] is null) continue;

            try
            {
                FixFrozenTextures(___game.api, item);
            }
            catch(Exception ex)
            {
                ___game.Logger.Warning("[BrainFreeze] an error occured while adjusting textures for frozen liquid '{0}' frozen liquid likely won't look very frozen, exception: {1}", item.Code, ex);
            }
        }
    }

    public static void FixFrozenTextures(ICoreAPI api, Item frozenItem)
    {
        var blendedOverlays = new BlendedOverlayTexture[]
        {
            new()
            {
                Base = new AssetLocation("game","block/liquid/ice/lake1")
            }
        };

        var firstTexture = frozenItem.FirstTexture;
        if (firstTexture != null)
        {

            if (firstTexture.Base.Domain == "game" && firstTexture.Base.Path.Contains("block/liquid/waterportion"))
            {
                firstTexture.Base = new AssetLocation("game","block/liquid/ice/lake1");
            }
            else
            {
                //TODO this should ideally be done client side, but for now it's done server side.
                (int height, int width) = api.Assets.CheckTextureSize(firstTexture.Base);

                if (height == 24 && width == 24)
                {
                    blendedOverlays[0].Base.Domain = "brainfreeze";
                }
                else if (!(height == 32 && width == 32))
                {
                    api.Logger.Warning("[BrainFreeze] The texture {0} used by {1}:{2} does not have a supported size for automatic frozen variant overlay blending. Supported sizes are: 32x32, 24x24. No ice overlay will be applied.", firstTexture.Base, frozenItem.Code.Domain, frozenItem.Code.Path);
                    blendedOverlays = null;
                }

                if (blendedOverlays is not null) firstTexture.BlendedOverlays = blendedOverlays;
            }
        }

        if(frozenItem.Attributes["waterTightContainerProps"].Token is JContainer inContainerProps)
        {
            var inContainerTextureStr = inContainerProps["texture"]["base"].ToString();
            if (inContainerTextureStr == "block/liquid/waterportion" || inContainerTextureStr == "game:block/liquid/waterportion")
            {
                inContainerProps["texture"]["base"] = "game:block/liquid/ice/lake1";
            }
            else if(blendedOverlays is not null)
            {
                inContainerProps["texture"]["blendedOverlays"] = JToken.FromObject(blendedOverlays);
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
                Tabs = ["general", "items", "brainfreeze"],
                Stacks =
                [
                    new()
                    {
                        Code = iceCube.Code,
                        Type = EnumItemClass.Item,
                        Attributes = attr
                    }
                ]
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