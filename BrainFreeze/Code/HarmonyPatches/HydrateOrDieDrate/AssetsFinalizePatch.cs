using HarmonyLib;
using HydrateOrDiedrate;
using Vintagestory.API.Common;

namespace BrainFreeze.Code.HarmonyPatches.HydrateOrDieDrate;

[HarmonyPatchCategory("hydrateordiedrate")]
[HarmonyPatch("HydrateOrDiedrate.HydrateOrDiedrateModSystem", "AssetsFinalize")]
internal static class AssetsFinalizePatch
{
    [HarmonyPostfix]
    public static void PostFix(ICoreAPI api)
    {
        var water = api.World.GetItem(new AssetLocation("waterportion"));

        var snowballHydration = HydrationManager.GetHydration(new ItemStack(water)) / 20;
        var snowball = api.World.GetItem(new AssetLocation("snowball-snow"));

        snowball.NutritionProps.Satiety = -1;
        snowball.Attributes.Token["hydration"] = snowballHydration;

        var slush = api.World.GetItem(new AssetLocation("slush"));

        slush.NutritionProps.Satiety = -1;
        slush.Attributes.Token["hydration"] = snowballHydration;
    }
}
