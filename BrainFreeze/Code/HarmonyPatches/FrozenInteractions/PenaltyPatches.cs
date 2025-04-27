using BrainFreeze.Config;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.HarmonyPatches.FrozenInteractions
{
    [HarmonyPatch]
    public static class PenaltyPatches
    {
        [HarmonyPatch(typeof(EntityBehaviorBodyTemperature), nameof(EntityBehaviorBodyTemperature.OnGameTick))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ChangeFreezeDamage(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var target = AccessTools.Method(typeof(Entity), nameof(Entity.ReceiveDamage));
            var configCall = AccessTools.PropertyGetter(typeof(BrainFreezeModSystem), nameof(BrainFreezeConfig.Instance));
            var freezeDamageCall = AccessTools.PropertyGetter(typeof(BrainFreezeConfig), nameof(BrainFreezeConfig.FreezingDamage));
            for (var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.Calls(target))
                {
                    var damageCode = codes[i - 1];
                    if(damageCode.opcode != OpCodes.Ldc_R4 && (damageCode.operand is not float damage || damage != 0.2f)) break;

                    codes[i - 1] = new(OpCodes.Call, configCall);
                    codes.Insert(i, new(OpCodes.Call, freezeDamageCall));
                    break;
                }
            }

            return codes;
        }

        [HarmonyPatch(typeof(EntityBehaviorBodyTemperature), "updateFreezingAnimState")]
        [HarmonyPrefix]
        public static void AddSpeedPenaly(EntityBehaviorBodyTemperature __instance)
        {
            var str = __instance.entity.WatchedAttributes.GetFloat("freezingEffectStrength", 0);

            __instance.entity.Stats.Set("walkspeed", "freezingPenalty", -str * BrainFreezeConfig.Instance.FreezingMaxSpeedPenalty, true);
        }
    }
}
