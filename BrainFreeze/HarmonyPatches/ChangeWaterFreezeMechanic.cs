using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BrainFreeze.HarmonyPatches
{
    [HarmonyPatch(typeof(BlockWater), nameof(BlockWater.ShouldReceiveServerGameTicks))]
    public static class ChangeWaterFreezeMechanic
    {


		public static bool Prefix(BlockWater __instance, IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra, ref bool __result)
		{
			extra = null;
			return false;
		}
    }
}
