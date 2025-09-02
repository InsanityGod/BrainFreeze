using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.HarmonyPatches.FrozenInteractions.Consumption;

[HarmonyPatch(typeof(CollectibleObject))]
public static class AddTemperaturePenalty
{
    [HarmonyPatch("tryEatStop")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> TryEatStopTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var methodToFind = AccessTools.Method(typeof(EntityAgent), nameof(EntityAgent.ReceiveSaturation));
        var methodToInject = AccessTools.Method(typeof(AddTemperaturePenalty), nameof(ApplyPenalty));
        for (var i = 0; i < codes.Count; i++)
        {
            var code = codes[i];
            if(code.opcode == OpCodes.Callvirt && code.operand == methodToFind)
            {
                codes.InsertRange(i + 1, new CodeInstruction[]
                {
                    new(OpCodes.Ldarg_3),
                    new(OpCodes.Ldarg_2),
                    new(OpCodes.Call, methodToInject),
                });

                break;
            }
        }

        return codes;
    }

    public static void ApplyPenalty(EntityAgent byEntity, ItemSlot itemSlot)
    {
        if(byEntity is not EntityPlayer player) return;

        var ConsumptionTemperaturePenalty = itemSlot.Itemstack.Collectible.Attributes != null ?
            itemSlot.Itemstack.Collectible.Attributes["ConsumptionTemperaturePenalty"].AsFloat()
            : 0;

        if(ConsumptionTemperaturePenalty != 0)
        {
            var beh = player.GetBehavior<EntityBehaviorBodyTemperature>();
            if(beh != null)
            {
                //TODO fine tune this
                beh.CurBodyTemperature += ConsumptionTemperaturePenalty;
            }
        }
    }
}
