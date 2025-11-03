using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.HarmonyPatches;

[HarmonyPatch]
public static class LiquidContainerMeshTweaks
{
    [HarmonyTargetMethods]
    public static IEnumerable<MethodInfo> TargetMethods()
    {
        yield return AccessTools.Method(typeof(BlockLiquidContainerTopOpened), nameof(BlockLiquidContainerTopOpened.GenMesh), [typeof(ICoreClientAPI), typeof(ItemStack), typeof(BlockPos)]);

        yield return AccessTools.Method(typeof(BlockBarrel), "getContentMesh");
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase originalMethod)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.Advance(matcher.Remaining);
        
        matcher.MatchEndBackwards(
            CodeMatch.Calls(AccessTools.Method(originalMethod.DeclaringType, "TesselateShape"))
        );
        matcher.MatchEndBackwards(
            new CodeMatch(instruction => instruction.IsLdloc() && instruction.operand is LocalBuilder local && local.LocalType == typeof(Shape))
        );
        
        matcher.InsertAfter(
            originalMethod.DeclaringType == typeof(BlockBarrel) ? CodeInstruction.LoadArgument(1) : CodeInstruction.LoadArgument(2),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LiquidContainerMeshTweaks), nameof(ReplaceAssetLocation)))
        );
    
        return matcher.InstructionEnumeration();
    }

    public static Shape ReplaceAssetLocation(Shape shape, ItemStack liquidStack)
    {
        if ((liquidStack?.Collectible.Variant?["brainfreeze"]) != null)
        {
            shape = shape.Clone();

            shape.WalkElements("*", element => element.RenderPass = -1);
        }

        return shape;
    }
}
