using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.HarmonyPatches.FrozenInteractions;

[HarmonyPatch]
public static class SnowPatches
{
    [HarmonyPatch(typeof(ItemSnowball), nameof(ItemSnowball.OnHeldInteractStart))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> AllowStartEatingSnowballTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var canPickSnowBlock = generator.DeclareLocal(typeof(bool));
        var checkLabel1 = generator.DefineLabel();
        var checkLabel2 = generator.DefineLabel();

        var canPickSnowBlockMethod = AccessTools.Method(typeof(BlockBehaviorSnowballable), nameof(BlockBehaviorSnowballable.canPickSnowballFrom));

        var testMethod = AccessTools.Method(typeof(SnowPatches), nameof(ShouldEatSnowballInstead));
        var baseMethod = AccessTools.Method(typeof(CollectibleObject), nameof(CollectibleObject.OnHeldInteractStart));

        Label normalFlowLabel = default;

        for (int i = 0; i < codes.Count; i++)
        {
            var code = codes[i];
            if(code.opcode == OpCodes.Brfalse_S && codes[i - 1].IsLdarg(3))
            {
                normalFlowLabel = (Label)code.operand;
                code.operand = checkLabel1;
            }
            else if (code.Calls(canPickSnowBlockMethod))
            {
                codes[i + 1].operand = checkLabel2;
                codes.InsertRange(i + 1, new CodeInstruction[]
                {
                    //Safe result for later usage
                    CodeInstruction.StoreLocal(canPickSnowBlock.LocalIndex),
                    CodeInstruction.LoadLocal(canPickSnowBlock.LocalIndex)
                });
            }
            else if(code.opcode == OpCodes.Ret)
            {
                var startCode = CodeInstruction.LoadArgument(2);
                startCode.labels.Add(checkLabel1);
                startCode.labels.Add(checkLabel2);

                codes.InsertRange(i + 1, new CodeInstruction[]
                {
                    //Load parameters and run check
                    startCode,
                    CodeInstruction.LoadLocal(canPickSnowBlock.LocalIndex),
                    new(OpCodes.Call, testMethod),
                    new(OpCodes.Brfalse_S, normalFlowLabel), //Go to normal flow if check failed

                    //Call base method to allow for eating
                    CodeInstruction.LoadArgument(0),
                    CodeInstruction.LoadArgument(1),
                    CodeInstruction.LoadArgument(2),
                    CodeInstruction.LoadArgument(3),
                    CodeInstruction.LoadArgument(4),
                    CodeInstruction.LoadArgument(5),
                    CodeInstruction.LoadArgument(6),
                    new(OpCodes.Call, baseMethod),
                    new(OpCodes.Ret),
                });

                break;
            }
        }

        return codes;
    }

    public static bool ShouldEatSnowballInstead(EntityAgent agent, bool canPickSnowBlock) => !canPickSnowBlock && agent.Controls.ShiftKey;

    [HarmonyPatch(typeof(ItemSnowball), nameof(ItemSnowball.OnHeldInteractStep))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> AllowStepEatingSnowball(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var startLabel = generator.DefineLabel();
        codes[0].labels.Add(startLabel);

        var testMethod = AccessTools.Method(typeof(SnowPatches), nameof(ShouldEatSnowballInstead));
        var baseMethod = AccessTools.Method(typeof(CollectibleObject), nameof(CollectibleObject.OnHeldInteractStep));

        codes.InsertRange(0, new CodeInstruction[]
        {
            CodeInstruction.LoadArgument(3),
            new(OpCodes.Ldc_I4_0),
            new(OpCodes.Call, testMethod),
            new(OpCodes.Brfalse_S, startLabel),
            CodeInstruction.LoadArgument(0),
            CodeInstruction.LoadArgument(1),
            CodeInstruction.LoadArgument(2),
            CodeInstruction.LoadArgument(3),
            CodeInstruction.LoadArgument(4),
            CodeInstruction.LoadArgument(5),
            new(OpCodes.Call, baseMethod),
            new(OpCodes.Ret)
        });

        return codes;
    }

    [HarmonyPatch(typeof(ItemSnowball), nameof(ItemSnowball.OnHeldInteractCancel))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> AllowCancelEatingSnowball(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var startLabel = generator.DefineLabel();
        codes[0].labels.Add(startLabel);

        var testMethod = AccessTools.Method(typeof(SnowPatches), nameof(ShouldEatSnowballInstead));
        var baseMethod = AccessTools.Method(typeof(CollectibleObject), nameof(CollectibleObject.OnHeldInteractCancel));

        codes.InsertRange(0, new CodeInstruction[]
        {
            CodeInstruction.LoadArgument(3),
            new(OpCodes.Ldc_I4_0),
            new(OpCodes.Call, testMethod),
            new(OpCodes.Brfalse_S, startLabel),
            CodeInstruction.LoadArgument(0),
            CodeInstruction.LoadArgument(1),
            CodeInstruction.LoadArgument(2),
            CodeInstruction.LoadArgument(3),
            CodeInstruction.LoadArgument(4),
            CodeInstruction.LoadArgument(5),
            CodeInstruction.LoadArgument(6),
            new(OpCodes.Call, baseMethod),
            new(OpCodes.Ret)
        });

        return codes;
    }

    [HarmonyPatch(typeof(ItemSnowball), nameof(ItemSnowball.OnHeldInteractStop))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> AllowStopEatingSnowball(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var startLabel = generator.DefineLabel();
        codes[0].labels.Add(startLabel);

        var testMethod = AccessTools.Method(typeof(SnowPatches), nameof(ShouldEatSnowballInstead));
        var baseMethod = AccessTools.Method(typeof(CollectibleObject), nameof(CollectibleObject.OnHeldInteractStop));

        codes.InsertRange(0, new CodeInstruction[]
        {
            CodeInstruction.LoadArgument(3),
            new(OpCodes.Ldc_I4_0),
            new(OpCodes.Call, testMethod),
            new(OpCodes.Brfalse_S, startLabel),
            CodeInstruction.LoadArgument(0),
            CodeInstruction.LoadArgument(1),
            CodeInstruction.LoadArgument(2),
            CodeInstruction.LoadArgument(3),
            CodeInstruction.LoadArgument(4),
            CodeInstruction.LoadArgument(5),
            new(OpCodes.Call, baseMethod),
            new(OpCodes.Ret)
        });

        return codes;
    }

    [HarmonyPatch(typeof(ItemSnowball), nameof(ItemSnowball.GetHeldInteractionHelp))]
    [HarmonyPostfix]
    public static void FixSnowballHeldInteractionHelp(ref WorldInteraction[] __result)
    {
        var eat = Array.Find(__result, interaction => interaction.ActionLangCode == "heldhelp-eat");
        if(eat != null) eat.HotKeyCode = "shift";
    }
    
    [HarmonyPatch(typeof(BlockBehaviorSnowballable), nameof(BlockBehaviorSnowballable.OnBlockInteractStart))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> AllowCollectingSnowWithBucket(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        //TODO allow for picking up snow
        return codes;
    }

    //TODO: refine and make PR to fix logic flaw in base game code
    [HarmonyPatch(typeof(BlockBehaviorSnowballable), nameof(BlockBehaviorSnowballable.canPickSnowballFrom))]
    [HarmonyPrefix]
    public static bool CanPickSnowballFromOverride(Block block, BlockPos pos, IPlayer byPlayer, ref bool __result)
    {
        var slot = byPlayer.Entity.RightHandItemSlot;
        //Only if we are crouching and have an empty hand or snowball
        if(byPlayer.Entity.Controls.ShiftKey && (slot.Empty || slot.Itemstack.Collectible is ItemSnowball))
        {
            if(block.BlockMaterial == EnumBlockMaterial.Snow || block.snowLevel != 0)
            {
                __result = byPlayer.Entity.World.BlockAccessor.GetBlock(pos.UpCopy()).BlockMaterial != EnumBlockMaterial.Snow; // Disallow when these are stacked snow blocks
            }
        }
        return false;
    }


    [HarmonyPatch(typeof(LiquidItemStackRenderer), nameof(LiquidItemStackRenderer.RegisterLiquidStackRenderer))]
    [HarmonyPrefix]
    public static bool MakeSnowballsNotUseLiquidRenderer(CollectibleObject obj) => obj is not ItemSnowball;
}
