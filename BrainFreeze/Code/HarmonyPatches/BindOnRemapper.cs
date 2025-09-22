using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace BrainFreeze.Code.HarmonyPatches;

[HarmonyPatch]
public static class BindOnRemapper
{

    [HarmonyPatch(typeof(ServerSystemItemIdRemapper), "MapByCode")]
    [HarmonyPostfix]
    public static void AppendBrainFreeze(ServerSystemItemIdRemapper __instance, Dictionary<int, AssetLocation> storedItemCodesById, AssetLocation newCode, AssetLocation oldCode, IServerPlayer player, int groupId, bool remap, bool force, bool quiet)
    {
        try
        {
            if(newCode.Path.EndsWith("-brainfreeze")) return;
            var newBrainFreezeCode = new AssetLocation(newCode.Domain, newCode.Path + "-brainfreeze");

            if (player.Entity.Api.World.GetItem(newBrainFreezeCode) is null) return;
            AccessTools.Method(typeof(ServerSystemItemIdRemapper), "MapByCode") //Re-invoke but for brainfreeze variant
                .Invoke(__instance, [
                    storedItemCodesById,
                    newBrainFreezeCode,
                    new AssetLocation(oldCode.Domain, oldCode.Path + "-brainfreeze"),
                    player, 
                    groupId, 
                    remap, 
                    force, 
                    quiet
                ]);
        }
        catch(Exception ex)
        {
            player?.Entity?.Api?.Logger.Error("[brainfreeze] failed to append frozen variant remappings: {0}", ex);
        }
    }
}
