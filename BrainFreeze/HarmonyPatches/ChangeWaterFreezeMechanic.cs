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
    //[HarmonyPatch(typeof(BlockWater), nameof(BlockWater.ShouldReceiveServerGameTicks))]
    public static class ChangeWaterFreezeMechanic
    {

		//TODO think of a better way to do this

		public static bool Prefix(BlockWater __instance, IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra, ref bool __result)
		{
			extra = null;
			return false;
			//return true;
			if (!GlobalConstants.MeltingFreezingEnabled)
			{
				return false;
			}

			if(!Traverse.Create(__instance).Field<bool>("freezable").Value) return false;
			
			//TODO see about making deep and surface variant of water instead
			BlockPos nPos = pos.Copy();

			nPos.Y++;
			
			if (world.BlockAccessor.GetBlock(nPos, 2).Id != 0 || world.BlockAccessor.GetBlock(nPos).Replaceable <= 6000) return false;
			nPos.Y--;

			for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
			{
				BlockFacing.HORIZONTALS[i].IterateThruFacingOffsets(nPos);
				if (
					offThreadRandom.NextDouble() > 0.75 //This makes it so that having more sides next to solid gives a higer change at freezing
					&& (
						world.BlockAccessor.GetBlock(nPos, 2) is BlockLakeIce //If block is frozen
						|| world.BlockAccessor.GetBlock(nPos).Replaceable < 5000 //Or is solid enough for attaching ice
					)
					&& world.BlockAccessor.GetClimateAt(
						pos,
						EnumGetClimateMode.ForSuppliedDate_TemperatureOnly,
						world.Calendar.TotalDays
					).Temperature <= Traverse.Create(__instance).Field<float>("freezingPoint").Value)
				{
					var block = world.GetBlock(AssetLocation.Create("game:lakeice"));
					//Console.WriteLine("TST");
					world.BlockAccessor.SetBlock(block.Id, pos, 2);
					return false;
				}
			}


			return false;
		}
    }
}
