using Cairo;
using HarmonyLib;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.HarmonyPatches
{
    [HarmonyPatch]
    public static class GuiFixes
    {
        //TODO see if we can dynamically create language keys instead -_-

        [HarmonyPatch(typeof(GuiDialogBarrel), "getContentsText")]
        [HarmonyPostfix]
        public static void BarrelGuiDialogPostfix(GuiDialogBarrel __instance, ref string __result)
        {
            var api = Traverse.Create(__instance).Field<ICoreClientAPI>("capi").Value;

            BlockEntityBarrel bebarrel = api.World.BlockAccessor.GetBlockEntity(__instance.BlockEntityPosition) as BlockEntityBarrel;

            foreach(var content in __instance.Inventory.Select(item => item.Itemstack).Append(bebarrel.CurrentRecipe?.Output?.ResolvedItemstack))
            {
                if (content?.Collectible?.Variant == null || content.Collectible.Variant["brainfreeze"] == null) continue;
                var code = content.Collectible.Code;
                var inContainerCode = $"{code.Domain}:incontainer-item-{code.Path}";
                var originalCode = $"{code.Domain}:incontainer-item-{content.Collectible.CodeWithoutFrozenPart(code.Path)}";
                __result = __result.Replace(inContainerCode, $"{Lang.Get("brainfreeze:frozen")} {Lang.Get(originalCode)}");
            }
        }

        [HarmonyPatch(typeof(BlockLiquidContainerBase), nameof(BlockLiquidContainerBase.GetPlacedBlockInfo))]
        [HarmonyPostfix]
        public static void GetPlacedBlockInfoPostfix(BlockLiquidContainerBase __instance, BlockPos pos, ref string __result)
        {
            var content = __instance.GetContent(pos);
            if (content?.Collectible?.Variant == null || content.Collectible.Variant["brainfreeze"] == null) return;
            var code = content.Collectible.Code;
            var inContainerCode = $"{code.Domain}:incontainer-item-{code.Path}";
            var originalCode = $"{code.Domain}:incontainer-item-{content.Collectible.CodeWithoutFrozenPart(code.Path)}";
            __result = __result.Replace(inContainerCode, $"{Lang.Get("brainfreeze:frozen")} {Lang.Get(originalCode)}");
        }

        [HarmonyPatch(typeof(BlockLiquidContainerBase), nameof(BlockLiquidContainerBase.GetContentInfo))]
        [HarmonyPostfix]
        public static void GetContentInfoPostfix(BlockLiquidContainerBase __instance, ItemSlot inSlot, StringBuilder dsc)
        {
            var content = __instance.GetContent(inSlot.Itemstack);
            if (content?.Collectible?.Variant == null || content.Collectible.Variant["brainfreeze"] == null) return;
            var code = content.Collectible.Code;
            var inContainerCode = $"{code.Domain}:incontainer-item-{code.Path}";
            var originalCode = $"{code.Domain}:incontainer-item-{content.Collectible.CodeWithoutFrozenPart(code.Path)}";
            dsc.Replace(inContainerCode, $"{Lang.Get("brainfreeze:frozen")} {Lang.Get(originalCode)}");
        }

        [HarmonyPatch(typeof(BlockLiquidContainerTopOpened), nameof(BlockLiquidContainerTopOpened.GetContainedInfo))]
        [HarmonyPostfix]
        public static void GetContainedInfoPostfix(BlockLiquidContainerTopOpened __instance, ItemSlot inSlot, ref string __result)
        {
            ItemStack content = __instance.GetContent(inSlot.Itemstack);
            if (content?.Collectible?.Variant == null || content.Collectible.Variant["brainfreeze"] == null) return;
            var code = content.Collectible.Code;
            var inContainerCode = $"{code.Domain}:incontainer-item-{code.Path}";
            var originalCode = $"{code.Domain}:incontainer-item-{content.Collectible.CodeWithoutFrozenPart(code.Path)}";
            __result = __result.Replace(inContainerCode, $"{Lang.Get("brainfreeze:frozen")} {Lang.Get(originalCode)}");
        }
    }
}
