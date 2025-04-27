using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BrainFreeze.Code.HarmonyPatches.TMP
{
    [HarmonyPatch(typeof(CompositeTexture))]
    public static class FixBlendedOverlayDomain //TODO check if still neccesary
    {
        [HarmonyPatch(nameof(CompositeTexture.Bake), argumentTypes: new Type[] { typeof(IAssetManager), typeof(CompositeTexture) })]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> BakeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var methodToReplace = AccessTools.Method(typeof(AssetLocation), nameof(AssetLocation.ToShortString));
            var replacementMethod = AccessTools.Method(typeof(FixBlendedOverlayDomain), nameof(ToShortStringIfNoDomainDifference));
        
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                // Look for the call to AssetLocation.ToShortString
                if (code.opcode == OpCodes.Callvirt && code.operand is MethodInfo mi && mi == methodToReplace)
                {
                    // Modify the instruction to call our replacement method
                    code.opcode = OpCodes.Call;
                    code.operand = replacementMethod;
        
                    // Insert an additional instruction to load the 'ct.Base' onto the stack
                    // Assuming 'ct.Base' is stored in a local variable
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_1)); // Replace with the correct local variable index for 'ct.Base'
                    i++; // Skip over the inserted instruction
                }
            }
        
            return codes;
        }
        
        public static string ToShortStringIfNoDomainDifference(AssetLocation location, CompositeTexture source) =>
            location.Domain == source.Base.Domain ? location.ToShortString() : location.ToString();
    }
}
