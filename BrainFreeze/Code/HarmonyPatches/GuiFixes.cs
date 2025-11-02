using Cairo;
using HarmonyLib;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.HarmonyPatches;

[HarmonyPatch]
public static class GuiFixes
{
    private static void FixString(ref string str, CollectibleObject collectible)
    {
        if (collectible?.Variant == null || collectible.Variant["brainfreeze"] == null) return;
        
        var code = collectible.Code;
        var inContainerCode = $"{code.Domain}:incontainer-item-{code.Path}";
        var originalCode = $"{code.Domain}:incontainer-item-{collectible.PathWithoutFrozenPart()}";
        str = str.Replace(inContainerCode, Lang.Get("brainfreeze:frozen", Lang.Get(originalCode)));
    }

    private static void FixString(StringBuilder str, CollectibleObject collectible)
    {
        if (collectible?.Variant == null || collectible.Variant["brainfreeze"] == null) return;
        
        var code = collectible.Code;
        var inContainerCode = $"{code.Domain}:incontainer-item-{code.Path}";
        var originalCode = $"{code.Domain}:incontainer-item-{collectible.PathWithoutFrozenPart()}";
        str.Replace(inContainerCode, Lang.Get("brainfreeze:frozen", Lang.Get(originalCode)));
    }

    [HarmonyPatch(typeof(GuiDialogBarrel), "getContentsText")]
    [HarmonyPostfix]
    public static void BarrelGuiDialogPostfix(GuiDialogBarrel __instance, ICoreClientAPI ___capi, ref string __result)
    {
        foreach(var content in __instance.Inventory.Select(item => item.Itemstack))
        {
            FixString(ref __result, content?.Collectible);
        }

        if(___capi.World.BlockAccessor.GetBlockEntity(__instance.BlockEntityPosition) is BlockEntityBarrel bebarrel)
        {
            FixString(ref __result, bebarrel.CurrentRecipe?.Output?.ResolvedItemstack?.Collectible);
        }
    }

    [HarmonyPatch(typeof(BlockLiquidContainerBase), nameof(BlockLiquidContainerBase.GetPlacedBlockInfo))]
    [HarmonyPostfix]
    public static void GetPlacedBlockInfoPostfix(BlockLiquidContainerBase __instance, BlockPos pos, ref string __result) 
        => FixString(ref __result, __instance.GetContent(pos)?.Collectible);

    [HarmonyPatch(typeof(BlockLiquidContainerBase), nameof(BlockLiquidContainerBase.GetContentInfo))]
    [HarmonyPostfix]
    public static void GetContentInfoPostfix(BlockLiquidContainerBase __instance, ItemSlot inSlot, StringBuilder dsc)
        => FixString(dsc, __instance.GetContent(inSlot.Itemstack)?.Collectible);

    [HarmonyPatch(typeof(BlockLiquidContainerTopOpened), nameof(BlockLiquidContainerTopOpened.GetContainedInfo))]
    [HarmonyPostfix]
    public static void GetContainedInfoPostfix(BlockLiquidContainerTopOpened __instance, ItemSlot inSlot, ref string __result)
        => FixString(ref __result, __instance.GetContent(inSlot.Itemstack)?.Collectible);
}
