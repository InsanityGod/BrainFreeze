using BrainFreeze.Behaviors;
using CustomTransitionLib;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace BrainFreeze.HarmonyPatches.DynamicRegistry
{
    [HarmonyPatch(typeof(RegistryObjectType), "CreateBasetype")]
    public static class DynamicFrozenVariant
    {
        private static readonly string[] AllowedCodes = new string[] { "waterportion" };
        public static void Postfix(RegistryObjectType __instance)
        {
            if(!__instance.Code.Path.Contains("waterportion") && !AllowedCodes.Contains(__instance.Code.Path)) return;

            var newVariant = new RegistryObjectVariantGroup
            {
                Code = "brainfreeze",
                States = new string[] { "frozen" },
                Combine = __instance.VariantGroups == null ? EnumCombination.Add : EnumCombination.Multiply
            };
            __instance.VariantGroups = (__instance.VariantGroups ?? Array.Empty<RegistryObjectVariantGroup>()).Append(newVariant);
        }

        public static void FinalizeFrozenCollectible(ICoreAPI api, CollectibleObject collectibleObject)
        {
            collectibleObject.MatterState = EnumMatterState.Solid;
            collectibleObject.CollectibleBehaviors ??= Array.Empty<CollectibleBehavior>();

            var frozenPrefix = new FrozenNamePrefix(collectibleObject);
            collectibleObject.CollectibleBehaviors = collectibleObject.CollectibleBehaviors.Append(frozenPrefix);
            frozenPrefix.OnLoaded(api);

            var nonFrozenCollectibleObject = api.World.GetItem(new AssetLocation(collectibleObject.CodeWithoutFrozenPart()));

            nonFrozenCollectibleObject.TransitionableProps ??= Array.Empty<TransitionableProperties>();
            collectibleObject.TransitionableProps ??= Array.Empty<TransitionableProperties>();

            var itemStack = new JsonItemStack
            {
                Code = collectibleObject.Code,
                Type = EnumItemClass.Item
            };

            nonFrozenCollectibleObject.TransitionableProps = nonFrozenCollectibleObject.TransitionableProps.Prepend(new TransitionableProperties
            {
                Type = EnumBrainFreezeTransitionType.Freeze.ConvertToFake(),
                FreshHours = NatFloat.Zero,
                TransitionHours = new NatFloat(16, 0, EnumDistribution.UNIFORM),
                TransitionRatio = 1,
                TransitionedStack = itemStack,
            }).ToArray();

            itemStack = new JsonItemStack
            {
                Code = nonFrozenCollectibleObject.Code,
                Type = EnumItemClass.Item
            };

            collectibleObject.TransitionableProps = collectibleObject.TransitionableProps.Prepend(new TransitionableProperties
            {
                Type = EnumBrainFreezeTransitionType.Thaw.ConvertToFake(),
                FreshHours = NatFloat.Zero,
                TransitionHours = new NatFloat(16, 0, EnumDistribution.UNIFORM),
                TransitionRatio = 1,
                TransitionedStack = itemStack,
            }).ToArray();


            var inContainerProps = (JContainer)collectibleObject.Attributes["waterTightContainerProps"].Token;
            inContainerProps["AllowSpill"] = false;
            inContainerProps["texture"]["base"] = "game:block/liquid/ice/lake1";
        }
    }
}
