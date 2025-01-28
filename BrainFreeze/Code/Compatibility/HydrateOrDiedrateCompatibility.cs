using HydrateOrDiedrate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace BrainFreeze.Code.Compatibility
{
    public static class HydrateOrDiedrateCompatibility
    {
        internal static void FixSnow(ICoreAPI api)
        {
                var water = api.World.GetItem(new AssetLocation("waterportion"));

                var snowballHydration = HydrationManager.GetHydration(new ItemStack(water)) / 20;
                var snowball = api.World.GetItem(new AssetLocation("snowball-snow"));

                snowball.NutritionProps.Satiety = -1;
                HydrationManager.SetHydration(api, snowball, snowballHydration);

                var slush = api.World.GetItem(new AssetLocation("slush"));
                
                slush.NutritionProps.Satiety = -1;
                HydrationManager.SetHydration(api, slush, snowballHydration);
        }
    }
}
