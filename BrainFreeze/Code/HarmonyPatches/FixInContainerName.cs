using HarmonyLib;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.HarmonyPatches
{
    [HarmonyPatch(typeof(BlockLiquidContainerBase), nameof(BlockLiquidContainerBase.GetPlacedBlockInfo))]
    public static class FixInContainerName1
    {
        public static void Postfix(BlockLiquidContainerBase __instance, BlockPos pos, ref string __result)
        {
            var content = __instance.GetContent(pos);
            if (content == null) return;
            var code = content.Collectible.Code;
            var inContainerCode = $"{code.Domain}:incontainer-item-{code.Path}";
            var originalCode = $"{code.Domain}:incontainer-item-{content.Collectible.CodeWithoutFrozenPart(code.Path)}";
            __result = __result.Replace(inContainerCode, $"{Lang.Get("brainfreeze:frozen")} {Lang.Get(originalCode)}");
        }
    }

    [HarmonyPatch(typeof(BlockLiquidContainerBase), nameof(BlockLiquidContainerBase.GetContentInfo))]
    public static class FixInContainerName2
    {
        public static void Postfix(BlockLiquidContainerBase __instance, ItemSlot inSlot, StringBuilder dsc)
        {
            var content = __instance.GetContent(inSlot.Itemstack);
            if (content == null) return;
            var code = content.Collectible.Code;
            var inContainerCode = $"{code.Domain}:incontainer-item-{code.Path}";
            var originalCode = $"{code.Domain}:incontainer-item-{content.Collectible.CodeWithoutFrozenPart(code.Path)}";
            dsc.Replace(inContainerCode, $"{Lang.Get("brainfreeze:frozen")} {Lang.Get(originalCode)}");
        }
    }
}