using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Vintagestory.API.Common;

namespace BrainFreeze.Code.HarmonyPatches.FrozenInteractions.Consumption;

[HarmonyPatch(typeof(CollectibleObject), "tryEatBegin")]
public static class PreventBeginEatIfFrozen
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var method = AccessTools.Method(typeof(CollectibleObject), nameof(CollectibleObject.GetNutritionProperties));
        var checkMethod = AccessTools.Method(typeof(Util), nameof(Util.IsFrozenWithWarning));
        for (int i = 0; i < codes.Count; i++)
        {
            var code = codes[i];

            if (code.opcode == OpCodes.Brfalse_S && codes[i - 1].operand == method)
            {
                codes.InsertRange(i + 1, new CodeInstruction[]
                {
                    new(OpCodes.Ldarg_1),
                    new(OpCodes.Call, checkMethod),
                    new(OpCodes.Brtrue_S, code.operand)
                });
                break;
            }
        }

        return codes;
    }
}